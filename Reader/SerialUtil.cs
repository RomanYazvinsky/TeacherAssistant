using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading;
using SerialPortLib;

namespace TeacherAssistant.ReaderPlugin {
    public class SerialUtil : ISerialUtil {
        private SerialPortInput _serialPort;
        public event EventHandler<string> Connected;
        public event EventHandler Disconnected;
        public event EventHandler<string> ConnectionFailed;
        private volatile bool _isReadingAllowed = false;
        private volatile bool _connected = false;
        private IObservable<string> _fromEvent;


        public SerialUtil() {
            _isReadingAllowed = false;
            Connected += (sender, s) => {
                Observable.Interval(TimeSpan.FromMilliseconds(5000))
                          .TakeWhile(l => _connected)
                          .Subscribe
                           (
                               l => {
                                   if (SerialPort.GetPortNames().Contains(s))
                                       return;
                                   ConnectionFailed?.Invoke(this, "Connection aborted");
                                   Close();
                               }
                           );
            };

            _serialPort = new SerialPortInput();
            _fromEvent = Observable
                        .FromEventPattern<SerialPortInput.MessageReceivedEventHandler, MessageReceivedEventArgs>
                         (
                             handler =>
                                 _serialPort.MessageReceived += handler,
                             handler => _serialPort.MessageReceived -= handler
                         )
                        .Select(eventPattern => { return Encoding.ASCII.GetString(eventPattern.EventArgs.Data); });
            OnRead().Subscribe(card => { Debug.WriteLine(card); });
        }


        private void TryConnect(string portName) {
            _serialPort.SetPort(portName, 57600);
            Timer timer = null;
            timer = new Timer
            (
                obj => {
                    // Dispose() is not immediate
                    if (_connected)
                        return;
                    Close();
                    timer.Dispose();
                    ConnectionFailed?.Invoke(this, $"Connection to port \"{portName}\" failed");
                },
                null,
                5000,
                Timeout.Infinite
            );
            _fromEvent.Take(1)
                      .Subscribe
                       (
                           result => {
                               // prevent timer to be executed
                               _connected = true;
                               timer.Dispose();
                               Connected?.Invoke(this, portName);
                           }
                       );

            _serialPort.Connect();
            _serialPort.SendMessage(Encoding.ASCII.GetBytes("get info"));
        }

        private IObservable<string> AsObservable() {
            return _fromEvent
               .Where(s => !s.StartsWith("RFID"));
        }

        public void Start() {
            if (_isReadingAllowed)
                return;
            foreach (var portName in SerialPort.GetPortNames()) {
                Debug.WriteLine($"Trying to access '{portName}' port - ");
                TryConnect(portName);
            }
        }


        public void Close() {
            _isReadingAllowed = false;
            _connected = false;
            if (_serialPort.IsConnected) {
                _serialPort.Disconnect();
            }

            Disconnected?.Invoke(this, null);
        }


        public IObservable<string[]> OnRead() {
            var observable = AsObservable()
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
    }
}