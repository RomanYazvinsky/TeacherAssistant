using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using JetBrains.Annotations;
using SerialPortLib;

namespace TeacherAssistant.ReaderPlugin {
    public class SerialUtil : IDisposable {
        private readonly SerialPortInput _serialPort;
        private readonly IObservable<string> _serialDataStream;
        [CanBeNull] private string _currentPort;
        private readonly Subject<ConnectionStatus> _whenConnectionStatusChanges;


        public SerialUtil() {
            _serialPort = new SerialPortInput();
            _whenConnectionStatusChanges = new Subject<ConnectionStatus>();
            _serialDataStream = Observable
                .FromEventPattern<SerialPortInput.MessageReceivedEventHandler, MessageReceivedEventArgs>
                (
                    handler => _serialPort.MessageReceived += handler,
                    handler => _serialPort.MessageReceived -= handler
                )
                .Select(eventPattern => Encoding.ASCII.GetString(eventPattern.EventArgs.Data));
            ConfigureConnectionChecking();
        }

        private void ConfigureConnectionChecking() {
            this.WhenConnectionStatusChanges
                .Where(status => status.IsConnected)
                .Subscribe(status => {
                    Observable.Interval(TimeSpan.FromMilliseconds(5000))
                        .TakeUntil(this.WhenConnectionStatusChanges)
                        .Subscribe(l => CheckConnection(status.SerialPortName));
                });
        }

        private void CheckConnection([NotNull] string portName) {
            if (SerialPort.GetPortNames().Contains(portName)) {
                return;
            }
            Close(ConnectionStatusChangeReason.DeviceDisconnected);
        }

        private void TryConnect([NotNull] string portName) {
            _serialPort.SetPort(portName, 57600);
            Observable.Timer(TimeSpan.FromMilliseconds(500))
                .TakeUntil(_serialDataStream)
                .Subscribe(_ => Close());
            _currentPort = portName;
            _serialPort.Connect();
            _serialPort.SendMessage(Encoding.ASCII.GetBytes("get info"));
        }

        public void Start() {
            foreach (var portName in SerialPort.GetPortNames()) {
                TryConnect(portName);
            }
        }

        public void Close() => Close(ConnectionStatusChangeReason.DisconnectRequested);

        private void Close(ConnectionStatusChangeReason? reason) {
            if (!_serialPort.IsConnected) {
                return;
            }

            if (reason != null) {
                var connectionStatus = new ConnectionStatus(reason.Value, _currentPort);
                _whenConnectionStatusChanges.OnNext(connectionStatus);
            }
            _serialPort.Disconnect();
            _currentPort = null;
        }


        public IObservable<ConnectionStatus> WhenConnectionStatusChanges => _whenConnectionStatusChanges;

        public IObservable<string[]> OnRead() {
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

        public void Dispose() {
            _whenConnectionStatusChanges?.Dispose();
            Close(null);
        }
    }

    public class ConnectionStatus {
        public ConnectionStatus(ConnectionStatusChangeReason changeReason, [NotNull] string serialPortName) {
            this.ChangeReason = changeReason;
            this.SerialPortName = serialPortName;
            this.IsConnected = changeReason == ConnectionStatusChangeReason.ConnectionRequested
                               || changeReason == ConnectionStatusChangeReason.AutoRestoreConnection;
        }

        public bool IsConnected { get; }
        public ConnectionStatusChangeReason ChangeReason { get; }

        [NotNull] public string SerialPortName { get; }
    }

    public enum ConnectionStatusChangeReason {
        ConnectionRequested,
        AutoRestoreConnection,
        DeviceDisconnected,
        DisconnectRequested
    }
}