using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace RestaurantManagementSystem
{
    public class MenuForm : UserControl
    {
        private DataGridView dgvMenu;
        private Button btnAdd, btnEdit, btnDelete, btnToggle, btnRefresh;
        private TextBox txtSearch;
        private ComboBox cmbCatFilter;
        private Label lblCount;

        public MenuForm()
        {
            InitializeComponents();
            LoadMenu();
        }

        private void InitializeComponents()
        {
            this.BackColor = Color.FromArgb(245, 245, 250);

            var lblTitle = new Label { Text = "Menu Management", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 30, 60), Location = new Point(5, 10), Size = new Size(350, 40) };

            var toolbar = new Panel { Location = new Point(5, 60), Size = new Size(960, 50), BackColor = Color.White };
            toolbar.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(230, 230, 240)), 0, 0, toolbar.Width - 1, toolbar.Height - 1);

            btnAdd = MakeBtn("+ Add Item", Color.FromArgb(255, 140, 0), 10, 8, 125);
            btnEdit = MakeBtn("✎ Edit", Color.FromArgb(50, 150, 255), 145, 8, 100);
            btnDelete = MakeBtn("🗑 Delete", Color.FromArgb(220, 80, 80), 255, 8, 100);
            btnToggle = MakeBtn("Toggle Avail.", Color.FromArgb(80, 160, 80), 365, 8, 125);
            btnRefresh = MakeBtn("↻ Refresh", Color.FromArgb(80, 80, 100), 500, 8, 100);

            var lCat = new Label { Text = "Category:", Location = new Point(618, 15), Size = new Size(70, 22), Font = new Font("Segoe UI", 9) };
            cmbCatFilter = new ComboBox { Location = new Point(690, 10), Size = new Size(130, 28), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCatFilter.SelectedIndexChanged += (s, e) => LoadMenu();

            txtSearch = new TextBox { Location = new Point(830, 12), Size = new Size(110, 26), Font = new Font("Segoe UI", 9), PlaceholderText = "Search..." };
            txtSearch.TextChanged += (s, e) => LoadMenu();

            toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnToggle, btnRefresh, lCat, cmbCatFilter, txtSearch });

            dgvMenu = CreateGrid(5, 120, 960, 540);
            dgvMenu.CellFormatting += DgvMenu_CellFormatting;

            this.Controls.AddRange(new Control[] { lblTitle, toolbar, dgvMenu });

            btnAdd.Click += (s, e) => OpenMenuItemDialog(null);
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnToggle.Click += BtnToggle_Click;
            btnRefresh.Click += (s, e) => LoadMenu();

            LoadCategories();
        }

        private Button MakeBtn(string text, Color color, int x, int y, int w)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 34), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private DataGridView CreateGrid(int x, int y, int w, int h)
        {
            var dgv = new DataGridView { Location = new Point(x, y), Size = new Size(w, h), BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9), SelectionMode = DataGridViewSelectionMode.FullRowSelect, GridColor = Color.FromArgb(230, 230, 240), MultiSelect = false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 255);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 100);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            return dgv;
        }

        private void LoadCategories()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT 0 AS CategoryID, 'All' AS CategoryName UNION SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName");
            cmbCatFilter.DataSource = dt;
            cmbCatFilter.DisplayMember = "CategoryName";
            cmbCatFilter.ValueMember = "CategoryID";
        }

        private void LoadMenu()
        {
            string sql = @"SELECT m.ItemID, m.ItemName AS 'Item Name', c.CategoryName AS 'Category',
                           m.Price, m.Description, CASE WHEN m.IsAvailable=1 THEN 'Yes' ELSE 'No' END AS 'Available'
                           FROM MenuItems m LEFT JOIN Categories c ON m.CategoryID=c.CategoryID WHERE 1=1";
            if (cmbCatFilter.SelectedValue != null && Convert.ToInt32(cmbCatFilter.SelectedValue) > 0)
                sql += $" AND m.CategoryID={cmbCatFilter.SelectedValue}";
            if (!string.IsNullOrEmpty(txtSearch.Text))
                sql += $" AND (m.ItemName LIKE '%{txtSearch.Text}%' OR m.Description LIKE '%{txtSearch.Text}%')";
            sql += " ORDER BY c.CategoryName, m.ItemName";

            dgvMenu.DataSource = DatabaseHelper.ExecuteQuery(sql);
            if (dgvMenu.Columns.Contains("ItemID")) dgvMenu.Columns["ItemID"].Visible = false;
        }

        private void DgvMenu_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvMenu.Columns[e.ColumnIndex].HeaderText == "Available" && e.Value != null)
            {
                e.CellStyle.ForeColor = e.Value.ToString() == "Yes" ? Color.FromArgb(40, 180, 100) : Color.FromArgb(220, 80, 80);
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvMenu.SelectedRows.Count == 0) { MessageBox.Show("Select an item."); return; }
            int id = Convert.ToInt32(dgvMenu.SelectedRows[0].Cells["ItemID"].Value);
            OpenMenuItemDialog(id);
        }

        private void OpenMenuItemDialog(int? itemId)
        {
            var form = new MenuItemDialog(itemId);
            if (form.ShowDialog() == DialogResult.OK) LoadMenu();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvMenu.SelectedRows.Count == 0) { MessageBox.Show("Select an item."); return; }
            int id = Convert.ToInt32(dgvMenu.SelectedRows[0].Cells["ItemID"].Value);
            string name = dgvMenu.SelectedRows[0].Cells["Item Name"].Value.ToString();
            if (MessageBox.Show($"Delete '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseHelper.ExecuteNonQuery("DELETE FROM MenuItems WHERE ItemID=@id", new SQLiteParameter[] { new("@id", id) });
                LoadMenu();
            }
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            if (dgvMenu.SelectedRows.Count == 0) { MessageBox.Show("Select an item."); return; }
            int id = Convert.ToInt32(dgvMenu.SelectedRows[0].Cells["ItemID"].Value);
            DatabaseHelper.ExecuteNonQuery("UPDATE MenuItems SET IsAvailable=CASE WHEN IsAvailable=1 THEN 0 ELSE 1 END WHERE ItemID=@id",
                new SQLiteParameter[] { new("@id", id) });
            LoadMenu();
        }
    }

    public class MenuItemDialog : Form
    {
        private int? itemId;
        private TextBox txtName, txtPrice, txtDesc;
        private ComboBox cmbCategory;
        private CheckBox chkAvailable;
        private Button btnSave, btnCancel;

        public MenuItemDialog(int? itemId)
        {
            this.itemId = itemId;
            InitializeComponents();
            if (itemId.HasValue) LoadItem();
        }

        private void InitializeComponents()
        {
            this.Text = itemId.HasValue ? "Edit Menu Item" : "Add Menu Item";
            this.Size = new Size(420, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            int y = 20;
            AddLabel("Item Name:", 20, y); txtName = AddTextBox(130, y, 250); y += 45;
            AddLabel("Category:", 20, y);
            cmbCategory = new ComboBox { Location = new Point(130, y), Size = new Size(250, 28), Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            var cats = DatabaseHelper.ExecuteQuery("SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName");
            cmbCategory.DataSource = cats; cmbCategory.DisplayMember = "CategoryName"; cmbCategory.ValueMember = "CategoryID";
            this.Controls.Add(cmbCategory); y += 45;
            AddLabel("Price ($):", 20, y); txtPrice = AddTextBox(130, y, 120); y += 45;
            AddLabel("Description:", 20, y); txtDesc = AddTextBox(130, y, 250, 60); y += 80;
            chkAvailable = new CheckBox { Text = "Available", Location = new Point(130, y), Font = new Font("Segoe UI", 10), Checked = true }; this.Controls.Add(chkAvailable); y += 40;

            btnSave = new Button { Text = "Save", Location = new Point(130, y), Size = new Size(120, 36), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "Cancel", Location = new Point(262, y), Size = new Size(100, 36), Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(150, 150, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private Label AddLabel(string text, int x, int y) { var l = new Label { Text = text, Location = new Point(x, y + 3), Size = new Size(105, 22), Font = new Font("Segoe UI", 9, FontStyle.Bold) }; this.Controls.Add(l); return l; }
        private TextBox AddTextBox(int x, int y, int w, int h = 28) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Segoe UI", 10), Multiline = h > 28 }; this.Controls.Add(t); return t; }

        private void LoadItem()
        {
            var dt = DatabaseHelper.ExecuteQuery($"SELECT * FROM MenuItems WHERE ItemID={itemId}");
            if (dt.Rows.Count == 0) return;
            var row = dt.Rows[0];
            txtName.Text = row["ItemName"].ToString();
            txtPrice.Text = row["Price"].ToString();
            txtDesc.Text = row["Description"].ToString();
            chkAvailable.Checked = Convert.ToBoolean(row["IsAvailable"]);
            cmbCategory.SelectedValue = Convert.ToInt32(row["CategoryID"]);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtPrice.Text)) { MessageBox.Show("Name and price are required."); return; }
            if (!decimal.TryParse(txtPrice.Text, out decimal price)) { MessageBox.Show("Invalid price."); return; }

            if (itemId.HasValue)
                DatabaseHelper.ExecuteNonQuery("UPDATE MenuItems SET ItemName=@n, CategoryID=@c, Price=@p, Description=@d, IsAvailable=@a WHERE ItemID=@id",
                    new SQLiteParameter[] { new("@n", txtName.Text), new("@c", cmbCategory.SelectedValue), new("@p", price), new("@d", txtDesc.Text), new("@a", chkAvailable.Checked ? 1 : 0), new("@id", itemId) });
            else
                DatabaseHelper.ExecuteNonQuery("INSERT INTO MenuItems (ItemName, CategoryID, Price, Description, IsAvailable) VALUES (@n,@c,@p,@d,@a)",
                    new SQLiteParameter[] { new("@n", txtName.Text), new("@c", cmbCategory.SelectedValue), new("@p", price), new("@d", txtDesc.Text), new("@a", chkAvailable.Checked ? 1 : 0) });

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
