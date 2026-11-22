using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace billing_desk
{
    public partial class Form1 : Form
    {
#nullable disable
        private TextBox txtBarcode, txtQty, txtSearch, txtCash;
        private Label lblGrandTotal, lblChange;
        private DataGridView dgvCart, dgvSearch;
        private RichTextBox rtbReceipt;
        private PrintDocument printDocument;
#nullable restore

        private readonly string connString = "Data Source=giantmart.db";

        public Form1()
        {
            InitializeCustomUI();
            SetupDatabase();
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        private void SetupDatabase()
        {
            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                string query = @"
                    CREATE TABLE IF NOT EXISTS Products (
                        Barcode TEXT PRIMARY KEY,
                        ProductName TEXT,
                        SalePrice REAL,
                        StockQty INTEGER
                    );
                    INSERT OR IGNORE INTO Products VALUES ('1001', 'Tapal Danedar 900g', 1250.00, 50);
                    INSERT OR IGNORE INTO Products VALUES ('1002', 'Olpers Milk 1L', 290.00, 100);
                    INSERT OR IGNORE INTO Products VALUES ('1003', 'Cooking Oil 5L', 2650.00, 30);
                    INSERT OR IGNORE INTO Products VALUES ('1004', 'National Salt 800g', 60.00, 200);
                    INSERT OR IGNORE INTO Products VALUES ('1005', 'Shan Biryani Masala', 130.00, 150);
                ";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InitializeCustomUI()
        {
            this.Text = "GIANT MART - Enterprise POS (Kifaya Style)";
            this.Size = new Size(1250, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 244, 248);

            // Top Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(25, 42, 86) };
            Label lblTitle = new Label { Text = "GIANT SUPER MARKET - POINT OF SALE", ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 12), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Barcode Input
            Label lblBar = new Label { Text = "Scan Barcode [F2]:", Location = new Point(20, 65), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblBar);

            txtBarcode = new TextBox { Location = new Point(20, 85), Size = new Size(180, 30), Font = new Font("Segoe UI", 11) };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            this.Controls.Add(txtBarcode);

            Label lblQ = new Label { Text = "Qty:", Location = new Point(210, 65), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblQ);

            txtQty = new TextBox { Text = "1", Location = new Point(210, 85), Size = new Size(50, 30), Font = new Font("Segoe UI", 11) };
            this.Controls.Add(txtQty);

            Button btnAdd = new Button { Text = "Add", Location = new Point(270, 84), Size = new Size(70, 32), BackColor = Color.FromArgb(0, 168, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.Click += (s, e) => AddItemToCart();
            this.Controls.Add(btnAdd);

            // Search Product Section [F4]
            Label lblSrch = new Label { Text = "Search Product (Name) [F4]:", Location = new Point(355, 65), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblSrch);

            txtSearch = new TextBox { Location = new Point(355, 85), Size = new Size(200, 30), Font = new Font("Segoe UI", 11) };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            this.Controls.Add(txtSearch);

            // Search Results Grid (Dropdown style)
            dgvSearch = new DataGridView { Location = new Point(355, 120), Size = new Size(360, 90), Visible = false, BackgroundColor = Color.White, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvSearch.Columns.Add("Barcode", "Barcode");
            dgvSearch.Columns.Add("Name", "Name");
            dgvSearch.Columns.Add("Price", "Price");
            dgvSearch.CellClick += DgvSearch_CellClick;
            this.Controls.Add(dgvSearch);
            dgvSearch.BringToFront();

            // Cart Table (Grid)
            dgvCart = new DataGridView { Location = new Point(20, 130), Size = new Size(735, 385), BackgroundColor = Color.White, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvCart.Columns.Add("Barcode", "Barcode");
            dgvCart.Columns.Add("Name", "Item Name");
            dgvCart.Columns.Add("Price", "Price");
            dgvCart.Columns.Add("Qty", "Qty");
            dgvCart.Columns.Add("Total", "Total");
            this.Controls.Add(dgvCart);

            // Bottom Billing Panel
            Panel pnlBill = new Panel { Location = new Point(20, 525), Size = new Size(735, 135), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblNetText = new Label { Text = "NET PAYABLE (PKR):", Location = new Point(15, 10), Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            pnlBill.Controls.Add(lblNetText);

            lblGrandTotal = new Label { Text = "0.00", Location = new Point(15, 35), Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.FromArgb(194, 54, 22), AutoSize = true };
            pnlBill.Controls.Add(lblGrandTotal);

            Label lblC = new Label { Text = "Cash Paid:", Location = new Point(250, 15), AutoSize = true };
            pnlBill.Controls.Add(lblC);
            txtCash = new TextBox { Text = "0", Location = new Point(250, 35), Size = new Size(110, 25) };
            txtCash.TextChanged += (s, e) => CalculateChange();
            pnlBill.Controls.Add(txtCash);

            lblChange = new Label { Text = "Change: 0.00", Location = new Point(250, 75), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Green, AutoSize = true };
            pnlBill.Controls.Add(lblChange);

            Button btnCheckout = new Button { Text = "CHECKOUT & PRINT [F5]", Location = new Point(480, 15), Size = new Size(230, 45), BackColor = Color.FromArgb(68, 189, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnCheckout.Click += (s, e) => CheckoutSale();
            pnlBill.Controls.Add(btnCheckout);

            Button btnClear = new Button { Text = "Clear Cart [F6]", Location = new Point(480, 70), Size = new Size(230, 30), BackColor = Color.FromArgb(232, 65, 24), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClear.Click += (s, e) => ClearCart();
            pnlBill.Controls.Add(btnClear);

            this.Controls.Add(pnlBill);

            // Right Side Receipt Preview & Print Setup
            Label lblRecTitle = new Label { Text = "Thermal Receipt Preview & Hard Copy:", Location = new Point(770, 65), Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblRecTitle);

            rtbReceipt = new RichTextBox { Location = new Point(770, 85), Size = new Size(440, 520), Font = new Font("Consolas", 10), ReadOnly = true, BackColor = Color.White };
            this.Controls.Add(rtbReceipt);

            Button btnPrintReceipt = new Button { Text = "Print Hard Copy (Thermal Printer)", Location = new Point(770, 615), Size = new Size(440, 40), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnPrintReceipt.Click += (s, e) => PrintThermalReceipt();
            this.Controls.Add(btnPrintReceipt);

            printDocument = new PrintDocument();
            printDocument.PrintPage += new PrintPageEventHandler(PrintDocument_PrintPage);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { txtBarcode.Focus(); txtBarcode.SelectAll(); }
            else if (e.KeyCode == Keys.F4) { txtSearch.Focus(); txtSearch.SelectAll(); }
            else if (e.KeyCode == Keys.F5) { CheckoutSale(); }
            else if (e.KeyCode == Keys.F6) { ClearCart(); }
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AddItemToCart();
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                dgvSearch.Visible = false;
                return;
            }

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                string query = "SELECT Barcode, ProductName, SalePrice FROM Products WHERE ProductName LIKE @kw OR Barcode LIKE @kw LIMIT 5";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        dgvSearch.Rows.Clear();
                        while (reader.Read())
                        {
                            dgvSearch.Rows.Add(reader["Barcode"].ToString(), reader["ProductName"].ToString(), reader["SalePrice"].ToString());
                        }
                        dgvSearch.Visible = dgvSearch.Rows.Count > 0;
                    }
                }
            }
        }

        private void DgvSearch_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string barcode = dgvSearch.Rows[e.RowIndex].Cells[0].Value.ToString();
                txtBarcode.Text = barcode;
                dgvSearch.Visible = false;
                txtSearch.Clear();
                AddItemToCart();
            }
        }

        private void AddItemToCart()
        {
            string barcode = txtBarcode.Text.Trim();
            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0) qty = 1;
            if (string.IsNullOrEmpty(barcode)) return;

            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                string query = "SELECT ProductName, SalePrice, StockQty FROM Products WHERE Barcode = @bar";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@bar", barcode);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string name = reader["ProductName"].ToString();
                            decimal price = Convert.ToDecimal(reader["SalePrice"]);
                            int stock = Convert.ToInt32(reader["StockQty"]);

                            if (stock < qty)
                            {
                                MessageBox.Show("Stock limit exceeded! Available: " + stock, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            bool found = false;
                            foreach (DataGridViewRow row in dgvCart.Rows)
                            {
                                if (row.Cells["Barcode"].Value?.ToString() == barcode)
                                {
                                    int oldQty = Convert.ToInt32(row.Cells["Qty"].Value);
                                    int newQty = oldQty + qty;
                                    row.Cells["Qty"].Value = newQty;
                                    row.Cells["Total"].Value = (newQty * price).ToString("0.00");
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                dgvCart.Rows.Add(barcode, name, price.ToString("0.00"), qty, (price * qty).ToString("0.00"));
                            }

                            UpdateGrandTotal();
                            txtBarcode.Clear();
                            txtQty.Text = "1";
                            txtBarcode.Focus();
                        }
                        else
                        {
                            MessageBox.Show("Product not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void UpdateGrandTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.Cells["Total"].Value != null)
                    total += Convert.ToDecimal(row.Cells["Total"].Value);
            }
            lblGrandTotal.Text = total.ToString("0.00");
            CalculateChange();
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(lblGrandTotal.Text, out decimal total) && decimal.TryParse(txtCash.Text, out decimal cash))
            {
                decimal change = cash - total;
                lblChange.Text = "Change: " + (change >= 0 ? change.ToString("0.00") : "0.00");
            }
        }

        private void CheckoutSale()
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("Cart is empty!", "Info");
                return;
            }

            decimal total = Convert.ToDecimal(lblGrandTotal.Text);
            
            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                foreach (DataGridViewRow row in dgvCart.Rows)
                {
                    string bar = row.Cells["Barcode"].Value.ToString();
                    int qty = Convert.ToInt32(row.Cells["Qty"].Value);
                    
                    string upd = "UPDATE Products SET StockQty = StockQty - @q WHERE Barcode = @b";
                    using (var cmd = new SqliteCommand(upd, conn))
                    {
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.Parameters.AddWithValue("@b", bar);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            GenerateReceipt(total);
            MessageBox.Show("Sale Completed Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GenerateReceipt(decimal total)
        {
            rtbReceipt.Clear();
            rtbReceipt.AppendText("================================================\n");
            rtbReceipt.AppendText("              GIANT SUPER MARKET                \n");
            rtbReceipt.AppendText("         Main Branch - Cash Counter #01         \n");
            rtbReceipt.AppendText("================================================\n");
            rtbReceipt.AppendText($"Date: {DateTime.Now}\n");
            rtbReceipt.AppendText("------------------------------------------------\n");
            rtbReceipt.AppendText("Item Description        Qty    Price     Total  \n");
            rtbReceipt.AppendText("------------------------------------------------\n");

            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                string name = row.Cells["Name"].Value.ToString();
                if (name.Length > 20) name = name.Substring(0, 20);
                rtbReceipt.AppendText(String.Format("{0,-22} {1,-5} {2,-9} {3}\n", name, row.Cells["Qty"].Value, row.Cells["Price"].Value, row.Cells["Total"].Value));
            }

            rtbReceipt.AppendText("------------------------------------------------\n");
            rtbReceipt.AppendText(String.Format("GRAND TOTAL:                   PKR {0:0.00}\n", total));
            rtbReceipt.AppendText(String.Format("CASH PAID:                     PKR {0:0.00}\n", string.IsNullOrEmpty(txtCash.Text) ? "0" : txtCash.Text));
            rtbReceipt.AppendText(String.Format("CHANGE RETURN:                 {0}\n", lblChange.Text));
            rtbReceipt.AppendText("================================================\n");
            rtbReceipt.AppendText("         Thanks for shopping with us!           \n");
            rtbReceipt.AppendText("      Software by Giant Enterprise Solutions    \n");
        }

        private void PrintThermalReceipt()
        {
            if (string.IsNullOrEmpty(rtbReceipt.Text))
            {
                MessageBox.Show("No receipt to print! Complete a checkout first.", "Warning");
                return;
            }

            PrintDialog printDialog = new PrintDialog();
            printDocument.DocumentName = "POS Receipt";
            printDialog.Document = printDocument;

            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDocument.Print();
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font font = new Font("Consolas", 9);
            Brush brush = Brushes.Black;
            e.Graphics.DrawString(rtbReceipt.Text, font, brush, new PointF(10, 10));
        }

        private void ClearCart()
        {
            dgvCart.Rows.Clear();
            lblGrandTotal.Text = "0.00";
            txtCash.Text = "0";
            lblChange.Text = "Change: 0.00";
            rtbReceipt.Clear();
            txtBarcode.Focus();
        }
    }
}   