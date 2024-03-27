using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TicTacToeServer
{
    public class Program
    {
        static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static readonly List<Socket> clientSockets = new List<Socket>();
        const int BUFFER_SIZE = 1000000;
        const int PORT = 27001;
        readonly static byte[] buffer = new byte[BUFFER_SIZE];
        public static bool IsFirst { get; set; } = false;
        static char[,] Points = new char[3, 3] { { '1', '2', '3' }, { '4', '5', '6' }, { '7', '8', '9' } };

        public static void Start()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();
        }

        private static void CloseAllSockets()
        {
            foreach (var socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server  . . .");
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse("192.168.1.69"), PORT));
            serverSocket.Listen(2);
            while (true)
            {
                serverSocket.BeginAccept(AcceptCallBack, null);
            }
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            Socket socket = null;
            try
            {
                socket = serverSocket.EndAccept(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            clientSockets.Add(socket);
            Console.WriteLine($"{socket.RemoteEndPoint} connected");

            string t = "";
            if (!IsFirst)
            {
                IsFirst = true;
                t = "X";
            }
            else
            {
                IsFirst = false;
                t = "O";
            }

            byte[] data = Encoding.ASCII.GetBytes(t);
            socket.Send(data);

            NotifyGameStart();

            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket current = (Socket)ar.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client forcefully disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            try
            {
                Console.WriteLine($"Received: {text}");

                SendGameBoardToAllClients();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private static void SendGameBoardToAllClients()
        {
            var mydata = ConvertingString(Points);
            byte[] data = Encoding.ASCII.GetBytes(mydata);
            foreach (var item in clientSockets)
            {
                item.Send(data);
                Console.WriteLine($"Data sent to {item.RemoteEndPoint}");
            }
        }

        private static void NotifyGameStart()
        {
            if (clientSockets.Count == 2)
            {
                byte[] startMessage = Encoding.ASCII.GetBytes(" The game has started!");
                foreach (var clientSocket in clientSockets)
                {
                    clientSocket.Send(startMessage);
                }
            }
        }

        private static string ConvertingString(char[,] points)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    sb.Append(points[i, k]);
                    sb.Append("\t");
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            Start();
        }
    }
}
