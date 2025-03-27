using CoreOSC;
using CoreOSC.IO;
using System;
using System.Net.Sockets;

namespace CSVToSound
{
    internal class OSC
    {
        private UdpClient? _udpClient;
        private readonly string _ipAddress;
        private readonly int _portNum;
        private readonly IMessageService? _messageService;

        public OSC(string ipAddress, int portNum, IMessageService? messageService = null)
        {
            _ipAddress = ipAddress;
            _portNum = portNum;
            _messageService = messageService;

            try
            {
                // Create the instance of the client for sending
                _udpClient = new UdpClient(_ipAddress, _portNum);
            }
            catch (Exception ex)
            {
                _messageService?.DisplayAlert
                (
                    "Connection Error",
                    $"Failed to establish connection: {ex.Message}",
                    "OK"
                );
            }
        }

        public async void SendMessage(string address, params object[] args)
        {
            try
            {
                // Construct the OSC message
                var message = new OscMessage(new Address(address), args);

                // Send the OSC message
                _udpClient.SendMessageAsync(message).Wait();
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        public void Close()
        {
            _udpClient?.Close();
            _udpClient = null;
        }
    }
}
