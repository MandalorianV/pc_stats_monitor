using System;
using System.IO.Ports;

namespace Monitor.Worker
{
    // PC Stats agent'in her tick'te okudugu ayni GpuTemp degerini
    // Arduino'ya seri port uzerinden yollar. Telefona giden TCP akisina
    // dokunmaz, paralel calisir.
    public class SerialFanSender
    {
        private SerialPort? _port;
        private readonly string _portName;

        public SerialFanSender(string portName)
        {
            _portName = portName; // ornek: "COM5" - Arduino'nun gercek portu
        }

        private void EnsureOpen()
        {
            if (_port != null && _port.IsOpen) return;

            try
            {
                _port = new SerialPort(_portName, 9600);
                _port.Open();
            }
            catch
            {
                _port = null; // Arduino takili degilse sessizce vazgec, bir sonraki tick'te tekrar dener
            }
        }

        public void SendGpuTemp(float gpuTemp)
        {
            EnsureOpen();
            if (_port == null || !_port.IsOpen) return;

            try
            {
                _port.WriteLine(gpuTemp.ToString("F1")); // ornek: "41.3"
            }
            catch
            {
                _port?.Close();
                _port = null; // koptuysa bir sonraki tick'te yeniden acmayi dener
            }
        }
    }
}
