using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TeacherAssistant.ReaderPlugin
{
    /**
     * Manages connection to Card reader
     */
    public class SerialUtil : ISerialUtil
    {
        private SerialPort _serialPort;
        public event EventHandler<string> DataReceivedSuccess;
        public event EventHandler<string> DataReceivedError;
        public event EventHandler<string> Connected;
        public event EventHandler Disconnected;
        private CancellationTokenSource _cancellationTokenSource;
        private static string HexToString(string hex)
        {
            var buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexdec = hex.Substring(i, 2);
                buffer[i / 2] = byte.Parse(hexdec, NumberStyles.HexNumber);
            }

            return Encoding.GetEncoding("Windows-1251").GetString(buffer);
        }


        private static string ParseData(string data)
        {
            return HexToString(Regex.Replace(data, @"\s+", ""));
        }

        private static string CleanData(string data)
        {
            return data.Replace("\r", "").Replace("\0", "");
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                EventHandler<string> connectedHandler = null;
                connectedHandler = async (sender, s) =>
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        while (true)
                        {
                            var result = await _serialPort.ReadAsync(_cancellationTokenSource.Token);
                            if (!result.StartsWith("Card"))
                            {
                                result = ParseData(result);
                            }

                            result = CleanData(result);
                            DataReceivedSuccess?.Invoke(this, result);

                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        _serialPort.Close();
                        Debug.WriteLine(e);
                        Connected -= connectedHandler;
                        Disconnected?.Invoke(this, null);
                    }
                };
                Connected += connectedHandler;
                foreach (var portName in SerialPort.GetPortNames())
                {
                    try
                    {
                        Debug.WriteLine($"Trying to access '{portName}' port - ");
                        await TryConnect(portName);
                        Connected?.Invoke(this, portName);
                    }
                    catch (OperationCanceledException e)
                    {
                        Debug.Write("Timeout exceed, trying one more time");
                        _serialPort.Close();
                        try
                        {
                            await TryConnect(portName);
                            Connected?.Invoke(this, portName);
                        }
                        catch (OperationCanceledException exception)
                        {
                            _serialPort.Close();
                        }

                    }
                }
            });
        }

        private async Task<bool> TryConnect(string portName)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _serialPort.PortName = portName;
            _serialPort.Open();
            _serialPort.Write("get info");
            Timer timer = null;
            timer = new Timer(obj =>
            {
                _cancellationTokenSource.Cancel(true);
                timer.Dispose();
            }, null, 2000, Timeout.Infinite);
            var result = await _serialPort.ReadAsync(_cancellationTokenSource.Token);
            timer.Dispose();
            return result.StartsWith("RFID");
        }

        private static SerialPort Initialize()
        {
            var port = new SerialPort
            {
                BaudRate = 57600,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                RtsEnable = false,
                DtrEnable = false,
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                WriteTimeout = 500
            };
            return port;
        }

        public SerialUtil()
        {
            _serialPort = Initialize();
        }

        public void Close()
        {
            _serialPort.Close();
            _cancellationTokenSource?.Cancel(true);
        }


        public async Task<string> ReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return await _serialPort.ReadAsync(cancellationToken);
        }

        public async Task<string> ReadCardAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            string cardInfo = await ReadAsync(cancellationToken);
            if (!cardInfo.StartsWith("Card UID"))
            {
                return null;
            }

            string hexStr = await ReadAsync(cancellationToken);


            cardInfo += Environment.NewLine + ParseData(hexStr);
            return cardInfo;
        }
    }
}