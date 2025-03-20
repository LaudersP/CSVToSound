using CoreOSC;
using CoreOSC.IO;
using System.Net.Sockets;

namespace CSVToSound
{
    internal class OSC
    {
        private readonly string _ipAddress;
        private readonly int _portNum;
        private UdpClient? _udpClient;

        public OSC(string ipAddress, int portNum)
        {
            _ipAddress = ipAddress;
            _portNum = portNum;

            _udpClient = new UdpClient(_ipAddress, _portNum);
        }

        public void SendMessage(string address, params object[] args)
        {
            var message = new OscMessage(new Address(address), args);

            _udpClient.SendMessageAsync(message).Wait();
        }

        public void Close()
        {
            _udpClient?.Close();
            _udpClient = null;
        }
    }
}
