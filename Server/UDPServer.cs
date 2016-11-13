using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class UDPServer
    {
        private ushort tcpPort;
        private const ushort UDP_PORT = 7;
        private UdpClient udpClient;
        private byte[] response;
        public Thread ListenThrad { get; set; }
        public ushort TcpPort { get; set; }

        public UDPServer(ushort tcpPort)
        {
            SetUdpListener();
            this.tcpPort = tcpPort;
        }

        private void SetUdpListener()
        {
            try
            {
                udpClient = new UdpClient();
                
            }
            catch (SocketException)
            {
                Console.WriteLine("Blad podczas tworzenia socketu, prawdopodobnie port " + UDP_PORT + " moze byc zajety.");
                Console.WriteLine("Nacisnij dowolny klawisz, aby wyjsc z programu.");
                Console.ReadKey();
                System.Environment.Exit(1);
            }
        }

        internal void StartListening()
        {
            response = Encoding.ASCII.GetBytes("OFFER " + tcpPort);

            //watek odpowiedzialny za nasluchiwanie na udp 
            ThreadStart udpListenThreadRef = new ThreadStart(Listen);
            ListenThrad = new Thread(udpListenThreadRef);
            ListenThrad.Start();
        }

        private void Listen()
        {
            IPEndPoint clientEp = new IPEndPoint(IPAddress.Any, UDP_PORT);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));

            while(true)
            {
                Console.WriteLine("Nasluchiwanie na protokole UDP na porcie "+UDP_PORT + "...");
                byte[] clientRequestData = udpClient.Receive(ref clientEp);
                string clientRequest = Encoding.ASCII.GetString(clientRequestData);
                Console.WriteLine("Otrzymano {0} od {1}, wysylanie odpowiedzi", clientRequest, clientEp.Address.ToString());
                udpClient.Send(response, response.Length, clientEp);
            }
        }
    }
}
