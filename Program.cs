using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace GiantMartPOS
{
    class CartItem
    {
        public string Barcode { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal SubTotal => Price * Qty;
    }

    class Program
    {
        static string connectionString = "Data Source=giantmart.db";

        static void Main(string[] args)
        {
            InitializeDatabase();
            RunPOS();
        }

        static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string createTable = @"
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
                using (var command = new SqliteCommand(createTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        static void RunPOS()
        {
            List<CartItem> cart = new List<CartItem>();
            bool running = true;

            while (running)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("       GIANT MART POS (VS CODE)         ");
                Console.WriteLine("========================================");
                Console.WriteLine("Current Cart Items:");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine($"{"Barcode",-10} {"Item Name",-20} {"Price",-8} {"Qty",-5} {"Total"}");
                Console.WriteLine("----------------------------------------");

                decimal grandTotal = 0;
                foreach (var item in cart)
                {
                    Console.WriteLine($"{item.Barcode,-10} {item.ProductName,-20} {item.Price,-8} {item.Qty,-5} {item.SubTotal}");
                    grandTotal += item.SubTotal;
                }

                Console.WriteLine("----------------------------------------");
                Console.WriteLine($"GRAND TOTAL: PKR {grandTotal:F2}");
                Console.WriteLine("========================================");
                Console.WriteLine("[1] Scan/Add Product  [2] Checkout & Print  [3] Clear Cart  [4] Exit");
                Console.Write("\nSelect option: ");

                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    Console.Write("Enter Barcode (e.g., 1001, 1002, 1003): ");
                    string barcode = Console.ReadLine();
                    Console.Write("Enter Quantity: ");
                    if (int.TryParse(Console.ReadLine(), out int qty) && qty > 0)
                    {
                        AddTOCart(cart, barcode, qty);
                    }
                    else
                    {
                        Console.WriteLine("Invalid Quantity! Press any key...");
                        Console.ReadKey();
                    }
                }
                else if (choice == "2")
                {
                    if (cart.Count == 0)
                    {
                        Console.WriteLine("Cart is empty! Press any key...");
                        Console.ReadKey();
                        continue;
                    }

                    Console.Write("Enter Cash Received: ");
                    if (decimal.TryParse(Console.ReadLine(), out decimal cash))
                    {
                        if (cash >= grandTotal)
                        {
                            PrintReceipt(cart, grandTotal, cash);
                            UpdateStock(cart);
                            cart.Clear();
                            Console.WriteLine("\nSale completed successfully! Press any key to continue...");
                            Console.ReadKey();
                        }
                        else
                        {
                            Console.WriteLine("Cash received is less than grand total! Press any key...");
                            Console.ReadKey();
                        }
                    }
                }
                else if (choice == "3")
                {
                    cart.Clear();
                }
                else if (choice == "4")
                {
                    running = false;
                }
            }
        }

        static void AddTOCart(List<CartItem> cart, string barcode, int qty)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ProductName, SalePrice, StockQty FROM Products WHERE Barcode = @bar";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@bar", barcode);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string name = reader["ProductName"].ToString();
                            decimal price = Convert.ToDecimal(reader["SalePrice"]);
                            int stock = Convert.ToInt32(reader["StockQty"]);

                            if (stock < qty)
                            {
                                Console.WriteLine($"Stock kam hai! Available: {stock}. Press any key...");
                                Console.ReadKey();
                                return;
                            }

                            var existing = cart.Find(x => x.Barcode == barcode);
                            if (existing != null)
                            {
                                existing.Qty += qty;
                            }
                            else
                            {
                                cart.Add(new CartItem { Barcode = barcode, ProductName = name, Price = price, Qty = qty });
                            }
                        }
                        else
                        {
                            Console.WriteLine("Product nahi mila! Press any key...");
                            Console.ReadKey();
                        }
                    }
                }
            }
        }

        static void UpdateStock(List<CartItem> cart)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                foreach (var item in cart)
                {
                    string updateQuery = "UPDATE Products SET StockQty = StockQty - @qty WHERE Barcode = @bar";
                    using (var command = new SqliteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@qty", item.Qty);
                        command.Parameters.AddWithValue("@bar", item.Barcode);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        static void PrintReceipt(List<CartItem> cart, decimal grandTotal, decimal cash)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("           GIANT SUPER MART             ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Date: {DateTime.Now}");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Item                 Qty   Price   Total");
            Console.WriteLine("----------------------------------------");

            foreach (var item in cart)
            {
                string name = item.ProductName.Length > 18 ? item.ProductName.Substring(0, 18) : item.ProductName;
                Console.WriteLine($"{name,-20} {item.Qty,-5} {item.Price,-7} {item.SubTotal}");
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine($"GRAND TOTAL : PKR {grandTotal:F2}");
            Console.WriteLine($"CASH PAID   : PKR {cash:F2}");
            Console.WriteLine($"CHANGE      : PKR {cash - grandTotal:F2}");
            Console.WriteLine("========================================");
            Console.WriteLine("     Thank You For Shopping With Us!    ");
            Console.WriteLine("========================================");
        }
    }
}