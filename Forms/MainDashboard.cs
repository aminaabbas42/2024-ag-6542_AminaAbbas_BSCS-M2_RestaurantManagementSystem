using System;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem
{
    public class MainDashboard : Form
    {
        private Panel panelSidebar, panelContent, panelHeader;
        private Label lblRestaurantName, lblUserInfo, lblDateTime;
        private Button btnOrders, btnMenu, btnTables, btnPayments, btnInventory, btnReports, btnUsers, btnLogout;
        private Panel activePanel;
        private Timer clockTimer;

        public MainDashboard()
        {
            InitializeComponents();
            LoadDashboardStats();
            StartClock();
        }

        private void InitializeComponents()
        {
            this.Text = "Bistro Manager - Dashboard";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 700);
            this.BackColor = Color.FromArgb(245, 245, 250);

            // Header
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(18, 18, 35)
            };
            panelHeader.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 0)), 0, panelHeader.Height - 3, panelHeader.Width, 3);
            };

            lblRestaurantName = new Label
            {
                Text = "🍽  BISTRO MANAGER",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 0),
                Location = new Point(220, 12),
                Size = new Size(350, 36),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblUserInfo = new Label
            {
                Text = $"👤  {Session.CurrentUser?.FullName}  |  {Session.CurrentUser?.Role}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 180, 220),
                Location = new Point(750, 15),
                Size = new Size(280, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("ddd, MMM dd  HH:mm"),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(255, 140, 0),
                Location = new Point(1040, 15),
                Size = new Size(220, 30),
                TextAlign = ContentAlignment.MiddleRight
            };

            panelHeader.Controls.AddRange(new Control[] { lblRestaurantName, lblUserInfo, lblDateTime });

            // Sidebar
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(22, 22, 42)
            };

            var sidebarTitle = new Label
            {
                Text = "NAVIGATION",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 140),
                Location = new Point(0, 20),
                Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelSidebar.Controls.Add(sidebarTitle);

            int btnY = 50;
            btnOrders = CreateNavButton("📋  Orders", btnY); btnY += 55;
            btnMenu = CreateNavButton("🍕  Menu Items", btnY); btnY += 55;
            btnTables = CreateNavButton("🪑  Tables", btnY); btnY += 55;
            btnPayments = CreateNavButton("💳  Payments", btnY); btnY += 55;
            btnInventory = CreateNavButton("📦  Inventory", btnY); btnY += 55;
            btnReports = CreateNavButton("📊  Reports", btnY); btnY += 55;

            if (Session.IsAdmin)
            {
                btnUsers = CreateNavButton("👥  Users", btnY); btnY += 55;
                panelSidebar.Controls.Add(btnUsers);
                btnUsers.Click += (s, e) => LoadPanel(new UsersForm());
            }

            btnLogout = new Button
            {
                Text = "🚪  Logout",
                Size = new Size(180, 42),
                Location = new Point(10, 680),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click;

            panelSidebar.Controls.AddRange(new Control[] { sidebarTitle, btnOrders, btnMenu, btnTables, btnPayments, btnInventory, btnReports, btnLogout });

            // Content area
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 250),
                Padding = new Padding(15)
            };

            this.Controls.Add(panelContent);
            this.Controls.Add(panelSidebar);
            this.Controls.Add(panelHeader);

            // Wire up nav buttons
            btnOrders.Click += (s, e) => LoadPanel(new OrdersForm());
            btnMenu.Click += (s, e) => LoadPanel(new MenuForm());
            btnTables.Click += (s, e) => LoadPanel(new TablesForm());
            btnPayments.Click += (s, e) => LoadPanel(new PaymentsForm());
            btnInventory.Click += (s, e) => LoadPanel(new InventoryForm());
            btnReports.Click += (s, e) => LoadPanel(new ReportsForm());

            // Load dashboard home
            LoadPanel(new HomePanel());
        }

        private Button CreateNavButton(string text, int y)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(180, 46),
                Location = new Point(10, y),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(32, 32, 58),
                ForeColor = Color.FromArgb(180, 180, 220),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(255, 140, 0); btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.FromArgb(32, 32, 58); btn.ForeColor = Color.FromArgb(180, 180, 220); };
            return btn;
        }

        private void LoadPanel(UserControl panel)
        {
            panelContent.Controls.Clear();
            panel.Dock = DockStyle.Fill;
            panelContent.Controls.Add(panel);
        }

        private void LoadDashboardStats() { }

        private void StartClock()
        {
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => lblDateTime.Text = DateTime.Now.ToString("ddd, MMM dd  HH:mm:ss");
            clockTimer.Start();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Session.CurrentUser = null;
                clockTimer?.Stop();
                Application.Restart();
            }
        }
    }
}
