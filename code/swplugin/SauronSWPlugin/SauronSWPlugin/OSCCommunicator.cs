using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Bespoke.Common.Osc;
using Transmitter;

namespace SauronSWPlugin
{
    class OSCCommunicator
    {
        private static readonly int SENDPORT = 5001;
        private static readonly int RECEIVEPORT = 5002;
        IPEndPoint solidworksPlugin;
        IPEndPoint openFrameworks;
        OscServer server;

        public delegate void MessageReceivedCallback(String addressedTo);
        MessageReceivedCallback mrc;

        public OSCCommunicator(MessageReceivedCallback messageReceivedCallback)
        {
            initOSC(messageReceivedCallback);
        }

        private void initOSC(MessageReceivedCallback messageReceivedCallback)
        {
            this.mrc = messageReceivedCallback;

            solidworksPlugin = new IPEndPoint(IPAddress.Loopback, SENDPORT);
            openFrameworks = new IPEndPoint(IPAddress.Loopback, SENDPORT);
            server = new OscServer(TransportType.Udp, IPAddress.Loopback, RECEIVEPORT);
            server.FilterRegisteredMethods = false;
            server.MessageReceived += new EventHandler<OscMessageReceivedEventArgs>(receivedMessage);
            server.ConsumeParsingExceptions = false;
            server.Start();
        }

        public void sendMessage(String address, String data)
        {
            OscBundle bundle = new OscBundle(solidworksPlugin);
            OscMessage message = new OscMessage(solidworksPlugin, address, data);
            bundle.Append(message);
            bundle.Send(openFrameworks);
        }

        private void receivedMessage(object sender, OscMessageReceivedEventArgs e)
        {
            OscMessage message = e.Message;
            string address = message.Address;
            //string data = message.Data[0];

            this.mrc(address);
        }
    }
}
