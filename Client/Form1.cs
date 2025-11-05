using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class Form1 : Form
    {
        const int BOARD_SIZE = 15;
        const int CELL_SIZE = 35;

        enum Cell { Empty, X, O }

        Cell[,] board = new Cell[BOARD_SIZE, BOARD_SIZE];
        bool myTurn = false;
        Cell mySymbol = Cell.X;

        TcpClient? client;
        StreamReader? reader;
        StreamWriter? writer;
        Thread? receiveThread;

        string playerName = "Player";

        // Thống kê
        int wins = 0;
        int losses = 0;

        // Lưu chế độ hiện tại
        string currentMode = "Local 2P";
        bool isAI = false;
        bool isOnline = false;

        Cell currentSymbol = Cell.X; // cho Local 2P
        Random rnd = new Random();

        ListBox lstHistory = new ListBox();

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;

            lstHistory.Location = new Point(BOARD_SIZE * CELL_SIZE + 80, 70);
            lstHistory.Size = new Size(220, 400);
            Controls.Add(lstHistory);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxMode.SelectedIndex = 0;
            comboBoxDifficulty.SelectedIndex = 0;
            ResetBoard();
        }

        private void ResetBoard()
        {
            for (int i = 0; i < BOARD_SIZE; i++)
                for (int j = 0; j < BOARD_SIZE; j++)
                    board[i, j] = Cell.Empty;

            myTurn = (currentMode == "Local 2P") ? true : false;
            currentSymbol = Cell.X;
            Invalidate();
        }

        // ======================== ONLINE CONNECT ========================
        private void ConnectServer(string ip, int port)
        {
            try
            {
                client = new TcpClient(ip, port);
                reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };

                writer.WriteLine(playerName);
                receiveThread = new Thread(ReceiveThread);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                MessageBox.Show("Đã kết nối server!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private void ReceiveThread()
        {
            try
            {
                while (true)
                {
                    string? msg = reader?.ReadLine();
                    if (msg == null) continue;

                    this.Invoke(new Action(() =>
                    {
                        if (msg.StartsWith("ROOM"))
                        {
                            string[] p = msg.Split(' ');
                            mySymbol = (p[1] == "X") ? Cell.X : Cell.O;
                            Text = $"Bạn: {playerName} ({mySymbol})";
                        }
                        else if (msg == "TURN")
                        {
                            myTurn = true;
                        }
                        else if (msg.StartsWith("MOVE"))
                        {
                            string[] s = msg.Split(' ');
                            int r = int.Parse(s[1]);
                            int c = int.Parse(s[2]);
                            Cell sym = (s[3] == "X") ? Cell.X : Cell.O;
                            board[r, c] = sym;
                            Invalidate();
                            if (sym != mySymbol) myTurn = true;
                        }
                        else if (msg.StartsWith("WIN"))
                        {
                            string winner = msg.Substring(4).Trim();
                            if (winner == playerName) wins++;
                            else losses++;
                            lstHistory.Items.Add($"{winner} thắng ({DateTime.Now:T})");
                            UpdateTitle();
                            ResetBoard();
                        }
                    }));
                }
            }
            catch { }
        }

        // ======================== DRAW BOARD ========================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawBoard(e.Graphics);
        }

        private void DrawBoard(Graphics g)
        {
            for (int i = 0; i <= BOARD_SIZE; i++)
            {
                g.DrawLine(Pens.Black, 50, 50 + i * CELL_SIZE, 50 + BOARD_SIZE * CELL_SIZE, 50 + i * CELL_SIZE);
                g.DrawLine(Pens.Black, 50 + i * CELL_SIZE, 50, 50 + i * CELL_SIZE, 50 + BOARD_SIZE * CELL_SIZE);
            }

            for (int i = 0; i < BOARD_SIZE; i++)
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (board[i, j] == Cell.X)
                        g.DrawString("X", new Font("Arial", 18, FontStyle.Bold), Brushes.Red,
                            50 + j * CELL_SIZE + 5, 50 + i * CELL_SIZE + 5);
                    else if (board[i, j] == Cell.O)
                        g.DrawString("O", new Font("Arial", 18, FontStyle.Bold), Brushes.Blue,
                            50 + j * CELL_SIZE + 5, 50 + i * CELL_SIZE + 5);
                }
        }

        // ======================== CLICK BOARD ========================
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            int col = (e.X - 50) / CELL_SIZE;
            int row = (e.Y - 50) / CELL_SIZE;
            if (row < 0 || col < 0 || row >= BOARD_SIZE || col >= BOARD_SIZE)
                return;

            if (currentMode == "Online")
            {
                if (!myTurn || writer == null) return;
                if (board[row, col] != Cell.Empty) return;

                board[row, col] = mySymbol;
                Invalidate();
                writer.WriteLine($"MOVE {row} {col} {mySymbol}");
                myTurn = false;

                if (CheckWin(row, col))
                {
                    writer.WriteLine($"WIN {playerName}");
                    wins++;
                    UpdateTitle();
                    ResetBoard();
                }
            }
            else if (currentMode == "Local 2P")
            {
                if (board[row, col] != Cell.Empty) return;
                board[row, col] = currentSymbol;
                Invalidate();

                if (CheckWin(row, col))
                {
                    string winner = (currentSymbol == Cell.X) ? "Người chơi X" : "Người chơi O";
                    MessageBox.Show($"{winner} thắng!");
                    if (currentSymbol == Cell.X) wins++; else losses++;
                    lstHistory.Items.Add($"{winner} thắng ({DateTime.Now:T})");
                    UpdateTitle();
                    ResetBoard();
                    return;
                }

                currentSymbol = (currentSymbol == Cell.X) ? Cell.O : Cell.X;
            }
            else if (currentMode == "AI")
            {
                if (!myTurn || board[row, col] != Cell.Empty) return;
                board[row, col] = Cell.X;
                Invalidate();

                if (CheckWin(row, col))
                {
                    MessageBox.Show("Bạn thắng!");
                    wins++;
                    UpdateTitle();
                    ResetBoard();
                    return;
                }

                myTurn = false;
                AITurn();
            }
        }

        // ======================== AI LOGIC ========================
        private void AITurn()
        {
            Thread.Sleep(400);
            int bestR = -1, bestC = -1;

            // AI tìm ô trống ngẫu nhiên gần nước đi trước
            for (int tries = 0; tries < 1000; tries++)
            {
                int r = rnd.Next(BOARD_SIZE);
                int c = rnd.Next(BOARD_SIZE);
                if (board[r, c] == Cell.Empty)
                {
                    bestR = r;
                    bestC = c;
                    break;
                }
            }

            if (bestR == -1) return;

            board[bestR, bestC] = Cell.O;
            Invalidate();

            if (CheckWin(bestR, bestC))
            {
                MessageBox.Show("AI thắng!");
                losses++;
                UpdateTitle();
                ResetBoard();
                return;
            }

            myTurn = true;
        }

        // ======================== UTILITIES ========================
        private bool CheckWin(int r, int c)
        {
            int[][] dirs = new int[][] {
                new int[]{0,1}, new int[]{1,0}, new int[]{1,1}, new int[]{1,-1}
            };
            foreach (var d in dirs)
            {
                int cnt = 1;
                cnt += Count(r, c, d[0], d[1]);
                cnt += Count(r, c, -d[0], -d[1]);
                if (cnt >= 5) return true;
            }
            return false;
        }

        private int Count(int r, int c, int dr, int dc)
        {
            int cnt = 0;
            Cell cur = board[r, c];
            int nr = r + dr, nc = c + dc;
            while (nr >= 0 && nc >= 0 && nr < BOARD_SIZE && nc < BOARD_SIZE && board[nr, nc] == cur)
            {
                cnt++;
                nr += dr;
                nc += dc;
            }
            return cnt;
        }

        private void UpdateTitle()
        {
            lblScore.Text = $"Tỷ số: {wins} - {losses}";
        }

        // ======================== BUTTONS ========================
        private void btnConnect_Click(object sender, EventArgs e)
        {
            currentMode = comboBoxMode.SelectedItem?.ToString() ?? "Local 2P";
            isAI = currentMode == "AI";
            isOnline = currentMode == "Online";
            ResetBoard();

            if (isOnline)
            {
                string ip = txtIP.Text.Trim();
                if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

                playerName = PromptName();
                ConnectServer(ip, 5000);
            }
            else if (isAI)
            {
                MessageBox.Show("Bắt đầu chơi với AI!");
                myTurn = true;
            }
            else
            {
                MessageBox.Show("Chế độ 2 người chơi trên cùng máy!");
            }
        }

        private string PromptName()
        {
            using (Form f = new Form())
            {
                f.Width = 250; f.Height = 150;
                f.Text = "Tên người chơi";

                TextBox t = new TextBox() { Left = 40, Top = 30, Width = 150 };
                Button ok = new Button() { Text = "OK", Left = 80, Top = 70, DialogResult = DialogResult.OK };
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.AcceptButton = ok;

                return (f.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(t.Text))
                    ? t.Text : "Player";
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetBoard();
        }
    }
}
