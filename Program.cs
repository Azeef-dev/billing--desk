using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace billing_desk
{
    public partial class Form1 : Form
    {
        private TextBox txtBarcode, txtQty, txtDiscount, txtCash;
        private Label lblGrandTotal, lblChange;
        private DataGridView dgvCart;
        private RichTextBox rtbReceipt;
        private string connString = "Data Source=giantmart.db";

        public Form1()
        {
            InitializeComponent();
            SetupDatabase();
            InitializeCustomUI();
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
                ";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InitializeCustomUI()
        {
            this.Text = "GIANT MART - Professional POS UI";
            this.Size = new Size(1150, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 244, 248);

            // Top Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(25, 42, 86) };
            Label lblTitle = new Label { Text = "GIANT SUPER MARKET - POINT OF SALE", ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 12), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Barcode Input
            Label lblBar = new Label { Text = "Scan Barcode:", Location = new Point(20, 70), Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblBar);

            txtBarcode = new TextBox { Location = new Point(20, 95), Size = new Size(220, 30), Font = new Font("Segoe UI", 12) };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            this.Controls.Add(txtBarcode);

            Label lblQ = new Label { Text = "Qty:", Location = new Point(250, 70), Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblQ);

            txtQty = new TextBox { Text = "1", Location = new Point(250, 95), Size = new Size(60, 30), Font = new Font("Segoe UI", 12) };
            this.Controls.Add(txtQty);

            Button btnAdd = new Button { Text = "Add to Cart", Location = new Point(325, 94), Size = new Size(110, 32), BackColor = Color.FromArgb(0, 168, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.Click += (s, e) => AddItemToCart();
            this.Controls.Add(btnAdd);

            // Cart Table (Grid)
            dgvCart = new DataGridView { Location = new Point(20, 145), Size = new Size(720, 370), BackgroundColor = Color.White, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvCart.Columns.Add("Barcode", "Barcode");
            dgvCart.Columns.Add("Name", "Item Name");
            dgvCart.Columns.Add("Price", "Price");
            dgvCart.Columns.Add("Qty", "Qty");
            dgvCart.Columns.Add("Total", "Total");
            this.Controls.Add(dgvCart);

            // Bottom Billing Panel
            Panel pnlBill = new Panel { Location = new Point(20, 530), Size = new Size(720, 120), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblNetText = new Label { Text = "NET PAYABLE (PKR):", Location = new Point(15, 10), Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            pnlBill.Controls.Add(lblNetText);

            lblGrandTotal = new Label { Text = "0.00", Location = new Point(15, 35), Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.FromArgb(194, 54, 22), AutoSize = true };
            pnlBill.Controls.Add(lblGrandTotal);

            Label lblC = new Label { Text = "Cash Paid:", Location = new Point(240, 15), AutoSize = true };
            pnlBill.Controls.Add(lblC);
            txtCash = new TextBox { Text = "0", Location = new Point(240, 35), Size = new Size(100, 25) };
            txtCash.TextChanged += (s, e) => CalculateChange();
            pnlBill.Controls.Add(txtCash);

            lblChange = new Label { Text = "Change: 0.00", Location = new Point(240, 75), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Green, AutoSize = true };
            pnlBill.Controls.Add(lblChange);

            Button btnCheckout = new Button { Text = "CHECKOUT & PRINT", Location = new Point(480, 15), Size = new Size(215, 45), BackColor = Color.FromArgb(68, 189, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnCheckout.Click += (s, e) => CheckoutSale();
            pnlBill.Controls.Add(btnCheckout);

            Button btnClear = new Button { Text = "Clear Cart", Location = new Point(480, 70), Size = new Size(215, 30), BackColor = Color.FromArgb(232, 65, 24), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClear.Click += (s, e) => ClearCart();
            pnlBill.Controls.Add(btnClear);

            this.Controls.Add(pnlBill);

            // Right Side Receipt Preview
            Label lblRecTitle = new Label { Text = "Receipt Preview:", Location = new Point(760, 70), Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblRecTitle);

            rtbReceipt = new RichTextBox { Location = new Point(760, 95, 350, 555), Size = new Size(350, 555), Font = new Font("Consolas", 10), ReadOnly = true, BackColor = Color.White };
            this.Controls.Add(rtbReceipt);
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
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
                                MessageBox.Show("Stock khatam ya kam hai!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            bool found = false;
                            foreach (DataGridViewRow row in dgvCart.Rows)
                            {
                                if (row.Cells["Barcode"].Value.ToString() == barcode)
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
                            MessageBox.Show("Product nahi mila!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Cart khali hai!", "Info");
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
            MessageBox.Show("Sale Successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearCart();
        }

        private void GenerateReceipt(decimal total)
        {
            rtbReceipt.Clear();
            rtbReceipt.AppendText("=====================================\n");
            rtbReceipt.AppendText("          GIANT SUPER MARKET         \n");
            rtbReceipt.AppendText("=====================================\n");
            rtbReceipt.AppendText($"Date: {DateTime.Now}\n");
            rtbReceipt.AppendText("-------------------------------------\n");
            rtbReceipt.AppendText("Item              Qty    Price   Total\n");
            rtbReceipt.AppendText("-------------------------------------\n");

            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                string name = row.Cells["Name"].Value.ToString();
                if (name.Length > 15) name = name.Substring(0, 15);
                rtbReceipt.AppendText($"{name,-17} {row.Cells["Qty"].Value,-6} {row.Cells["Price"].Value,-7} {row.Cells["Total"].Value}\n");
            }

            rtbReceipt.AppendText("-------------------------------------\n");
            rtbReceipt.AppendText($"GRAND TOTAL: PKR {total:0.00}\n");
            rtbReceipt.AppendText("=====================================\n");
            rtbReceipt.AppendText("      Thanks for shopping with us!   \n");
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