using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    class Client
    {
        static Stopwatch stopWatch = new Stopwatch();
        static UdpClient broadcaster = new UdpClient();
        static IPEndPoint server = new IPEndPoint(IPAddress.Any, 0);
        static List<string> ipAddressList = new List<string>();
        static List<string> portList = new List<string>();
        static Random random = new Random();


        static void Main(string[] args)
        {
            string prevAddress;
            byte[] request = Encoding.ASCII.GetBytes("DISCOVER");
            bool attemptPrevConnection = false;
            int listSelection;

            //sprawdzanie, czy plik przechowujący ostatnie adresy istnieje
            prevAddress = GetPreviousServerAddress();

            //uzupełnianie listy adresów
            InitIpAddressAndPortLists(prevAddress);

            //zapytanie użytkownika o chęć połączenia z poprzednim serwerem
            attemptPrevConnection = ConnectToPreviousServer(prevAddress);

            //Obsluga poprzedniej decyzji usera
            while(true)
            {
                if(attemptPrevConnection)
                {
                    listSelection = 0;
                }
                else
                {
                    //wysylanie komunkatu az nie odpowie server
                    SendDiscovery(request);

                    Console.WriteLine("Znaleziono następujące serwery: ");

                    for(int i=0;i<ipAddressList.Count;i++)
                    {
                        Console.WriteLine("Address IP : "+ipAddressList[i]+" Port : "+portList[i]);
                    }

                    //wybranie serwera do polaczenia

                    listSelection = SelectAddressFromList();

                    Console.WriteLine(ipAddressList[listSelection] + ":"+portList[listSelection]);

                    SaveServerAddress(listSelection);
                }

                Console.WriteLine("Proba polaczenia z:"+ipAddressList[listSelection]+":"+portList[listSelection]);
                IPEndPoint remoteEp = new IPEndPoint(IPAddress.Parse((string)ipAddressList[listSelection]), ushort.Parse((string)portList[listSelection]));

                //Socket TCP/IP
                Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    ConnectToTcpServer(remoteEp, tcpSocket);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Nastapil blad podczas proby polaczenia z serwerem:");
                    Console.WriteLine(ipAddressList[listSelection] + ":" + portList[listSelection]);
                    attemptPrevConnection = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Nieoczekiwany wyjatek: {0}", e.ToString());
                }
            }

            
        }

        private static void ConnectToTcpServer(IPEndPoint remoteEp, Socket tcpSocket)
        {
            tcpSocket.Connect(remoteEp);

            //polaczenie z serwerem
            Console.WriteLine("Socket polaczyl sie z {0}",tcpSocket.RemoteEndPoint.ToString());
            string nickname;

            //przypomnienie ostatniego nicku
            GetLastUsedNick();

            //pobranie nicku
            Console.WriteLine("podaj nick");
            nickname = Console.ReadLine();

            //wysylanie nicku do serwera
            SendNickNameToTCPServer(tcpSocket, nickname);

            //odbior odpowiedzi od serwera
            nickname = CheckNickNameValidation(tcpSocket, nickname);

            SaveNickName(nickname);

            SendValueToServer(tcpSocket);

        }

        private static void SendValueToServer(Socket tcpSocket)
        {
            stopWatch.Start();

            while(true)
            {
                while(stopWatch.ElapsedMilliseconds>=2000)
                {
                    int value = random.Next(0, 101);
                    string message = "VALUE " + value;
                    byte[] byteValue = Encoding.ASCII.GetBytes(message);

                    Console.WriteLine("Wysylanie liczby "+value+" do serwera...");
                    tcpSocket.Send(byteValue);
                    stopWatch.Restart();
                }
            }
        }

        private static void SaveNickName(string nickname)
        {
            Console.WriteLine("Nick zostal zaakceptowany przez serwer.");
            StreamWriter file = new StreamWriter("nickname.txt");
            file.WriteLine(nickname);
            file.Close();
        }

        private static string CheckNickNameValidation(Socket tcpSocket, string nickname)
        {
            byte[] bytes = new byte[1024];
            int nickReply = tcpSocket.Receive(bytes);
            string nickReplyStr = Encoding.ASCII.GetString(bytes, 0, nickReply);

            //jezeli nick zostal odrzucony to user zostanie poproszony o podanie innego
            while(nickReplyStr!="NICK_OK")
            {
                Console.WriteLine("Nick zostal odrzucony przez server.Wprowadz inny");
                nickname = Console.ReadLine();

                SendNickNameToTCPServer(tcpSocket, nickname);

                bytes = new byte[1024];
                nickReply = tcpSocket.Receive(bytes);
                nickReplyStr = Encoding.ASCII.GetString(bytes, 0, nickReply);
            }

            return nickname;
        }

        private static void SendNickNameToTCPServer(Socket tcpSocket, string nickname)
        {
            byte[] nicknameMsg = Encoding.ASCII.GetBytes("NICK " + nickname);
            Console.WriteLine("Wysylanie nicku do serwera...");
            tcpSocket.Send(nicknameMsg);
        }

        private static void GetLastUsedNick()
        {
            string nickname;
            if (File.Exists("nickname.txt"))
            {
                try
                {
                    nickname = File.ReadLines("nickname.txt").Last();
                    Console.WriteLine("Ostatnio uzywany nick: "+nickname);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Wystapil problem podczas proby odczytu nicku z pliku");
                }
            }
        }

        private static void SaveServerAddress(int listSelection)
        {
            StreamWriter file = new StreamWriter("ipList.txt", true);
            file.WriteLine(ipAddressList[listSelection] + ":" + portList[listSelection]);
            file.Close();
        }

        private static int SelectAddressFromList()
        {
            int listSelection = ipAddressList.Count;
            bool isIndexCorrect;

            Console.WriteLine("Wprowadz numer pozycji, aby polaczyc sie z danym serwerem: ");

            do
            {
                isIndexCorrect = true;
                listSelection = ipAddressList.Count;
                string userInput = Console.ReadLine();

                try
                {
                    listSelection = int.Parse(userInput);
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentNullException || ex is OverflowException)
                {
                    isIndexCorrect = false;
                }

                if (listSelection >= ipAddressList.Count)
                {
                    isIndexCorrect = false;
                }

                if (!isIndexCorrect)
                {
                    Console.WriteLine("Blad, prosze wpisac prawidlowa wartosc");
                }

            } while (!isIndexCorrect);

            return listSelection;
        }

        private static void SendDiscovery(byte[] request)
        {
            while(ipAddressList.Count==0)
            {
                broadcaster.EnableBroadcast = true;
                Console.WriteLine("Wysylanie komunikatu DISCOVER...");
                broadcaster.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, 7));

                //Nasluchiwanie na odpowiedz ze strony serwerow
                Console.WriteLine("Oczekiwanie na odpowiedz serwerow...");
                stopWatch.Start();
                startListening();

                //oczekiwanie na odpowiedz
                Thread.Sleep(5000);
                stopWatch.Start();
                stopWatch.Reset();

                if(ipAddressList.Count==0)
                {
                    Console.WriteLine("Nie znaleziono zadnych serwerow");
                }

            }
        }

        //asynchroniczne otrzymywanie odpowiedz z serwerow
        private static void startListening()
        {
            broadcaster.BeginReceive(receive, new object());
        }

        private static void receive(IAsyncResult ar)
        {
            if(stopWatch.ElapsedMilliseconds<4000)
            {
                byte[] bytes = broadcaster.EndReceive(ar, ref server);
                string reply = Encoding.ASCII.GetString(bytes);

                //sprawdzanie czy serwer odeslal wlasciwy komunikat
                if(reply.Split(' ')[0]=="OFFER")
                {
                    ipAddressList.Add(server.Address.ToString());
                    portList.Add(reply.Split(' ')[1]);
                }
            }
            else
            {
                return;
            }
            startListening();
        }

        private static bool ConnectToPreviousServer(string prevAddress)
        {
            bool attemptPrevConnection = false;

            if(ipAddressList.Count!=0)
            {
                bool isSelectionValid;

                Console.WriteLine("Ostatnio laczono sie z serwerem : " + prevAddress);
                Console.WriteLine("Czy chcesz sprobowac polaczyc sie z serwerem? ([t]ak/[n]ie)");

                do
                {
                    isSelectionValid = true;
                    string userInput = Console.ReadLine();

                    if(userInput.ToLower() =="t" || userInput.ToLower() =="tak")
                    {
                        attemptPrevConnection = true;
                    }
                    else if(userInput.ToLower() == "n" || userInput.ToLower()=="nie")
                    {
                        attemptPrevConnection = false;
                        ipAddressList.Clear();
                    }
                    else
                    {
                        Console.WriteLine("BLAD, nalezy wpisac [t]ak lub [n]ie");
                        isSelectionValid = false;
                    }
                } while (!isSelectionValid);
            }

            return attemptPrevConnection;
            
        }

        private static void InitIpAddressAndPortLists(string prevAddress)
        {
            if(prevAddress != default(string))
            {
                if(prevAddress.IndexOf(':')!=-1)
                {
                    try
                    {
                        //IPAddress.Parse(prevAddress.Split(':')[0]);
                        //ushort port = ushort.Parse(prevAddress.Split('1')[0]);

                        ipAddressList.Add(prevAddress.Split(':')[0]);
                        portList.Add(prevAddress.Split(':')[1]);
                    }
                    catch (Exception ex ) when (ex is FormatException || ex is ArgumentNullException || ex is OverflowException)
                    {
                        Console.WriteLine("Wystapil problem podczas proby odczytu adresu serwera z pliku");
                    }
                }
                else
                {
                    Console.WriteLine("Wystapil problem podczas proby odczytu adresu serwera z pliku");
                }
            }
        }

        private static string GetPreviousServerAddress()
        {
            string prevAddress = default(string);

            if (File.Exists("ipList.txt"))
            {
                try
                {
                    prevAddress = File.ReadLines("ipList.txt").Last();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Wystapil blad podczas odczytu adresu serwera z pliku");
                }
            }

            return prevAddress;
        }
    }
}
