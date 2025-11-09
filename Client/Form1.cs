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
        bool myTurn = true;
        Cell mySymbol = Cell.X;
        Cell currentSymbol = Cell.X;

        // Online
        TcpClient? client;
        StreamReader? reader;
        StreamWriter? writer;
        Thread? receiveThread;

        string playerName = "Người chơi";

        int wins = 0;
        int losses = 0;

        string currentMode = "Local 2P";
        bool isAI = false;
        bool isOnline = false;

        Random rnd = new Random();

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
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

            myTurn = true;
            currentSymbol = Cell.X;
            panelBoard.Invalidate();
        }

        // ======================= DRAW BOARD =======================
        private void PanelBoard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Beige);

            // Vẽ lưới
            for (int i = 0; i <= BOARD_SIZE; i++)
            {
                g.DrawLine(Pens.Black, 0, i * CELL_SIZE, BOARD_SIZE * CELL_SIZE, i * CELL_SIZE);
                g.DrawLine(Pens.Black, i * CELL_SIZE, 0, i * CELL_SIZE, BOARD_SIZE * CELL_SIZE);
            }

            // Vẽ X,O
            for (int r = 0; r < BOARD_SIZE; r++)
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (board[r, c] == Cell.X)
                        g.DrawString("X", new Font("Arial", 18, FontStyle.Bold), Brushes.Red, c * CELL_SIZE + 5, r * CELL_SIZE + 5);
                    else if (board[r, c] == Cell.O)
                        g.DrawString("O", new Font("Arial", 18, FontStyle.Bold), Brushes.Blue, c * CELL_SIZE + 5, r * CELL_SIZE + 5);
                }
        }

        // ======================= CLICK BOARD =======================
        private void PanelBoard_MouseClick(object sender, MouseEventArgs e)
        {
            int col = e.X / CELL_SIZE;
            int row = e.Y / CELL_SIZE;

            if (row < 0 || col < 0 || row >= BOARD_SIZE || col >= BOARD_SIZE)
                return;

            if (board[row, col] != Cell.Empty) return;

            if (currentMode == "Local 2P")
            {
                board[row, col] = currentSymbol;
                if (CheckWin(row, col))
                {
                    string winner = (currentSymbol == Cell.X) ? "Người chơi X" : "Người chơi O";
                    MessageBox.Show($"{winner} thắng!");
                    if (currentSymbol == Cell.X) wins++; else losses++;
                    lstHistory.Items.Add($"{winner} thắng ({DateTime.Now:T})");
                    UpdateScore();
                    ResetBoard();
                    return;
                }
                currentSymbol = (currentSymbol == Cell.X) ? Cell.O : Cell.X;
            }
            else if (currentMode == "Máy")
            {
                if (!myTurn) return;
                board[row, col] = Cell.X;
                if (CheckWin(row, col))
                {
                    MessageBox.Show("Bạn thắng!");
                    wins++;
                    UpdateScore();
                    ResetBoard();
                    return;
                }
                myTurn = false;
                panelBoard.Invalidate();
                AITurn();
            }
            else if (currentMode == "Người")
            {
                if (!myTurn || writer == null) return;
                board[row, col] = mySymbol;
                panelBoard.Invalidate();
                writer.WriteLine($"MOVE {row} {col} {mySymbol}");
                myTurn = false;
                if (CheckWin(row, col))
                {
                    writer.WriteLine($"WIN {playerName}");
                }
            }

            panelBoard.Invalidate();
        }

        // ======================= AI =======================
        private void AITurn()
        {
            Thread.Sleep(300);
            (int r, int c) = FindBestMove();
            if (r == -1) { myTurn = true; return; }

            board[r, c] = Cell.O;
            if (CheckWin(r, c))
            {
                MessageBox.Show("Máy thắng!");
                losses++;
                UpdateScore();
                ResetBoard();
                return;
            }
            myTurn = true;
            panelBoard.Invalidate();
        }

        private (int, int) FindBestMove()
        {
            // AI đơn giản nhưng hiệu quả: tấn công + block
            int bestScore = -1;
            int bestR = -1, bestC = -1;

            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (board[r, c] != Cell.Empty) continue;
                    int score = EvaluateMove(r, c, Cell.O);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestR = r;
                        bestC = c;
                    }
                }
            }

            return (bestR, bestC);
        }

        private int EvaluateMove(int r, int c, Cell player)
        {
            int score = 0;
            int[][] dirs = new int[][] { new int[] {0,1}, new int[]{1,0}, new int[]{1,1}, new int[]{1,-1} };

            foreach (var d in dirs)
            {
                int line = 1 + Count(r, c, d[0], d[1], player) + Count(r, c, -d[0], -d[1], player);
                score = Math.Max(score, line);
            }

            // Block người chơi
            int playerLine = 1 + Count(r, c, 0, 1, Cell.X) + Count(r, c, 0, -1, Cell.X);
            playerLine = Math.Max(playerLine, 1 + Count(r, c, 1, 0, Cell.X) + Count(r, c, -1, 0, Cell.X));
            playerLine = Math.Max(playerLine, 1 + Count(r, c, 1, 1, Cell.X) + Count(r, c, -1, -1, Cell.X));
            playerLine = Math.Max(playerLine, 1 + Count(r, c, 1, -1, Cell.X) + Count(r, c, -1, 1, Cell.X));

            score += playerLine * 2; // ưu tiên block người chơi
            return score;
        }

        private int Count(int r, int c, int dr, int dc, Cell player)
        {
            int cnt = 0;
            int nr = r + dr, nc = c + dc;
            while (nr >= 0 && nc >= 0 && nr < BOARD_SIZE && nc < BOARD_SIZE && board[nr, nc] == player)
            {
                cnt++;
                nr += dr; nc += dc;
            }
            return cnt;
        }

        // ======================= Check Win =======================
        private bool CheckWin(int r, int c)
        {
            int[][] dirs = new int[][] { new int[]{0,1}, new int[]{1,0}, new int[]{1,1}, new int[]{1,-1} };
            foreach (var d in dirs)
            {
                int cnt = 1;
                cnt += Count(r, c, d[0], d[1], board[r,c]);
                cnt += Count(r, c, -d[0], -d[1], board[r,c]);
                if (cnt >= 5) return true;
            }
            return false;
        }

        private void UpdateScore()
        {
            lblScore.Text = $"Tỷ số: {wins} - {losses}";
        }

        // ======================= CHAT =======================
        private void BtnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text)) return;
            string msg = txtMessage.Text.Trim();
            if (isOnline && writer != null)
            {
                writer.WriteLine($"CHAT {msg}");
            }
            rtbChat.AppendText($"Bạn: {msg}{Environment.NewLine}");
            txtMessage.Clear();
        }

        // ======================= BUTTON =======================
        private void btnConnect_Click(object sender, EventArgs e)
        {
            currentMode = comboBoxMode.SelectedItem?.ToString() ?? "Local 2P";
            isAI = currentMode == "Máy";
            isOnline = currentMode == "Người";
            ResetBoard();

            if (isAI)
            {
                MessageBox.Show("Bắt đầu chơi với Máy!");
                myTurn = true;
            }
            else if (currentMode == "Local 2P")
            {
                MessageBox.Show("Chế độ 2 người chơi trên cùng máy!");
            }
            else if (isOnline)
            {
                string ip = txtIP.Text.Trim();
                if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

                playerName = PromptName();
                ConnectServer(ip, 5000);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetBoard();
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

                return (f.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(t.Text)) ? t.Text : "Người chơi";
            }
        }

        // ======================= ONLINE =======================
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
                        if (msg.StartsWith("START"))
                        {
                            string[] p = msg.Split(' ');
                            string symbol = p[1];
                            string firstPlayer = p[2];

                            mySymbol = (symbol == "X") ? Cell.X : Cell.O;
                            Text = $"Bạn: {playerName} ({mySymbol})";

                            myTurn = (firstPlayer == playerName);
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
                            panelBoard.Invalidate();
                            if (sym != mySymbol) myTurn = true;
                        }
                        else if (msg.StartsWith("WIN"))
                        {
                            string[] parts = msg.Split(' ');
                            string winner = parts[1];
                            lblWinner.Text = $"Người thắng: {winner}";
                            lstHistory.Items.Add($"{winner} thắng ({DateTime.Now:T})");
                            ResetBoard();
                        }
                        else if (msg.StartsWith("CHAT"))
                        {
                            string chat = msg.Substring(5);
                            rtbChat.AppendText($"{chat}{Environment.NewLine}");
                        }
                    }));
                }
            }
            catch { }
        }
    }
}
