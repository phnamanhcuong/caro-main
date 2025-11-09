using System.Windows.Forms;
using System.Drawing;

namespace CaroClient
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelBoard;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.ComboBox comboBoxDifficulty;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.RichTextBox rtbChat;
        private System.Windows.Forms.ListBox lstHistory;
        private System.Windows.Forms.Label lblScore;
        private System.Windows.Forms.Label lblWinner;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnSend;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.panelBoard = new System.Windows.Forms.Panel();
            this.comboBoxMode = new System.Windows.Forms.ComboBox();
            this.comboBoxDifficulty = new System.Windows.Forms.ComboBox();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.rtbChat = new System.Windows.Forms.RichTextBox();
            this.lstHistory = new System.Windows.Forms.ListBox();
            this.lblScore = new System.Windows.Forms.Label();
            this.lblWinner = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();

            // ===================== Form =====================
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Text = "Caro Game";

            // ===================== panelBoard =====================
            this.panelBoard.Location = new System.Drawing.Point(10, 10);
            this.panelBoard.Size = new System.Drawing.Size(525, 525);
            this.panelBoard.BackColor = System.Drawing.Color.Beige;
            this.panelBoard.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelBoard_Paint);
            this.panelBoard.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PanelBoard_MouseClick);
            this.Controls.Add(this.panelBoard);

            // ===================== ComboBoxMode =====================
            this.comboBoxMode.Location = new System.Drawing.Point(550, 30);
            this.comboBoxMode.Size = new System.Drawing.Size(150, 25);
            this.comboBoxMode.Items.AddRange(new object[] { "Local 2P", "Máy", "Người" });
            this.Controls.Add(this.comboBoxMode);

            // ===================== ComboBoxDifficulty =====================
            this.comboBoxDifficulty.Location = new System.Drawing.Point(550, 70);
            this.comboBoxDifficulty.Size = new System.Drawing.Size(150, 25);
            this.comboBoxDifficulty.Items.AddRange(new object[] { "Dễ", "Trung bình", "Khó", "Rất khó" });
            this.Controls.Add(this.comboBoxDifficulty);

            // ===================== txtIP =====================
            this.txtIP.Location = new System.Drawing.Point(550, 110);
            this.txtIP.Size = new System.Drawing.Size(150, 25);
            this.txtIP.PlaceholderText = "Nhập IP Server";
            this.Controls.Add(this.txtIP);

            // ===================== lblScore =====================
            this.lblScore.Location = new System.Drawing.Point(550, 150);
            this.lblScore.Size = new System.Drawing.Size(200, 25);
            this.lblScore.Text = "Tỷ số: 0 - 0";
            this.Controls.Add(this.lblScore);

            // ===================== lblWinner =====================
            this.lblWinner.Location = new System.Drawing.Point(550, 180);
            this.lblWinner.Size = new System.Drawing.Size(200, 25);
            this.lblWinner.Text = "Người thắng: ";
            this.Controls.Add(this.lblWinner);

            // ===================== lstHistory =====================
            this.lstHistory.Location = new System.Drawing.Point(550, 210);
            this.lstHistory.Size = new System.Drawing.Size(300, 150);
            this.Controls.Add(this.lstHistory);

            // ===================== rtbChat =====================
            this.rtbChat.Location = new System.Drawing.Point(550, 370);
            this.rtbChat.Size = new System.Drawing.Size(300, 150);
            this.rtbChat.ReadOnly = true;
            this.Controls.Add(this.rtbChat);

            // ===================== txtMessage =====================
            this.txtMessage.Location = new System.Drawing.Point(550, 530);
            this.txtMessage.Size = new System.Drawing.Size(220, 25);
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtMessage_KeyDown);
            this.Controls.Add(this.txtMessage);

            // ===================== btnSend =====================
            this.btnSend.Location = new System.Drawing.Point(780, 530);
            this.btnSend.Size = new System.Drawing.Size(70, 25);
            this.btnSend.Text = "Gửi";
            this.btnSend.Click += new System.EventHandler(this.BtnSend_Click);
            this.Controls.Add(this.btnSend);

            // ===================== btnConnect =====================
            this.btnConnect.Location = new System.Drawing.Point(720, 30);
            this.btnConnect.Size = new System.Drawing.Size(120, 25);
            this.btnConnect.Text = "Kết nối / Bắt đầu";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            this.Controls.Add(this.btnConnect);

            // ===================== btnReset =====================
            this.btnReset.Location = new System.Drawing.Point(720, 70);
            this.btnReset.Size = new System.Drawing.Size(120, 25);
            this.btnReset.Text = "Đặt lại bàn";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            this.Controls.Add(this.btnReset);

            this.ResumeLayout(false);
        }
    }
}
