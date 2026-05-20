using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem
{
    public class OrdersForm : UserControl
    {
        private DataGridView dgvOrders;
        private Button btnNew, btnView, btnClose, btnRefresh;
        private ComboBox cmbFilter;
        private TextBox txtSearch;
        private Label lblCount;

        public OrdersForm()
        {
            InitializeComponents();
            LoadOrders();
        }

        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);

            var lblTitle = new Label
            {
                Text = "Order Management",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 60),
                Location = new Point(5, 10),
                Size = new Size(350, 40)
            };

            // Toolbar
            var toolbar = new Panel
            {
                Location = new Point(5, 60),
                Size = new Size(960, 50),
                BackColor = Color.White
            };
            toolbar.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(230, 230, 240)), 0, 0, toolbar.Width - 1, toolbar.Height - 1);

            btnNew = CreateButton("+ New Order", Color.FromArgb(255, 140, 0), 10, 8);
            btnView = CreateButton("View Details", Color.FromArgb(50, 150, 255), 145, 8);
            btnClose = CreateButton("Close Order", Color.FromArgb(220, 80, 80), 280, 8);
            btnRefresh = CreateButton("↻ Refresh", Color.FromArgb(80, 80, 100), 415, 8);

            var lblFilter = new Label { Text = "Filter:", Location = new Point(560, 15), Size = new Size(45, 22), Font = new Font("Segoe UI", 9) };
            cmbFilter = new ComboBox
            {
                Location = new Point(605, 10),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilter.Items.AddRange(new[] { "All", "Open", "Paid", "Cancelled" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => LoadOrders();

            txtSearch = new TextBox
            {
                Location = new Point(730, 12),
                Size = new Size(120, 26),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Search..."
            };
            txtSearch.TextChanged += (s, e) => LoadOrders();

            lblCount = new Label
            {
                Text = "0 orders",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(865, 15),
                Size = new Size(80, 22)
            };

            toolbar.Controls.AddRange(new Control[] { btnNew, btnView, btnClose, btnRefresh, lblFilter, cmbFilter, txtSearch, lblCount });

            dgvOrders = CreateGrid(5, 120, 960, 540);

            this.Controls.AddRange(new Control[] { lblTitle, toolbar, dgvOrders });

            btnNew.Click += BtnNew_Click;
            btnView.Click += BtnView_Click;
            btnClose.Click += BtnClose_Click;
            btnRefresh.Click += (s, e) => LoadOrders();
        }

        private Button CreateButton(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(125, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private DataGridView CreateGrid(int x, int y, int w, int h)
        {
            var dgv = new DataGridView
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Color.FromArgb(230, 230, 240),
                MultiSelect = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            return dgv;
        }

        private void LoadOrders()
        {
            string filter = cmbFilter.SelectedItem?.ToString() ?? "All";
            string search = txtSearch.Text.Trim();

            string sql = @"SELECT o.OrderID AS 'Order #', t.TableNumber AS 'Table', 
                          u.FullName AS 'Waiter', o.OrderDate AS 'Date/Time',
                          o.Status, o.TotalAmount AS 'Total ($)', o.Notes
                          FROM Orders o
                          LEFT JOIN RestaurantTables t ON o.TableID=t.TableID
                          LEFT JOIN Users u ON o.UserID=u.UserID
                          WHERE 1=1";

            if (filter != "All") sql += $" AND o.Status='{filter}'";
            if (!string.IsNullOrEmpty(search)) sql += $" AND (CAST(o.OrderID AS TEXT) LIKE '%{search}%' OR u.FullName LIKE '%{search}%')";
            sql += " ORDER BY o.OrderDate DESC";

            var dt = DatabaseHelper.ExecuteQuery(sql);
            dgvOrders.DataSource = dt;
            lblCount.Text = $"{dt.Rows.Count} orders";

            // Color code status
            dgvOrders.CellFormatting += (s, e) => {
                if (dgvOrders.Columns[e.ColumnIndex].HeaderText == "Status" && e.Value != null)
                {
                    e.CellStyle.ForeColor = e.Value.ToString() switch
                    {
                        "Open" => Color.FromArgb(50, 150, 255),
                        "Paid" => Color.FromArgb(40, 180, 100),
                        "Cancelled" => Color.FromArgb(220, 80, 80),
                        _ => Color.Black
                    };
                    e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                }
            };
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new NewOrderForm();
            if (form.ShowDialog() == DialogResult.OK)
                LoadOrders();
        }

        private void BtnView_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) { MessageBox.Show("Select an order."); return; }
            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["Order #"].Value);
            new OrderDetailsForm(orderId).ShowDialog();
            LoadOrders();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) { MessageBox.Show("Select an order."); return; }
            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["Order #"].Value);
            string status = dgvOrders.SelectedRows[0].Cells["Status"].Value.ToString();
            if (status != "Open") { MessageBox.Show("Only open orders can be closed."); return; }

            if (MessageBox.Show("Cancel this order?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                DatabaseHelper.ExecuteNonQuery("UPDATE Orders SET Status='Cancelled' WHERE OrderID=@id",
                    new SQLiteParameter[] { new("@id", orderId) });
                DatabaseHelper.ExecuteNonQuery("UPDATE RestaurantTables SET Status='Available' WHERE TableID=(SELECT TableID FROM Orders WHERE OrderID=@id)",
                    new SQLiteParameter[] { new("@id", orderId) });
                LoadOrders();
            }
        }
    }
}
