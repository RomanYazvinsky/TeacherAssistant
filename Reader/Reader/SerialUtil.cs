using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SerialPortLib;

namespace TeacherAssistant.Reader
{
    public class SerialUtil : IDisposable
    {
        private readonly SerialPortInput _serialPort;
        private readonly IObservable<string> _serialDataStream;
        private readonly Subject<string[]> _readData = new Subject<string[]>();
        private readonly BehaviorSubject<ConnectionStatus> _connectionStatus;
        [CanBeNull] private string _currentPort;


        public SerialUtil()
        {
            _serialPort = new SerialPortInput();
            _connectionStatus = new BehaviorSubject<ConnectionStatus>(
                new ConnectionStatus(ConnectionStatusChangeReason.InitialState, null)
            );
            _serialDataStream = Observable
                .FromEventPattern<SerialPortInput.MessageReceivedEventHandler, MessageReceivedEventArgs>
                (
                    handler => _serialPort.MessageReceived += handler,
                    handler => _serialPort.MessageReceived -= handler
                )
                .Select(eventPattern => Encoding.ASCII.GetString(eventPattern.EventArgs.Data));
            ConfigureConnectionChecking();
            ConfigureDataReading();
        }

        private void ConfigureConnectionChecking()
        {
            this.ConnectionStatus
                .Where(status => status.IsConnected)
                .Subscribe(status =>
                {
                    Observable.Interval(TimeSpan.FromMilliseconds(5000))
                        .TakeUntil(this.ConnectionStatus.Skip(1))
                        // ReSharper disable once AssignNullToNotNullAttribute => isConnected means it has port name
                        .Subscribe(l => CheckConnection(status.SerialPortName));
                });
        }

        private void ConfigureDataReading()
        {
            OnRead().Subscribe(data => { this._readData.OnNext(data); });
        }

        private void CheckConnection([NotNull] string portName)
        {
            if (SerialPort.GetPortNames().Contains(portName))
            {
                return;
            }

            Close(ConnectionStatusChangeReason.DeviceDisconnected);
        }

        private async Task<bool> TryConnect([NotNull] string portName)
        {
            _serialPort.SetPort(portName, 57600);
            _currentPort = portName;
            _serialPort.Connect();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            var timer = Observable.Timer(TimeSpan.FromMilliseconds(1000));
            timer.TakeUntil(_serialDataStream).Subscribe(_ => Close(null));
            _serialPort.SendMessage(Encoding.ASCII.GetBytes("get info"));
            await timer;
            return _serialPort.IsConnected;
        }

        public async void Start()
        {
            foreach (var name in SerialPort.GetPortNames())
            {
                var tryConnect = await TryConnect(name);
                if (tryConnect)
                {
                    _connectionStatus.OnNext(new ConnectionStatus(ConnectionStatusChangeReason.ConnectionRequested, name));
                    return;
                }

                await Task.Delay(500);
            }
        }

        public void Close() => Close(ConnectionStatusChangeReason.DisconnectRequested);

        private void Close(ConnectionStatusChangeReason? reason)
        {
            if (reason != null)
            {
                var connectionStatus = new ConnectionStatus(reason.Value, _currentPort);
                _connectionStatus.OnNext(connectionStatus);
            }

            _serialPort.Disconnect();
            _currentPort = null;
        }

        public bool IsRunning => _serialPort.IsConnected;

        public IObservable<ConnectionStatus> ConnectionStatus => _connectionStatus;

        public IObservable<string[]> ReadData => this._readData;

        private IObservable<string[]> OnRead()
        {
            var observable = _serialDataStream
                .Where(s => !s.StartsWith("RFID"))
                .Select
                (
                    s => s
                        .Replace("\r", "")
                        .Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries)
                )
                .Where(strings => strings.Length > 0);
            var throttle = observable.Throttle(TimeSpan.FromMilliseconds(500));
            return observable.Buffer(throttle)
                .Select(s => s.Aggregate((strings, strings1) => strings.Concat(strings1).ToArray()));
        }

        public void Dispose()
        {
            _connectionStatus?.Dispose();
            _readData?.OnCompleted();
            _readData?.Dispose();
            Close(null);
        }
    }

    public class ConnectionStatus
    {
        public ConnectionStatus(ConnectionStatusChangeReason changeReason, [CanBeNull] string serialPortName)
        {
            this.ChangeReason = changeReason;
            this.SerialPortName = serialPortName;
            this.IsConnected = changeReason == ConnectionStatusChangeReason.ConnectionRequested
                               || changeReason == ConnectionStatusChangeReason.AutoRestoreConnection;
        }

        public bool IsConnected { get; }
        public ConnectionStatusChangeReason ChangeReason { get; }

        [CanBeNull] public string SerialPortName { get; }
    }

    public enum ConnectionStatusChangeReason
    {
        InitialState,
        ConnectionRequested,
        AutoRestoreConnection,
        DeviceDisconnected,
        DisconnectRequested
    }
}
