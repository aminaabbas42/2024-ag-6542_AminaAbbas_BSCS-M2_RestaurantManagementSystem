using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.Models;

// ===== TABLES FORM =====
namespace RestaurantManagementSystem
{
    public class TablesForm : UserControl
    {
        private FlowLayoutPanel flpTables;
        private Button btnRefresh, btnAddTable;
        private Label lblLegend;

        public TablesForm()
        {
            InitializeComponents();
            LoadTables();
        }

        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);
            var lblTitle = new Label { Text = "Table Management", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(5, 10), Size = new Size(350, 40) };

            btnRefresh = new Button { Text = "↻ Refresh", Location = new Point(5, 62), Size = new Size(110, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(80, 80, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadTables();

            btnAddTable = new Button { Text = "+ Add Table", Location = new Point(125, 62), Size = new Size(120, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAddTable.FlatAppearance.BorderSize = 0;
            btnAddTable.Click += BtnAddTable_Click;

            lblLegend = new Label { Text = "🟢 Available   🔴 Occupied   🟡 Reserved", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(80, 80, 100), Location = new Point(270, 70), Size = new Size(400, 22) };

            flpTables = new FlowLayoutPanel { Location = new Point(5, 110), Size = new Size(960, 560), BackColor = Color.FromArgb(245, 245, 250), AutoScroll = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            this.Controls.AddRange(new Control[] { lblTitle, btnRefresh, btnAddTable, lblLegend, flpTables });
        }

        private void LoadTables()
        {
            flpTables.Controls.Clear();
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM RestaurantTables ORDER BY TableNumber");
            foreach (DataRow row in dt.Rows)
                flpTables.Controls.Add(CreateTableCard(row));
        }

        private Panel CreateTableCard(DataRow row)
        {
            string status = row["Status"].ToString();
            Color cardColor = status switch
            {
                "Available" => Color.FromArgb(40, 180, 100),
                "Occupied" => Color.FromArgb(220, 80, 80),
                "Reserved" => Color.FromArgb(255, 180, 0),
                _ => Color.Gray
            };

            var card = new Panel { Size = new Size(160, 160), Margin = new Padding(8), BackColor = Color.White, Cursor = Cursors.Hand };
            card.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(cardColor), 0, 0, card.Width, 8);
                using var pen = new Pen(Color.FromArgb(220, 220, 235));
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lNum = new Label { Text = $"TABLE {row["TableNumber"]}", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(0, 20), Size = new Size(160, 30), TextAlign = ContentAlignment.MiddleCenter };
            var lIcon = new Label { Text = "🪑", Font = new Font("Segoe UI Emoji", 24), Location = new Point(0, 52), Size = new Size(160, 50), TextAlign = ContentAlignment.MiddleCenter };
            var lCap = new Label { Text = $"Capacity: {row["Capacity"]}", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(100, 100, 130), Location = new Point(0, 105), Size = new Size(160, 20), TextAlign = ContentAlignment.MiddleCenter };
            var lStatus = new Label { Text = status, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = cardColor, Location = new Point(0, 128), Size = new Size(160, 20), TextAlign = ContentAlignment.MiddleCenter };

            card.Controls.AddRange(new Control[] { lNum, lIcon, lCap, lStatus });

            int tableId = Convert.ToInt32(row["TableID"]);
            card.Click += (s, e) => ShowTableMenu(tableId, status, card);
            foreach (Control c in card.Controls)
                c.Click += (s, e) => ShowTableMenu(tableId, status, card);

            return card;
        }

        private void ShowTableMenu(int tableId, string status, Panel card)
        {
            var menu = new ContextMenuStrip();
            if (status == "Available")
            {
                menu.Items.Add("Set Reserved", null, (s, e) => UpdateTableStatus(tableId, "Reserved"));
                menu.Items.Add("Set Occupied", null, (s, e) => UpdateTableStatus(tableId, "Occupied"));
            }
            else
            {
                menu.Items.Add("Set Available", null, (s, e) => UpdateTableStatus(tableId, "Available"));
            }
            menu.Show(card, new Point(80, 80));
        }

        private void UpdateTableStatus(int tableId, string status)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE RestaurantTables SET Status=@s WHERE TableID=@id",
                new SQLiteParameter[] { new("@s", status), new("@id", tableId) });
            LoadTables();
        }

        private void BtnAddTable_Click(object sender, EventArgs e)
        {
            var maxNum = DatabaseHelper.ExecuteScalar("SELECT COALESCE(MAX(TableNumber),0)+1 FROM RestaurantTables");
            var result = Microsoft.VisualBasic.Interaction.InputBox("Enter capacity for new table:", "Add Table", "4");
            if (int.TryParse(result, out int cap) && cap > 0)
            {
                DatabaseHelper.ExecuteNonQuery("INSERT INTO RestaurantTables (TableNumber, Capacity, Status) VALUES (@n, @c, 'Available')",
                    new SQLiteParameter[] { new("@n", maxNum), new("@c", cap) });
                LoadTables();
            }
        }
    }
}

// ===== PAYMENTS FORM =====
namespace RestaurantManagementSystem
{
    public class PaymentsForm : UserControl
    {
        private DataGridView dgvOrders, dgvPayments;
        private Button btnPay, btnRefresh;
        private Label lblTotal;
        private TabControl tabControl;

        public PaymentsForm()
        {
            InitializeComponents();
            LoadOpenOrders();
            LoadPaymentHistory();
        }

        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);
            var lblTitle = new Label { Text = "Payments", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(5, 10), Size = new Size(350, 40) };

            tabControl = new TabControl { Location = new Point(5, 60), Size = new Size(960, 610), Font = new Font("Segoe UI", 9) };

            // Tab 1 - Process Payment
            var tabProcess = new TabPage("Process Payment") { BackColor = Color.FromArgb(245, 245, 250) };
            var lOpen = new Label { Text = "Open Orders - Select to Process Payment:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(5, 10), Size = new Size(400, 26) };

            dgvOrders = CreateGrid(5, 40, 950, 300);

            btnPay = new Button { Text = "💳  Process Payment", Location = new Point(5, 350), Size = new Size(200, 40), Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = Color.FromArgb(40, 180, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPay.FlatAppearance.BorderSize = 0;
            btnPay.Click += BtnPay_Click;

            lblTotal = new Label { Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 0), Location = new Point(220, 354), Size = new Size(300, 32) };

            tabProcess.Controls.AddRange(new Control[] { lOpen, dgvOrders, btnPay, lblTotal });

            // Tab 2 - Payment History
            var tabHistory = new TabPage("Payment History") { BackColor = Color.FromArgb(245, 245, 250) };
            btnRefresh = new Button { Text = "↻ Refresh", Location = new Point(5, 10), Size = new Size(110, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(80, 80, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadPaymentHistory();

            dgvPayments = CreateGrid(5, 54, 950, 490);
            tabHistory.Controls.AddRange(new Control[] { btnRefresh, dgvPayments });

            tabControl.TabPages.AddRange(new[] { tabProcess, tabHistory });
            dgvOrders.SelectionChanged += (s, e) => {
                if (dgvOrders.SelectedRows.Count > 0)
                    lblTotal.Text = $"Total: ${dgvOrders.SelectedRows[0].Cells["Total"].Value}";
            };

            this.Controls.AddRange(new Control[] { lblTitle, tabControl });
        }

        private DataGridView CreateGrid(int x, int y, int w, int h)
        {
            var dgv = new DataGridView { Location = new Point(x, y), Size = new Size(w, h), BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect, GridColor = Color.FromArgb(230, 230, 240), MultiSelect = false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42); dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); dgv.ColumnHeadersHeight = 36;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255); dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100); dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            return dgv;
        }

        private void LoadOpenOrders()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"SELECT o.OrderID AS 'Order #', t.TableNumber AS 'Table', u.FullName AS 'Waiter', o.OrderDate AS 'Date', o.TotalAmount AS 'Total', o.Notes FROM Orders o LEFT JOIN RestaurantTables t ON o.TableID=t.TableID LEFT JOIN Users u ON o.UserID=u.UserID WHERE o.Status='Open' ORDER BY o.OrderDate");
            dgvOrders.DataSource = dt;
        }

        private void LoadPaymentHistory()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"SELECT p.PaymentID AS '#', p.OrderID AS 'Order', t.TableNumber AS 'Table', p.AmountPaid AS 'Amount Paid', p.PaymentMethod AS 'Method', p.PaymentDate AS 'Date' FROM Payments p LEFT JOIN Orders o ON p.OrderID=o.OrderID LEFT JOIN RestaurantTables t ON o.TableID=t.TableID ORDER BY p.PaymentDate DESC");
            dgvPayments.DataSource = dt;
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) { MessageBox.Show("Select an order to pay."); return; }
            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["Order #"].Value);
            decimal total = Convert.ToDecimal(dgvOrders.SelectedRows[0].Cells["Total"].Value);
            new PaymentDialog(orderId, total, LoadOpenOrders, LoadPaymentHistory).ShowDialog();
        }
    }

    public class PaymentDialog : Form
    {
        private int orderId; private decimal total;
        private ComboBox cmbMethod; private TextBox txtAmount;
        private Label lblChange; private Button btnConfirm, btnCancel;
        private Action reload1, reload2;

        public PaymentDialog(int orderId, decimal total, Action reload1, Action reload2)
        {
            this.orderId = orderId; this.total = total;
            this.reload1 = reload1; this.reload2 = reload2;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = $"Process Payment - Order #{orderId}";
            this.Size = new Size(380, 320); this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White; this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;

            var lTitle = new Label { Text = $"Payment for Order #{orderId}", Font = new Font("Segoe UI", 13, FontStyle.Bold), Location = new Point(20, 15), Size = new Size(330, 30) };
            var lTotal = new Label { Text = $"Total Due: ${total:F2}", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 0), Location = new Point(20, 50), Size = new Size(330, 32) };
            var lMethod = new Label { Text = "Payment Method:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 95), Size = new Size(150, 24) };
            cmbMethod = new ComboBox { Location = new Point(175, 93), Size = new Size(165, 28), Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMethod.Items.AddRange(new[] { "Cash", "Credit Card", "Debit Card", "Mobile Pay" }); cmbMethod.SelectedIndex = 0;
            var lAmt = new Label { Text = "Amount Received:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 135), Size = new Size(150, 24) };
            txtAmount = new TextBox { Location = new Point(175, 133), Size = new Size(165, 28), Font = new Font("Segoe UI", 10), Text = total.ToString("F2") };
            txtAmount.TextChanged += (s, e) => { if (decimal.TryParse(txtAmount.Text, out decimal amt)) lblChange.Text = $"Change: ${Math.Max(0, amt - total):F2}"; };
            lblChange = new Label { Text = "Change: $0.00", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(40, 180, 100), Location = new Point(20, 175), Size = new Size(330, 28) };
            btnConfirm = new Button { Text = "✔  Confirm Payment", Location = new Point(20, 220), Size = new Size(195, 42), Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = Color.FromArgb(40, 180, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnConfirm.FlatAppearance.BorderSize = 0; btnConfirm.Click += BtnConfirm_Click;
            btnCancel = new Button { Text = "Cancel", Location = new Point(225, 220), Size = new Size(115, 42), Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(150, 150, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0; btnCancel.Click += (s, e) => this.Close();
            this.Controls.AddRange(new Control[] { lTitle, lTotal, lMethod, cmbMethod, lAmt, txtAmount, lblChange, btnConfirm, btnCancel });
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text, out decimal amt) || amt < total) { MessageBox.Show("Insufficient amount."); return; }
            DatabaseHelper.ExecuteNonQuery("INSERT INTO Payments (OrderID, AmountPaid, PaymentMethod) VALUES (@oid, @amt, @meth)",
                new SQLiteParameter[] { new("@oid", orderId), new("@amt", amt), new("@meth", cmbMethod.SelectedItem) });
            DatabaseHelper.ExecuteNonQuery("UPDATE Orders SET Status='Paid' WHERE OrderID=@id", new SQLiteParameter[] { new("@id", orderId) });
            DatabaseHelper.ExecuteNonQuery("UPDATE RestaurantTables SET Status='Available' WHERE TableID=(SELECT TableID FROM Orders WHERE OrderID=@id)", new SQLiteParameter[] { new("@id", orderId) });
            MessageBox.Show($"Payment of ${amt:F2} received.\nChange: ${amt - total:F2}", "Payment Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            reload1?.Invoke(); reload2?.Invoke();
            this.Close();
        }
    }
}

// ===== INVENTORY FORM =====
namespace RestaurantManagementSystem
{
    public class InventoryForm : UserControl
    {
        private DataGridView dgvInventory;
        private Button btnAdd, btnEdit, btnDelete, btnRefresh;

        public InventoryForm() { InitializeComponents(); LoadInventory(); }

        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);
            var lblTitle = new Label { Text = "Inventory Management", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(5, 10), Size = new Size(400, 40) };

            var toolbar = new Panel { Location = new Point(5, 60), Size = new Size(960, 50), BackColor = Color.White };
            toolbar.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(230, 230, 240)), 0, 0, toolbar.Width - 1, toolbar.Height - 1);
            btnAdd = MakeBtn("+ Add Item", Color.FromArgb(255, 140, 0), 10, 8); btnEdit = MakeBtn("✎ Edit", Color.FromArgb(50, 150, 255), 145, 8); btnDelete = MakeBtn("🗑 Delete", Color.FromArgb(220, 80, 80), 255, 8); btnRefresh = MakeBtn("↻ Refresh", Color.FromArgb(80, 80, 100), 365, 8);
            toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });

            dgvInventory = CreateGrid(5, 120, 960, 540);
            dgvInventory.CellFormatting += (s, e) => {
                if (dgvInventory.Columns[e.ColumnIndex].HeaderText == "Status" && e.Value != null)
                    e.CellStyle.ForeColor = e.Value.ToString() == "Low Stock" ? Color.FromArgb(220, 80, 80) : Color.FromArgb(40, 180, 100);
            };

            this.Controls.AddRange(new Control[] { lblTitle, toolbar, dgvInventory });
            btnAdd.Click += (s, e) => { new InventoryDialog(null).ShowDialog(); LoadInventory(); };
            btnEdit.Click += (s, e) => { if (dgvInventory.SelectedRows.Count == 0) return; new InventoryDialog(Convert.ToInt32(dgvInventory.SelectedRows[0].Cells["ID"].Value)).ShowDialog(); LoadInventory(); };
            btnDelete.Click += (s, e) => { if (dgvInventory.SelectedRows.Count == 0) return; if (MessageBox.Show("Delete item?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { DatabaseHelper.ExecuteNonQuery("DELETE FROM Inventory WHERE InventoryID=@id", new SQLiteParameter[] { new("@id", Convert.ToInt32(dgvInventory.SelectedRows[0].Cells["ID"].Value)) }); LoadInventory(); } };
            btnRefresh.Click += (s, e) => LoadInventory();
        }

        private Button MakeBtn(string text, Color color, int x, int y) { var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(125, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; btn.FlatAppearance.BorderSize = 0; return btn; }

        private DataGridView CreateGrid(int x, int y, int w, int h)
        {
            var dgv = new DataGridView { Location = new Point(x, y), Size = new Size(w, h), BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect, GridColor = Color.FromArgb(230, 230, 240), MultiSelect = false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42); dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); dgv.ColumnHeadersHeight = 36;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255); dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100); dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            return dgv;
        }

        private void LoadInventory()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT InventoryID AS ID, ItemName AS 'Item', Quantity, Unit, MinStock AS 'Min Stock', LastUpdated AS 'Last Updated', CASE WHEN Quantity<=MinStock THEN 'Low Stock' ELSE 'OK' END AS Status FROM Inventory ORDER BY ItemName");
            dgvInventory.DataSource = dt;
            if (dgvInventory.Columns.Contains("ID")) dgvInventory.Columns["ID"].Visible = false;
        }
    }

    public class InventoryDialog : Form
    {
        private int? invId; private TextBox txtName, txtQty, txtUnit, txtMin; private Button btnSave, btnCancel;
        public InventoryDialog(int? invId) { this.invId = invId; InitializeComponents(); if (invId.HasValue) LoadItem(); }
        private void InitializeComponents()
        {
            this.Text = invId.HasValue ? "Edit Inventory" : "Add Inventory Item"; this.Size = new Size(360, 320); this.StartPosition = FormStartPosition.CenterParent; this.BackColor = Color.White; this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;
            int y = 20; AddLbl("Item Name:", 20, y); txtName = AddTxt(140, y, 190); y += 45; AddLbl("Quantity:", 20, y); txtQty = AddTxt(140, y, 100); y += 45; AddLbl("Unit:", 20, y); txtUnit = AddTxt(140, y, 100); y += 45; AddLbl("Min Stock:", 20, y); txtMin = AddTxt(140, y, 100); y += 55;
            btnSave = new Button { Text = "Save", Location = new Point(80, y), Size = new Size(110, 36), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; btnSave.FlatAppearance.BorderSize = 0; btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "Cancel", Location = new Point(205, y), Size = new Size(100, 36), Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(150, 150, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; btnCancel.FlatAppearance.BorderSize = 0; btnCancel.Click += (s, e) => this.Close();
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }
        private void AddLbl(string t, int x, int y) { this.Controls.Add(new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(115, 22), Font = new Font("Segoe UI", 9, FontStyle.Bold) }); }
        private TextBox AddTxt(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28), Font = new Font("Segoe UI", 10) }; this.Controls.Add(t); return t; }
        private void LoadItem() { var dt = DatabaseHelper.ExecuteQuery($"SELECT * FROM Inventory WHERE InventoryID={invId}"); if (dt.Rows.Count == 0) return; var row = dt.Rows[0]; txtName.Text = row["ItemName"].ToString(); txtQty.Text = row["Quantity"].ToString(); txtUnit.Text = row["Unit"].ToString(); txtMin.Text = row["MinStock"].ToString(); }
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text) || !decimal.TryParse(txtQty.Text, out decimal qty)) { MessageBox.Show("Check required fields."); return; }
            decimal.TryParse(txtMin.Text, out decimal min);
            if (invId.HasValue) DatabaseHelper.ExecuteNonQuery("UPDATE Inventory SET ItemName=@n, Quantity=@q, Unit=@u, MinStock=@m, LastUpdated=CURRENT_TIMESTAMP WHERE InventoryID=@id", new SQLiteParameter[] { new("@n", txtName.Text), new("@q", qty), new("@u", txtUnit.Text), new("@m", min), new("@id", invId) });
            else DatabaseHelper.ExecuteNonQuery("INSERT INTO Inventory (ItemName, Quantity, Unit, MinStock) VALUES (@n,@q,@u,@m)", new SQLiteParameter[] { new("@n", txtName.Text), new("@q", qty), new("@u", txtUnit.Text), new("@m", min) });
            this.Close();
        }
    }
}

// ===== REPORTS FORM =====
namespace RestaurantManagementSystem
{
    public class ReportsForm : UserControl
    {
        private DataGridView dgvReport; private ComboBox cmbReport; private DateTimePicker dtpFrom, dtpTo; private Button btnGenerate; private Label lblSummary;
        public ReportsForm() { InitializeComponents(); }
        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);
            var lblTitle = new Label { Text = "Reports & Analytics", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(5, 10), Size = new Size(400, 40) };
            var lRep = new Label { Text = "Report Type:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(5, 68), Size = new Size(100, 24) };
            cmbReport = new ComboBox { Location = new Point(110, 65), Size = new Size(200, 28), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReport.Items.AddRange(new[] { "Sales Summary", "Orders by Table", "Top Menu Items", "Payment Methods", "Daily Revenue" });
            cmbReport.SelectedIndex = 0;
            var lFrom = new Label { Text = "From:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(325, 68), Size = new Size(45, 24) };
            dtpFrom = new DateTimePicker { Location = new Point(370, 65), Size = new Size(140, 28), Font = new Font("Segoe UI", 9), Value = DateTime.Now.AddDays(-30) };
            var lTo = new Label { Text = "To:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(520, 68), Size = new Size(30, 24) };
            dtpTo = new DateTimePicker { Location = new Point(550, 65), Size = new Size(140, 28), Font = new Font("Segoe UI", 9), Value = DateTime.Now };
            btnGenerate = new Button { Text = "Generate Report", Location = new Point(702, 63), Size = new Size(150, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; btnGenerate.FlatAppearance.BorderSize = 0; btnGenerate.Click += GenerateReport;
            lblSummary = new Label { Location = new Point(5, 108), Size = new Size(960, 28), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(80, 80, 100) };
            dgvReport = CreateGrid(5, 140, 960, 520);
            this.Controls.AddRange(new Control[] { lblTitle, lRep, cmbReport, lFrom, dtpFrom, lTo, dtpTo, btnGenerate, lblSummary, dgvReport });
        }
        private DataGridView CreateGrid(int x, int y, int w, int h) { var dgv = new DataGridView { Location = new Point(x, y), Size = new Size(w, h), BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect, GridColor = Color.FromArgb(230, 230, 240) }; dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42); dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); dgv.ColumnHeadersHeight = 36; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255); return dgv; }
        private void GenerateReport(object sender, EventArgs e)
        {
            string from = dtpFrom.Value.ToString("yyyy-MM-dd"), to = dtpTo.Value.AddDays(1).ToString("yyyy-MM-dd");
            DataTable dt = cmbReport.SelectedItem.ToString() switch
            {
                "Sales Summary" => DatabaseHelper.ExecuteQuery($"SELECT DATE(o.OrderDate) AS Date, COUNT(*) AS Orders, SUM(o.TotalAmount) AS Revenue FROM Orders o WHERE o.Status='Paid' AND o.OrderDate>='{from}' AND o.OrderDate<'{to}' GROUP BY DATE(o.OrderDate) ORDER BY Date DESC"),
                "Orders by Table" => DatabaseHelper.ExecuteQuery($"SELECT t.TableNumber AS Table, COUNT(*) AS Orders, SUM(o.TotalAmount) AS Revenue FROM Orders o JOIN RestaurantTables t ON o.TableID=t.TableID WHERE o.OrderDate>='{from}' AND o.OrderDate<'{to}' GROUP BY t.TableNumber ORDER BY Revenue DESC"),
                "Top Menu Items" => DatabaseHelper.ExecuteQuery($"SELECT m.ItemName AS Item, SUM(oi.Quantity) AS 'Times Ordered', SUM(oi.SubTotal) AS Revenue FROM OrderItems oi JOIN MenuItems m ON oi.ItemID=m.ItemID JOIN Orders o ON oi.OrderID=o.OrderID WHERE o.OrderDate>='{from}' AND o.OrderDate<'{to}' GROUP BY m.ItemName ORDER BY Revenue DESC"),
                "Payment Methods" => DatabaseHelper.ExecuteQuery($"SELECT PaymentMethod AS Method, COUNT(*) AS Transactions, SUM(AmountPaid) AS Total FROM Payments WHERE PaymentDate>='{from}' AND PaymentDate<'{to}' GROUP BY PaymentMethod"),
                "Daily Revenue" => DatabaseHelper.ExecuteQuery($"SELECT DATE(OrderDate) AS Date, COUNT(*) AS Orders, SUM(TotalAmount) AS Revenue FROM Orders WHERE Status='Paid' AND OrderDate>='{from}' AND OrderDate<'{to}' GROUP BY DATE(OrderDate) ORDER BY Date"),
                _ => new DataTable()
            };
            dgvReport.DataSource = dt;
            lblSummary.Text = $"Report: {cmbReport.SelectedItem}  |  Period: {dtpFrom.Value:MMM dd} - {dtpTo.Value:MMM dd, yyyy}  |  {dt.Rows.Count} records";
        }
    }
}

// ===== USERS FORM =====
namespace RestaurantManagementSystem
{
    public class UsersForm : UserControl
    {
        private DataGridView dgvUsers; private Button btnAdd, btnEdit, btnDelete, btnRefresh;
        public UsersForm() { InitializeComponents(); LoadUsers(); }
        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);
            var lblTitle = new Label { Text = "User Management", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(5, 10), Size = new Size(400, 40) };
            var toolbar = new Panel { Location = new Point(5, 60), Size = new Size(960, 50), BackColor = Color.White }; toolbar.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(230, 230, 240)), 0, 0, toolbar.Width - 1, toolbar.Height - 1);
            btnAdd = MBtn("+ Add User", Color.FromArgb(255, 140, 0), 10, 8); btnEdit = MBtn("✎ Edit", Color.FromArgb(50, 150, 255), 145, 8); btnDelete = MBtn("🗑 Delete", Color.FromArgb(220, 80, 80), 255, 8); btnRefresh = MBtn("↻ Refresh", Color.FromArgb(80, 80, 100), 365, 8);
            toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });
            dgvUsers = CreateGrid(5, 120, 960, 540);
            this.Controls.AddRange(new Control[] { lblTitle, toolbar, dgvUsers });
            btnAdd.Click += (s, e) => { new UserDialog(null).ShowDialog(); LoadUsers(); }; btnEdit.Click += (s, e) => { if (dgvUsers.SelectedRows.Count == 0) return; new UserDialog(Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value)).ShowDialog(); LoadUsers(); }; btnDelete.Click += BtnDelete_Click; btnRefresh.Click += (s, e) => LoadUsers();
        }
        private Button MBtn(string t, Color c, int x, int y) { var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(125, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
        private DataGridView CreateGrid(int x, int y, int w, int h) { var dgv = new DataGridView { Location = new Point(x, y), Size = new Size(w, h), BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect, GridColor = Color.FromArgb(230, 230, 240), MultiSelect = false }; dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42); dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); dgv.ColumnHeadersHeight = 36; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255); dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100); dgv.DefaultCellStyle.SelectionForeColor = Color.Black; return dgv; }
        private void LoadUsers() { var dt = DatabaseHelper.ExecuteQuery("SELECT UserID AS ID, FullName AS 'Full Name', Username, Role, CreatedAt AS 'Created' FROM Users ORDER BY Role, FullName"); dgvUsers.DataSource = dt; if (dgvUsers.Columns.Contains("ID")) dgvUsers.Columns["ID"].Visible = false; }
        private void BtnDelete_Click(object sender, EventArgs e) { if (dgvUsers.SelectedRows.Count == 0) return; int id = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value); if (id == Models.Session.CurrentUser.UserID) { MessageBox.Show("Cannot delete your own account."); return; } if (MessageBox.Show("Delete user?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { DatabaseHelper.ExecuteNonQuery("DELETE FROM Users WHERE UserID=@id", new SQLiteParameter[] { new("@id", id) }); LoadUsers(); } }
    }

    public class UserDialog : Form
    {
        private int? userId; private TextBox txtName, txtUser, txtPass; private ComboBox cmbRole; private Button btnSave, btnCancel;
        public UserDialog(int? userId) { this.userId = userId; InitializeComponents(); if (userId.HasValue) LoadUser(); }
        private void InitializeComponents()
        {
            this.Text = userId.HasValue ? "Edit User" : "Add User"; this.Size = new Size(380, 340); this.StartPosition = FormStartPosition.CenterParent; this.BackColor = Color.White; this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;
            int y = 20; AL("Full Name:", 20, y); txtName = AT(150, y, 200); y += 45; AL("Username:", 20, y); txtUser = AT(150, y, 200); y += 45; AL("Password:", 20, y); txtPass = AT(150, y, 200); txtPass.PasswordChar = '●'; y += 45; AL("Role:", 20, y);
            cmbRole = new ComboBox { Location = new Point(150, y), Size = new Size(200, 28), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList }; cmbRole.Items.AddRange(new[] { "Admin", "Cashier", "Waiter", "Chef" }); cmbRole.SelectedIndex = 2; this.Controls.Add(cmbRole); y += 55;
            btnSave = new Button { Text = "Save", Location = new Point(80, y), Size = new Size(110, 36), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; btnSave.FlatAppearance.BorderSize = 0; btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "Cancel", Location = new Point(205, y), Size = new Size(100, 36), Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(150, 150, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; btnCancel.FlatAppearance.BorderSize = 0; btnCancel.Click += (s, e) => this.Close();
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }
        private void AL(string t, int x, int y) { this.Controls.Add(new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(125, 22), Font = new Font("Segoe UI", 9, FontStyle.Bold) }); }
        private TextBox AT(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28), Font = new Font("Segoe UI", 10) }; this.Controls.Add(t); return t; }
        private void LoadUser() { var dt = DatabaseHelper.ExecuteQuery($"SELECT * FROM Users WHERE UserID={userId}"); if (dt.Rows.Count == 0) return; var row = dt.Rows[0]; txtName.Text = row["FullName"].ToString(); txtUser.Text = row["Username"].ToString(); txtPass.Text = row["Password"].ToString(); cmbRole.SelectedItem = row["Role"].ToString(); }
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text)) { MessageBox.Show("All fields are required."); return; }
            string hashedPw = DatabaseHelper.HashPassword(txtPass.Text);
            if (userId.HasValue) DatabaseHelper.ExecuteNonQuery("UPDATE Users SET FullName=@n, Username=@u, Password=@p, Role=@r WHERE UserID=@id", new SQLiteParameter[] { new("@n", txtName.Text), new("@u", txtUser.Text), new("@p", hashedPw), new("@r", cmbRole.SelectedItem), new("@id", userId) });
            else DatabaseHelper.ExecuteNonQuery("INSERT INTO Users (FullName, Username, Password, Role) VALUES (@n,@u,@p,@r)", new SQLiteParameter[] { new("@n", txtName.Text), new("@u", txtUser.Text), new("@p", hashedPw), new("@r", cmbRole.SelectedItem) });
            this.Close();
        }
    }
}
