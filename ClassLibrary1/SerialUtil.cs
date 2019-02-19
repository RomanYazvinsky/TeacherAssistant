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
    public class SerialUtil
    {
        private static CancellationToken _cancellationToken;

        private SerialPort _serialPort;

        private static SerialUtil _serialUtil;

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

        private static SerialPort Initialize()
        {
            SerialPort port = new SerialPort();
            port.BaudRate = 57600;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.RtsEnable = false;
            port.DtrEnable = false;
            port.Handshake = Handshake.None;
            port.ReadTimeout = 1000;
            port.WriteTimeout = 500;
            return port;
        }

        private SerialUtil()
        {
            _serialPort = Initialize();
        }

        public static async Task<SerialUtil> GetInstance()
        {
            if (_serialUtil == null)
            {
                _serialUtil = new SerialUtil();
                await _serialUtil.Reconnect();
            }

            return _serialUtil;
        }

        public async Task Reconnect()
        {
            string port = await GetActivePort();
            if (port != null)
            {
                _serialPort.PortName = port;
                _serialPort.Open();
            }
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public bool IsOpen()
        {
            return _serialPort.IsOpen;
        }
        public static async Task<string> GetActivePort()
        {
            SerialPort port = Initialize();
            foreach (var portName in SerialPort.GetPortNames())
            {
                try
                {
                    port.PortName = portName;
                    port.Open();
                    port.Write("get info");
                    string report = await port.ReadAsync();
                    port.Close();
                    if (report.StartsWith("RFID Reader"))
                    {
                        return portName;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    port.Close();
                }
               
            }

            return null;
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