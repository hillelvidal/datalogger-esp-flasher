using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ESPFlasher.Services
{
    public class SerialMonitorService : IDisposable
    {
        private readonly ILogger _logger;
        private SerialPort? _serialPort;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isMonitoring;

        public event EventHandler<string>? DataReceived;
        public event EventHandler<string>? StatusChanged;

        public bool IsMonitoring => _isMonitoring;

        public SerialMonitorService(ILogger logger)
        {
            _logger = logger;
        }

        public bool StartMonitoring(string portName, int baudRate = 115200)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Monitor already running");
                return false;
            }

            try
            {
                _serialPort = new SerialPort(portName, baudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    Encoding = Encoding.UTF8
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.ErrorReceived += SerialPort_ErrorReceived;

                _serialPort.Open();
                _isMonitoring = true;

                _cancellationTokenSource = new CancellationTokenSource();

                _logger.LogInformation($"Serial monitor started on {portName} at {baudRate} baud");
                StatusChanged?.Invoke(this, $"Monitoring {portName} at {baudRate} baud");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start serial monitor on {portName}");
                StatusChanged?.Invoke(this, $"Failed to start monitor: {ex.Message}");
                Cleanup();
                return false;
            }
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring)
            {
                return;
            }

            _logger.LogInformation("Stopping serial monitor");
            StatusChanged?.Invoke(this, "Monitor stopped");

            Cleanup();
        }

        public bool SendData(string data)
        {
            if (!_isMonitoring || _serialPort == null || !_serialPort.IsOpen)
            {
                _logger.LogWarning("Cannot send data: monitor not running");
                return false;
            }

            try
            {
                _serialPort.WriteLine(data);
                _logger.LogDebug($"Sent: {data}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send data");
                StatusChanged?.Invoke(this, $"Send error: {ex.Message}");
                return false;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            try
            {
                string data = _serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    DataReceived?.Invoke(this, data);
                }
            }
            catch (TimeoutException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading serial data");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            _logger.LogWarning($"Serial port error: {e.EventType}");
            StatusChanged?.Invoke(this, $"Port error: {e.EventType}");
        }

        private void Cleanup()
        {
            _isMonitoring = false;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.ErrorReceived -= SerialPort_ErrorReceived;
                    _serialPort.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during serial port cleanup");
                }
                finally
                {
                    _serialPort = null;
                }
            }
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
