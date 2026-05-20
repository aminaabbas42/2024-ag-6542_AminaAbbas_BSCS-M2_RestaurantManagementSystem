using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem
{
    public class LoginForm : Form
    {
        private Panel panelMain;
        private Label lblTitle, lblSubtitle, lblUsername, lblPassword, lblVersion;
        private TextBox txtUsername, txtPassword;
        private Button btnLogin, btnExit;
        private PictureBox picLogo;
        private Panel panelCard;

        public LoginForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Restaurant Management System - Login";
            this.Size = new Size(480, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(18, 18, 35);

            // Background gradient panel
            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 35)
            };

            // Card panel
            panelCard = new Panel
            {
                Size = new Size(380, 440),
                Location = new Point(50, 70),
                BackColor = Color.FromArgb(28, 28, 50),
                BorderStyle = BorderStyle.None
            };
            panelCard.Paint += (s, e) => {
                var g = e.Graphics;
                using var pen = new Pen(Color.FromArgb(255, 140, 0), 1);
                g.DrawRectangle(pen, 0, 0, panelCard.Width - 1, panelCard.Height - 1);
            };

            // Restaurant icon label
            var lblIcon = new Label
            {
                Text = "🍽",
                Font = new Font("Segoe UI Emoji", 36),
                ForeColor = Color.FromArgb(255, 140, 0),
                Location = new Point(145, 25),
                Size = new Size(90, 70),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblTitle = new Label
            {
                Text = "BISTRO MANAGER",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 0),
                Location = new Point(20, 100),
                Size = new Size(340, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblSubtitle = new Label
            {
                Text = "Restaurant Management System",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(160, 160, 200),
                Location = new Point(20, 135),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var sep = new Panel
            {
                Location = new Point(40, 165),
                Size = new Size(300, 1),
                BackColor = Color.FromArgb(255, 140, 0)
            };

            lblUsername = new Label
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 160, 200),
                Location = new Point(40, 185),
                Size = new Size(300, 18)
            };

            txtUsername = new TextBox
            {
                Location = new Point(40, 205),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(40, 40, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "admin"
            };

            lblPassword = new Label
            {
                Text = "PASSWORD",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 160, 200),
                Location = new Point(40, 250),
                Size = new Size(300, 18)
            };

            txtPassword = new TextBox
            {
                Location = new Point(40, 270),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(40, 40, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                Text = "admin123"
            };

            btnLogin = new Button
            {
                Text = "LOGIN",
                Location = new Point(40, 325),
                Size = new Size(300, 42),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            btnExit = new Button
            {
                Text = "EXIT",
                Location = new Point(40, 378),
                Size = new Size(300, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(80, 80, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Application.Exit();

            lblVersion = new Label
            {
                Text = "v1.0  |  Default: admin / admin123",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 100, 130),
                Location = new Point(0, 540),
                Size = new Size(480, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            panelCard.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSubtitle, sep, lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnExit });
            panelMain.Controls.Add(panelCard);
            panelMain.Controls.Add(lblVersion);
            this.Controls.Add(panelMain);

            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };
            txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPassword.Focus(); };
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hashedPassword = DatabaseHelper.HashPassword(password);
            string sql = "SELECT * FROM Users WHERE Username=@u AND Password=@p AND IsActive=1";
            var dt = DatabaseHelper.ExecuteQuery(sql, new System.Data.SQLite.SQLiteParameter[] {
                new("@u", username),
                new("@p", hashedPassword)
            });

            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                Session.CurrentUser = new User
                {
                    UserID = Convert.ToInt32(row["UserID"]),
                    Username = row["Username"].ToString(),
                    Role = row["Role"].ToString(),
                    FullName = row["FullName"].ToString()
                };

                this.Hide();
                var dashboard = new MainDashboard();
                dashboard.FormClosed += (s2, e2) => this.Close();
                dashboard.Show();
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }
}
