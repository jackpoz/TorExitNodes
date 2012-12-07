using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TorExitNodes
{
    class TorControlConnection : IDisposable
    {
        Socket socket;

        public TorControlConnection()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect()
        {
            socket.Connect("127.0.0.1", 9051);
            SendText("authenticate \"\"");
        }

        public void SetConf(string key, string value)
        {
            SendText("setconf " + key + "=" + value);
            SaveConf();
        }

        public void SaveConf()
        {
            SendText("saveconf");
        }

        public void SetExitNodes(string nodes)
        {
            SetConf("ExitNodes", nodes);
        }

        public void Dispose()
        {
            if (socket != null)
                socket.Dispose();
        }

        public void SendText(string text)
        {
            socket.Send(System.Text.Encoding.UTF8.GetBytes(text + "\r\n"));
        }
    }
}
