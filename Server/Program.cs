using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CaroServer
{
    class Player
    {
        public TcpClient? Client;
        public StreamReader? Reader;
        public StreamWriter? Writer;
        public string? Name;
        public Room? RoomRef;
    }

    class Room
    {
        public Player? P1;
        public Player? P2;
        public bool IsXTurn = true; // true = X, false = O
        private static Random rnd = new Random();

        public void Reset()
        {
            if (P1 != null && P2 != null)
            {
                // ---- [Sửa 1] Random chọn ai đi X trước ----
                if (rnd.Next(2) == 0)
                {
                    Send(P1, $"START X {P1.Name}");
                    Send(P2, $"START O {P1.Name}");
                    Send(P1, "TURN");
                    IsXTurn = true;
                }
                else
                {
                    Send(P1, $"START O {P2.Name}");
                    Send(P2, $"START X {P2.Name}");
                    Send(P2, "TURN");
                    IsXTurn = false;
                }
            }
        }

        private void Send(Player? p, string text)
        {
            if (p?.Writer == null) return;
            try { p.Writer.WriteLine(text); }
            catch { }
        }
    }

    class Program
    {
        static List<Player> waitingPlayers = new List<Player>();
        static List<Room> rooms = new List<Room>();
        static Dictionary<string, int> scores = new Dictionary<string, int>();

        static void Main()
        {
            Console.Title = "=== CARO SERVER ===";
            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Server running on port 5000...\n");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread t = new Thread(() => HandleClient(client));
                t.IsBackground = true;
                t.Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            Player p = new Player
            {
                Client = client,
                Reader = new StreamReader(client.GetStream(), Encoding.UTF8),
                Writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true }
            };

            Send(p, "HELLO");

            try
            {
                p.Name = p.Reader?.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(p.Name))
                    p.Name = "Unknown";

                Console.WriteLine($"Client joined: {p.Name}");
            }
            catch
            {
                client.Close();
                return;
            }

            // ---- [Sửa 2] Ghép phòng với lock ----
            lock (waitingPlayers)
            {
                waitingPlayers.Add(p);
                if (waitingPlayers.Count >= 2)
                {
                    Player p1 = waitingPlayers[0]!;
                    Player p2 = waitingPlayers[1]!;
                    waitingPlayers.RemoveRange(0, 2);

                    Room r = new Room { P1 = p1, P2 = p2 };
                    lock (rooms) { rooms.Add(r); } // ---- lock rooms ----
                    p1.RoomRef = r;
                    p2.RoomRef = r;

                    r.Reset();
                }
            }

            try
            {
                while (true)
                {
                    string? msg = p.Reader?.ReadLine();
                    if (string.IsNullOrEmpty(msg))
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    if (msg.StartsWith("MOVE"))
                    {
                        Room? r = p.RoomRef;
                        if (r?.P1 == null || r?.P2 == null) continue;

                        Player other = (r.P1 == p) ? r.P2! : r.P1!;
                        Send(other, msg);

                        r.IsXTurn = !r.IsXTurn;
                        Send(other, "TURN");
                    }
                    else if (msg.StartsWith("WIN"))
                    {
                        string winnerName = msg.Substring(4).Trim();
                        Room? r = p.RoomRef;
                        if (r?.P1 == null || r?.P2 == null) return;

                        if (!scores.ContainsKey(winnerName))
                            scores[winnerName] = 0;
                        scores[winnerName] += 1;

                        string log = $"{winnerName} thắng lúc {DateTime.Now} (Tổng thắng: {scores[winnerName]})";
                        File.AppendAllText("result.txt", log + Environment.NewLine);
                        Console.WriteLine($"SAVE RESULT => {log}");

                        // ---- [Sửa 3] WIN chỉ gửi 2 tham số ----
                        Send(r.P1, $"WIN {winnerName} {scores[winnerName]}");
                        Send(r.P2, $"WIN {winnerName} {scores[winnerName]}");

                        r.Reset();
                    }
                    else if (msg.StartsWith("CHAT"))
                    {
                        // ---- [Sửa 4] Chat gửi kèm tên người gửi ----
                        string chatText = msg.Substring(5).Trim(); // bỏ "CHAT "
                        string fullMsg = $"CHAT {p.Name}: {chatText}";

                        File.AppendAllText("chatlog.txt", $"{DateTime.Now:T} {p.Name}: {chatText}{Environment.NewLine}");
                        Console.WriteLine($"CHAT: {fullMsg}");

                        Room? r = p.RoomRef;
                        if (r?.P1 != null && r?.P2 != null)
                        {
                            Send(r.P1, fullMsg);
                            Send(r.P2, fullMsg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with {p.Name}: {ex.Message}");
            }
            finally
            {
                client.Close();

                // ---- [Sửa 5] Thông báo đối thủ nếu client disconnect ----
                lock (waitingPlayers)
                {
                    waitingPlayers.Remove(p);
                }

                Room? r = p.RoomRef;
                if (r != null)
                {
                    Player? other = (r.P1 == p) ? r.P2 : r.P1;
                    if (other != null)
                    {
                        Send(other, $"CHAT Hệ thống: Đối thủ {p.Name} đã ngắt kết nối.");
                    }
                    lock (rooms) { rooms.Remove(r); }
                }
            }
        }

        static void Send(Player? p, string text)
        {
            if (p?.Writer == null) return;
            try { p.Writer.WriteLine(text); }
            catch { }
        }
    }
}
