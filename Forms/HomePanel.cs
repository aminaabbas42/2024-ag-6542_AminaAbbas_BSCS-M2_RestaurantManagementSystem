using System;
using System.Drawing;
using System.Windows.Forms;

namespace RestaurantManagementSystem
{
    public class HomePanel : UserControl
    {
        public HomePanel()
        {
            InitializeComponents();
            LoadStats();
        }

        private Label lblTodayOrders, lblRevenue, lblTables, lblPendingOrders;
        private DataGridView dgvRecentOrders;

        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);

            var lblTitle = new Label
            {
                Text = "Dashboard Overview",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 60),
                Location = new Point(5, 10),
                Size = new Size(400, 40)
            };

            var lblSub = new Label
            {
                Text = $"Welcome back, {Models.Session.CurrentUser?.FullName}!  Today is {DateTime.Now:dddd, MMMM dd, yyyy}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 120, 150),
                Location = new Point(5, 50),
                Size = new Size(600, 22)
            };

            // Stat cards
            var card1 = CreateStatCard("Today's Orders", "0", Color.FromArgb(255, 140, 0), "📋", 5, 85, out lblTodayOrders);
            var card2 = CreateStatCard("Today's Revenue", "$0.00", Color.FromArgb(40, 180, 100), "💰", 235, 85, out lblRevenue);
            var card3 = CreateStatCard("Available Tables", "0/10", Color.FromArgb(50, 150, 255), "🪑", 465, 85, out lblTables);
            var card4 = CreateStatCard("Pending Orders", "0", Color.FromArgb(220, 80, 80), "⏳", 695, 85, out lblPendingOrders);

            var lblRecent = new Label
            {
                Text = "Recent Orders",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 60),
                Location = new Point(5, 240),
                Size = new Size(200, 30)
            };

            dgvRecentOrders = new DataGridView
            {
                Location = new Point(5, 275),
                Size = new Size(950, 380),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Color.FromArgb(230, 230, 240)
            };
            dgvRecentOrders.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42);
            dgvRecentOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRecentOrders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvRecentOrders.ColumnHeadersHeight = 36;
            dgvRecentOrders.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255);
            dgvRecentOrders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100);
            dgvRecentOrders.DefaultCellStyle.SelectionForeColor = Color.Black;

            this.Controls.AddRange(new Control[] { lblTitle, lblSub, card1, card2, card3, card4, lblRecent, dgvRecentOrders });
        }

        private Panel CreateStatCard(string title, string value, Color accent, string icon, int x, int y, out Label valueLabel)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(215, 115),
                BackColor = Color.White
            };
            panel.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(accent), 0, 0, 5, panel.Height);
                using var pen = new Pen(Color.FromArgb(230, 230, 240));
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var lIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 22),
                Location = new Point(155, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lTitle = new Label
            {
                Text = title.ToUpper(),
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 120, 150),
                Location = new Point(15, 20),
                Size = new Size(135, 20)
            };

            valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = accent,
                Location = new Point(13, 45),
                Size = new Size(180, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lSub = new Label
            {
                Text = "As of today",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(160, 160, 180),
                Location = new Point(15, 90),
                Size = new Size(135, 18)
            };

            panel.Controls.AddRange(new Control[] { lIcon, lTitle, valueLabel, lSub });
            return panel;
        }
          //try and catch
        private void LoadStats()
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");

                var ordersToday = DatabaseHelper.ExecuteScalar($"SELECT COUNT(*) FROM Orders WHERE DATE(OrderDate)='{today}'");
                lblTodayOrders.Text = ordersToday?.ToString() ?? "0";

                var revenue = DatabaseHelper.ExecuteScalar($"SELECT COALESCE(SUM(TotalAmount),0) FROM Orders WHERE DATE(OrderDate)='{today}' AND Status='Paid'");
                lblRevenue.Text = $"${Convert.ToDecimal(revenue):F2}";

                var available = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM RestaurantTables WHERE Status='Available'");
                var total = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM RestaurantTables");
                lblTables.Text = $"{available}/{total}";

                var pending = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Orders WHERE Status='Open'");
                lblPendingOrders.Text = pending?.ToString() ?? "0";

                // Load recent orders
                var dt = DatabaseHelper.ExecuteQuery(@"
                    SELECT o.OrderID, t.TableNumber, u.FullName AS Waiter,
                           o.OrderDate, o.Status, o.TotalAmount
                    FROM Orders o
                    LEFT JOIN RestaurantTables t ON o.TableID = t.TableID
                    LEFT JOIN Users u ON o.UserID = u.UserID
                    ORDER BY o.OrderDate DESC LIMIT 20");

                dgvRecentOrders.DataSource = dt;

                if (dgvRecentOrders.Columns.Count > 0)
                {
                    dgvRecentOrders.Columns["OrderID"].HeaderText = "Order #";
                    dgvRecentOrders.Columns["TableNumber"].HeaderText = "Table";
                    dgvRecentOrders.Columns["Waiter"].HeaderText = "Waiter";
                    dgvRecentOrders.Columns["OrderDate"].HeaderText = "Date & Time";
                    dgvRecentOrders.Columns["Status"].HeaderText = "Status";
                    dgvRecentOrders.Columns["TotalAmount"].HeaderText = "Total";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard: " + ex.Message);
            }
        }
    }
}
