using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Server
    {
        static ushort tcpPort;
        static List<string> localIpListForTcp = new List<string>();
        static bool isPortValid;

        static void Main(string[] args)
        {
            GetIpList();

            GetTcpPort();

            UDPServer udpServer = new UDPServer(tcpPort);
            TCPServer tcpServer = new TCPServer(tcpPort, localIpListForTcp);

            //odpowiedz od klienta z komunikatem i nr portu
            udpServer.StartListening();
            
            //Tworzenie socketow i watkow dla nasluchu TCP
            tcpServer.StartTCPListening();

            udpServer.ListenThrad.Join();
        }

        private static void GetTcpPort()
        {
            Console.WriteLine("Podaj port");
            do
            {
                string tcpPortString = Console.ReadLine();
                isPortValid = CheckIfPortIsValid(tcpPortString);
            } while (!isPortValid);
        }

        private static bool CheckIfPortIsValid(string tcpPortString)
        {
            bool isPortValid = true;
            try
            {
                tcpPort = ushort.Parse(tcpPortString);
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentNullException || ex is OverflowException)
            {
                Console.WriteLine("Nastapil blad przy wprowadzaniu portu. Sprobuj ponownie:");
                isPortValid = false;
            }

            return isPortValid;
        }

        private static void GetIpList()
        {
            IPAddress[] IpA = Dns.GetHostAddresses(Dns.GetHostName());

            foreach(var ip in IpA)
            {
                if(ip.AddressFamily==AddressFamily.InterNetwork)
                {
                    localIpListForTcp.Add(ip.ToString());
                }
            }

            localIpListForTcp.Add("127.0.0.1");
        }
    }
}
