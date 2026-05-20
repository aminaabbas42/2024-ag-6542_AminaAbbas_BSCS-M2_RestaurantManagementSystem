using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem
{
    public class NewOrderForm : Form
    {
        private ComboBox cmbTable, cmbCategory;
        private DataGridView dgvMenu, dgvOrderItems;
        private Button btnAddItem, btnRemoveItem, btnPlaceOrder, btnCancel;
        private Label lblTotal, lblTableStatus;
        private TextBox txtNotes, txtQty;
        private List<OrderItemEntry> orderItems = new();

        public NewOrderForm()
        {
            InitializeComponents();
            LoadTables();
            LoadCategories();
            LoadMenuItems();
        }

        class OrderItemEntry
        {
            public int ItemID { get; set; }
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal SubTotal => Quantity * UnitPrice;
        }

        private void InitializeComponents()
        {
            this.Text = "New Order";
            this.Size = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblTitle = new Label { Text = "Create New Order", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(15, 12), Size = new Size(350, 34) };

            // Left panel - table selection & menu
            var pLeft = new Panel { Location = new Point(15, 55), Size = new Size(490, 580), BackColor = Color.White };
            pLeft.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 235)), 0, 0, pLeft.Width - 1, pLeft.Height - 1);

            var lTable = new Label { Text = "Select Table:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 12), Size = new Size(100, 22) };
            cmbTable = new ComboBox { Location = new Point(115, 10), Size = new Size(160, 26), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTable.SelectedIndexChanged += CmbTable_Changed;

            lblTableStatus = new Label { Location = new Point(285, 12), Size = new Size(190, 22), Font = new Font("Segoe UI", 9), ForeColor = Color.Green };

            var lCat = new Label { Text = "Category:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 48), Size = new Size(80, 22) };
            cmbCategory = new ComboBox { Location = new Point(95, 46), Size = new Size(150, 26), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategory.SelectedIndexChanged += (s, e) => LoadMenuItems();

            var lMenu = new Label { Text = "Menu Items", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(10, 80), Size = new Size(200, 24) };

            dgvMenu = new DataGridView
            {
                Location = new Point(10, 108),
                Size = new Size(470, 340),
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
            StyleGrid(dgvMenu);
            dgvMenu.DoubleClick += (s, e) => AddItemToOrder();

            var lQty = new Label { Text = "Qty:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 460), Size = new Size(35, 26) };
            txtQty = new TextBox { Location = new Point(50, 458), Size = new Size(60, 26), Font = new Font("Segoe UI", 10), Text = "1" };

            btnAddItem = new Button { Text = "➕ Add to Order", Location = new Point(125, 456), Size = new Size(150, 32), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.Click += (s, e) => AddItemToOrder();

            var lNotes = new Label { Text = "Notes:", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 500), Size = new Size(55, 22) };
            txtNotes = new TextBox { Location = new Point(70, 498), Size = new Size(405, 26), Font = new Font("Segoe UI", 9), PlaceholderText = "Special requests, allergies..." };

            pLeft.Controls.AddRange(new Control[] { lTable, cmbTable, lblTableStatus, lCat, cmbCategory, lMenu, dgvMenu, lQty, txtQty, btnAddItem, lNotes, txtNotes });

            // Right panel - order items
            var pRight = new Panel { Location = new Point(520, 55), Size = new Size(500, 580), BackColor = Color.White };
            pRight.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 235)), 0, 0, pRight.Width - 1, pRight.Height - 1);

            var lOrder = new Label { Text = "Order Items", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(10, 12), Size = new Size(250, 28) };

            dgvOrderItems = new DataGridView
            {
                Location = new Point(10, 48),
                Size = new Size(480, 390),
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
            StyleGrid(dgvOrderItems);

            btnRemoveItem = new Button { Text = "🗑 Remove Item", Location = new Point(10, 450), Size = new Size(150, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(220, 80, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRemoveItem.FlatAppearance.BorderSize = 0;
            btnRemoveItem.Click += BtnRemoveItem_Click;

            var sep = new Panel { Location = new Point(10, 498), Size = new Size(480, 1), BackColor = Color.FromArgb(200, 200, 220) };

            var lTotalLabel = new Label { Text = "TOTAL:", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 80), Location = new Point(10, 510), Size = new Size(100, 32) };
            lblTotal = new Label { Text = "$0.00", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 0), Location = new Point(120, 506), Size = new Size(200, 38), TextAlign = ContentAlignment.MiddleLeft };

            btnPlaceOrder = new Button { Text = "✔  PLACE ORDER", Location = new Point(10, 550), Size = new Size(230, 45), Font = new Font("Segoe UI", 12, FontStyle.Bold), BackColor = Color.FromArgb(40, 180, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPlaceOrder.FlatAppearance.BorderSize = 0;
            btnPlaceOrder.Click += BtnPlaceOrder_Click;

            btnCancel = new Button { Text = "✖  Cancel", Location = new Point(255, 550), Size = new Size(150, 45), Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(150, 150, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pRight.Controls.AddRange(new Control[] { lOrder, dgvOrderItems, btnRemoveItem, sep, lTotalLabel, lblTotal, btnPlaceOrder, btnCancel });

            this.Controls.AddRange(new Control[] { lblTitle, pLeft, pRight });
        }

        private void StyleGrid(DataGridView dgv)
        {
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void LoadTables()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT TableID, TableNumber, Capacity, Status FROM RestaurantTables WHERE Status='Available' ORDER BY TableNumber");
            cmbTable.DataSource = dt;
            cmbTable.DisplayMember = "TableNumber";
            cmbTable.ValueMember = "TableID";
        }

        private void LoadCategories()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT 0 AS CategoryID, 'All Categories' AS CategoryName UNION SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName");
            cmbCategory.DataSource = dt;
            cmbCategory.DisplayMember = "CategoryName";
            cmbCategory.ValueMember = "CategoryID";
        }

        private void LoadMenuItems()
        {
            string sql = @"SELECT m.ItemID, m.ItemName, c.CategoryName, m.Price, m.Description
                           FROM MenuItems m LEFT JOIN Categories c ON m.CategoryID=c.CategoryID
                           WHERE m.IsAvailable=1";
            if (cmbCategory.SelectedValue != null && Convert.ToInt32(cmbCategory.SelectedValue) > 0)
                sql += $" AND m.CategoryID={cmbCategory.SelectedValue}";
            sql += " ORDER BY m.ItemName";

            dgvMenu.DataSource = DatabaseHelper.ExecuteQuery(sql);
            if (dgvMenu.Columns.Contains("ItemID")) dgvMenu.Columns["ItemID"].Visible = false;
        }

        private void CmbTable_Changed(object sender, EventArgs e)
        {
            if (cmbTable.SelectedItem is DataRowView row)
                lblTableStatus.Text = $"Capacity: {row["Capacity"]} | {row["Status"]}";
        }

        private void AddItemToOrder()
        {
            if (dgvMenu.SelectedRows.Count == 0) return;
            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0) { MessageBox.Show("Enter valid quantity."); return; }

            int itemId = Convert.ToInt32(dgvMenu.SelectedRows[0].Cells["ItemID"].Value);
            string itemName = dgvMenu.SelectedRows[0].Cells["ItemName"].Value.ToString();
            decimal price = Convert.ToDecimal(dgvMenu.SelectedRows[0].Cells["Price"].Value);

            var existing = orderItems.FirstOrDefault(x => x.ItemID == itemId);
            if (existing != null)
                existing.Quantity += qty;
            else
                orderItems.Add(new OrderItemEntry { ItemID = itemId, ItemName = itemName, Quantity = qty, UnitPrice = price });

            RefreshOrderGrid();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvOrderItems.SelectedRows.Count == 0) return;
            int idx = dgvOrderItems.SelectedRows[0].Index;
            orderItems.RemoveAt(idx);
            RefreshOrderGrid();
        }

        private void RefreshOrderGrid()
        {
            var dt = new DataTable();
            dt.Columns.Add("Item");
            dt.Columns.Add("Qty");
            dt.Columns.Add("Unit Price");
            dt.Columns.Add("Sub Total");

            foreach (var item in orderItems)
                dt.Rows.Add(item.ItemName, item.Quantity, $"${item.UnitPrice:F2}", $"${item.SubTotal:F2}");

            dgvOrderItems.DataSource = dt;
            decimal total = orderItems.Sum(x => x.SubTotal);
            lblTotal.Text = $"${total:F2}";
        }

        private void BtnPlaceOrder_Click(object sender, EventArgs e)
        {
            if (cmbTable.SelectedValue == null) { MessageBox.Show("Please select a table."); return; }
            if (orderItems.Count == 0) { MessageBox.Show("Please add at least one item."); return; }

            int tableId = Convert.ToInt32(cmbTable.SelectedValue);
            decimal total = orderItems.Sum(x => x.SubTotal);
            long newOrderId = 0;

            try
            {
                // Use atomic transaction — if any step fails, everything rolls back
                DatabaseHelper.ExecuteTransaction((conn, tx) =>
                {
                    var cmdOrder = new System.Data.SQLite.SQLiteCommand(
                        @"INSERT INTO Orders (TableID, UserID, Status, TotalAmount, Notes)
                          VALUES (@t, @u, 'Open', @amt, @n); SELECT last_insert_rowid();", conn, tx);
                    cmdOrder.Parameters.AddWithValue("@t",   tableId);
                    cmdOrder.Parameters.AddWithValue("@u",   Models.Session.CurrentUser.UserID);
                    cmdOrder.Parameters.AddWithValue("@amt", total);
                    cmdOrder.Parameters.AddWithValue("@n",   txtNotes.Text);
                    newOrderId = (long)cmdOrder.ExecuteScalar();

                    foreach (var item in orderItems)
                    {
                        var cmdItem = new System.Data.SQLite.SQLiteCommand(
                            @"INSERT INTO OrderItems (OrderID, ItemID, Quantity, UnitPrice, SubTotal)
                              VALUES (@oid, @iid, @qty, @up, @st)", conn, tx);
                        cmdItem.Parameters.AddWithValue("@oid", newOrderId);
                        cmdItem.Parameters.AddWithValue("@iid", item.ItemID);
                        cmdItem.Parameters.AddWithValue("@qty", item.Quantity);
                        cmdItem.Parameters.AddWithValue("@up",  item.UnitPrice);
                        cmdItem.Parameters.AddWithValue("@st",  item.SubTotal);
                        cmdItem.ExecuteNonQuery();
                    }

                    var cmdTable = new System.Data.SQLite.SQLiteCommand(
                        "UPDATE RestaurantTables SET Status='Occupied' WHERE TableID=@t", conn, tx);
                    cmdTable.Parameters.AddWithValue("@t", tableId);
                    cmdTable.ExecuteNonQuery();
                });

                DatabaseHelper.LogAction(Models.Session.CurrentUser.UserID,
                    "PlaceOrder", "Orders", (int)newOrderId, $"Table {tableId}, Total ${total:F2}");

                MessageBox.Show($"Order #{newOrderId} placed successfully!",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error placing order: " + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
