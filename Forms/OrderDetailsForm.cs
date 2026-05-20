using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace RestaurantManagementSystem
{
    public class OrderDetailsForm : Form
    {
        private int orderId;
        private DataGridView dgvItems;
        private Label lblOrderInfo, lblTotal, lblStatus;
        private Button btnPrint, btnClose;

        public OrderDetailsForm(int orderId)
        {
            this.orderId = orderId;
            InitializeComponents();
            LoadOrderDetails();
        }

        private void InitializeComponents()
        {
            this.Text = $"Order Details - #{orderId}";
            this.Size = new Size(600, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblTitle = new Label
            {
                Text = $"Order Receipt #{orderId}",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 60),
                Location = new Point(20, 15),
                Size = new Size(380, 34)
            };

            lblStatus = new Label
            {
                Location = new Point(420, 18),
                Size = new Size(140, 28),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White
            };

            lblOrderInfo = new Label
            {
                Location = new Point(20, 60),
                Size = new Size(550, 60),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 80, 100)
            };

            var sep = new Panel { Location = new Point(20, 125), Size = new Size(550, 1), BackColor = Color.FromArgb(200, 200, 220) };

            var lItems = new Label { Text = "Order Items:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 135), Size = new Size(200, 24) };

            dgvItems = new DataGridView
            {
                Location = new Point(20, 163),
                Size = new Size(555, 320),
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
            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvItems.ColumnHeadersHeight = 34;
            dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255);

            var sep2 = new Panel { Location = new Point(20, 495), Size = new Size(555, 1), BackColor = Color.FromArgb(200, 200, 220) };

            var lTotalLbl = new Label { Text = "TOTAL AMOUNT:", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 508), Size = new Size(200, 30) };
            lblTotal = new Label { Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 0), Location = new Point(230, 503), Size = new Size(200, 36) };

            btnPrint = new Button { Text = "🖨 Print Receipt", Location = new Point(20, 550), Size = new Size(160, 38), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(50, 150, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += BtnPrint_Click;

            btnClose = new Button { Text = "Close", Location = new Point(460, 550), Size = new Size(115, 38), Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(150, 150, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblTitle, lblStatus, lblOrderInfo, sep, lItems, dgvItems, sep2, lTotalLbl, lblTotal, btnPrint, btnClose });
        }

        private void LoadOrderDetails()
        {
            var orderDt = DatabaseHelper.ExecuteQuery($@"
                SELECT o.*, t.TableNumber, u.FullName AS WaiterName
                FROM Orders o 
                LEFT JOIN RestaurantTables t ON o.TableID=t.TableID
                LEFT JOIN Users u ON o.UserID=u.UserID
                WHERE o.OrderID={orderId}");

            if (orderDt.Rows.Count == 0) return;
            var row = orderDt.Rows[0];

            lblStatus.Text = row["Status"].ToString();
            lblStatus.BackColor = row["Status"].ToString() switch
            {
                "Open" => Color.FromArgb(50, 150, 255),
                "Paid" => Color.FromArgb(40, 180, 100),
                "Cancelled" => Color.FromArgb(220, 80, 80),
                _ => Color.Gray
            };

            lblOrderInfo.Text = $"Table: {row["TableNumber"]}    Waiter: {row["WaiterName"]}    Date: {Convert.ToDateTime(row["OrderDate"]):yyyy-MM-dd HH:mm}\n" +
                               $"Notes: {row["Notes"]}";

            var itemsDt = DatabaseHelper.ExecuteQuery($@"
                SELECT m.ItemName AS 'Item', oi.Quantity AS 'Qty',
                       oi.UnitPrice AS 'Unit Price', oi.SubTotal AS 'Sub Total'
                FROM OrderItems oi
                JOIN MenuItems m ON oi.ItemID=m.ItemID
                WHERE oi.OrderID={orderId}");

            dgvItems.DataSource = itemsDt;
            lblTotal.Text = $"${Convert.ToDecimal(row["TotalAmount"]):F2}";
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"Receipt for Order #{orderId} sent to printer.\n\n(In a production environment, this would print to your configured receipt printer.)",
                "Print Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
