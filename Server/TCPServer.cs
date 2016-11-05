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
    public class TCPServer
    {
        private List<string> localIpListForTcp;
        private ushort tcpPort;
        private List<string> nickNameList;

        public byte[] DataBytesReceived { get; set; }
        public string ClientNick { get; set; }

        public TCPServer(ushort tcpPort, List<string> localIpListForTcp)
        {
            this.tcpPort = tcpPort;
            this.localIpListForTcp = localIpListForTcp;
            nickNameList = new List<string>();
            DataBytesReceived = new byte[1024];
        }

        internal void StartTCPListening()
        {
            foreach(var ip in localIpListForTcp)
            {
                Thread ListenThread = new Thread(() => Listen(ip));
                ListenThread.Start();
            }
        }

        private void Listen(string ip)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Parse(ip), tcpPort));

            //Ciagly nasluch na protokole TCP
            while(true)
            {
                Console.WriteLine("Nasluchiwanie po protokole TCP na adresie "+((IPEndPoint)socket.LocalEndPoint).Address.ToString()+" : "+tcpPort+" ...");

                //limit oczekujących polaczen
                socket.Listen(5);
                Socket handler = socket.Accept();

                //Tworzenie nowych watkow obslugujacych polaczenie TCP
                Thread tcpThread = new Thread(() => tcpProcessing(handler));
                tcpThread.Start();
            }
        }

        private void tcpProcessing(Socket handler)
        {
            try
            {
                Console.WriteLine("Polaczono z {0}", handler.RemoteEndPoint.ToString());
                ClientNick = GetClientNick(handler);

                //po zalogowaniu sie uzytkownika na serwerze, natepuje odbior przeslanych danych
                while (true)
                {
                    string response = "";
                    response = GetResponseFromClient(handler);

                    //sprawdzanie poprawnosci otrzymanego komunikatu
                    ValidateResponse(ClientNick, response);
                }

            }
            catch (SocketException)
            {
                DisconnectClient(ClientNick);
            }
        }

        private void ValidateResponse(string clientNick, string response)
        {
            if (response.Split(' ')[0] == "VALUE")
            {
                int value;
                try
                {
                    value = int.Parse(response.Split(' ')[1]);
                    Console.WriteLine("NICK: {0} , biezaca wartosc: {1}",clientNick,value);
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentNullException || ex is OverflowException)
                {
                    Console.WriteLine("Blad podczas otrzymywania liczby od: "+clientNick);
                }
            }
        }

        private void DisconnectClient(string clientNick)
        {
            if (clientNick != "")
            {
                nickNameList.Remove(clientNick);
                int counter = 0;

                while (counter < 5)
                {
                    Console.WriteLine(clientNick + " -> DISCONNECTED");
                    Thread.Sleep(1000);
                    counter++;
                }
            }
            else
            {
                Console.WriteLine("BLAD POLACZENIA");
            }
        }

        private string GetClientNick(Socket handler)
        {
            bool isClientNickValid = true;
            string clientNick="";

            //sprawdzanie poprawnosci nicku
            while(isClientNickValid)
            {
                string dataReceived;
                string nick="";

                //odbior danych nadeslanych przez klienta
                dataReceived = GetResponseFromClient(handler);

                //sprawdzanie czy komunikat ma odpowiedni format
                if (dataReceived.Split(' ')[0] == "NICK")
                {
                    //jezeli tak, odbior dalszej czesci po NICK
                    for(int i=1;i<dataReceived.Split(' ').Length;i++)
                    {
                        nick += dataReceived.Split(' ')[i];
                    }

                    //sprawdzanie czy nick nie jest pusty
                    CheckNickValidation(ref isClientNickValid, ref clientNick, nick);

                    //jezeli nick byl poprawny to wysylamy komunikat do klienta
                    if(isClientNickValid)
                    {
                        SendMessageToClient(handler, "NICK_OK");
                        Console.WriteLine("Uzytkownik "+clientNick+" podlaczyl sie do serwera");
                    }
                    else
                    {
                        SendMessageToClient(handler, "NICK_ERROR");
                    }
                }
                else
                {
                    isClientNickValid = false;
                    SendMessageToClient(handler, "NICK_ERROR");
                }
            }

            return clientNick;

        }

        private void SendMessageToClient(Socket handler, string v)
        {
            byte[] msg = Encoding.ASCII.GetBytes(v);
            handler.Send(msg);
        }

        private void CheckNickValidation(ref bool isClientNickValid, ref string clientNick, string nick)
        {
            if(nick==String.Empty)
            {
                isClientNickValid = false;
            }
            else
            {
                if(nickNameList.Contains(nick))
                {
                    isClientNickValid = false;
                }
                else
                {
                    nickNameList.Add(nick);
                    clientNick = nick;
                }
            }
        }

        private string GetResponseFromClient(Socket handler)
        {
            string dataReceived="";

            while(true)
            {
                int bytesRec = handler.Receive(DataBytesReceived);
                dataReceived += Encoding.ASCII.GetString(DataBytesReceived, 0, bytesRec);

                if (dataReceived.Length > 0)
                {
                    break;
                }
                 
            }

            return dataReceived;
        }
    }
}
