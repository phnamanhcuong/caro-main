using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CaroServer
{
    internal class Program
    {
        static TcpListener? server;
        static List<Player> waitingPlayers = new List<Player>();
        static List<GameRoom> rooms = new List<GameRoom>();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "🎮 Caro Server";
            int port = 5000;

            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"🚀 Server Caro đang chạy trên cổng {port}...");
            Console.WriteLine("⏳ Đang chờ người chơi kết nối...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread t = new Thread(() => HandleClient(client));
                t.IsBackground = true;
                t.Start();
            }
        }

        static void HandleClient(TcpClient tcpClient)
        {
            Player player = new Player(tcpClient);

            try
            {
                player.Writer.WriteLine("Nhập tên:");
                string? name = player.Reader.ReadLine();
                if (string.IsNullOrEmpty(name)) name = "Người chơi";
                player.Name = name;
                Console.WriteLine($"✅ {player.Name} đã kết nối.");

                lock (waitingPlayers)
                {
                    if (waitingPlayers.Count > 0)
                    {
                        Player other = waitingPlayers[0];
                        waitingPlayers.RemoveAt(0);

                        GameRoom room = new GameRoom(other, player);
                        rooms.Add(room);
                        room.Start();
                        Console.WriteLine($"🎯 Ghép cặp: {other.Name} vs {player.Name}");
                    }
                    else
                    {
                        waitingPlayers.Add(player);
                        player.Writer.WriteLine("⏳ Đang chờ đối thủ...");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi client: " + ex.Message);
            }
        }
    }

    class Player
    {
        public string Name = "Player";
        public TcpClient Client;
        public StreamReader Reader;
        public StreamWriter Writer;
        public GameRoom? Room;
        public char Symbol;

        public Player(TcpClient c)
        {
            Client = c;
            Reader = new StreamReader(c.GetStream(), Encoding.UTF8);
            Writer = new StreamWriter(c.GetStream(), Encoding.UTF8) { AutoFlush = true };
        }
    }

    class GameRoom
    {
        Player p1, p2;
        public GameRoom(Player a, Player b)
        {
            p1 = a; p2 = b;
            p1.Room = this; p2.Room = this;
        }

        public void Start()
        {
            p1.Symbol = 'X';
            p2.Symbol = 'O';

            p1.Writer.WriteLine($"ROOM X {p2.Name}");
            p2.Writer.WriteLine($"ROOM O {p1.Name}");

            p1.Writer.WriteLine("TURN"); // X đi trước

            Thread t1 = new Thread(() => Listen(p1));
            Thread t2 = new Thread(() => Listen(p2));
            t1.IsBackground = true;
            t2.IsBackground = true;
            t1.Start();
            t2.Start();
        }

        private void Listen(Player p)
        {
            try
            {
                while (true)
                {
                    string? msg = p.Reader.ReadLine();
                    if (msg == null) break;

                    if (msg.StartsWith("MOVE"))
                    {
                        // Gửi cho đối thủ
                        Player other = (p == p1) ? p2 : p1;
                        other.Writer.WriteLine(msg);
                        other.Writer.WriteLine("TURN");
                    }
                    else if (msg.StartsWith("WIN"))
                    {
                        string winner = msg.Substring(4).Trim();
                        p1.Writer.WriteLine("WIN " + winner);
                        p2.Writer.WriteLine("WIN " + winner);
                        Console.WriteLine($"🏆 Trận {p1.Name} vs {p2.Name}: {winner} thắng!");
                    }
                }
            }
            catch
            {
                // Ngắt kết nối
                Player other = (p == p1) ? p2 : p1;
                other.Writer.WriteLine("WIN " + other.Name);
                Console.WriteLine($"❌ {p.Name} ngắt kết nối. {other.Name} thắng!");
            }
        }
    }
}
