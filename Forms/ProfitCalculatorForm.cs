using System;
using System.Windows.Forms;

namespace InventoryPOS.Forms
{
    public class ProfitCalculatorForm : Form
    {
        private Label lblListingPrice = null!;
        private TextBox txtListingPrice = null!;
        private Label lblShippingPrice = null!;
        private TextBox txtShippingPrice = null!;
        private Button btnCalculate = null!;
        private GroupBox gbResults = null!;
        private Label lblEbayResult = null!;
        private Label lblPoshmarkResult = null!;
        private Label lblDepopResult = null!;
        private Label lblCost = null!;
        private TextBox txtCost = null!;

        public ProfitCalculatorForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Profit Calculator - Clothing Resale";
            this.Size = new Size(450, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9F);

            // Listing Price
            lblListingPrice = new Label
            {
                Text = "Listing Price ($):",
                Location = new Point(20, 25),
                AutoSize = true
            };
            this.Controls.Add(lblListingPrice);

            txtListingPrice = new TextBox
            {
                Location = new Point(150, 22),
                Size = new Size(120, 25),
                Text = "0.00"
            };
            txtListingPrice.KeyPress += TxtNumeric_KeyPress;
            this.Controls.Add(txtListingPrice);

            // Shipping Price
            lblShippingPrice = new Label
            {
                Text = "Shipping Price ($):",
                Location = new Point(20, 60),
                AutoSize = true
            };
            this.Controls.Add(lblShippingPrice);

            txtShippingPrice = new TextBox
            {
                Location = new Point(150, 57),
                Size = new Size(120, 25),
                Text = "0.00"
            };
            txtShippingPrice.KeyPress += TxtNumeric_KeyPress;
            this.Controls.Add(txtShippingPrice);

            // Cost (COG)
            lblCost = new Label
            {
                Text = "Cost / COG ($):",
                Location = new Point(20, 95),
                AutoSize = true
            };
            this.Controls.Add(lblCost);

            txtCost = new TextBox
            {
                Location = new Point(150, 92),
                Size = new Size(120, 25),
                Text = "0.00"
            };
            txtCost.KeyPress += TxtNumeric_KeyPress;
            this.Controls.Add(txtCost);

            // Calculate Button
            btnCalculate = new Button
            {
                Text = "Calculate",
                Location = new Point(20, 135),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCalculate.FlatAppearance.BorderSize = 0;
            btnCalculate.Click += BtnCalculate_Click;
            this.Controls.Add(btnCalculate);

            // Results GroupBox
            gbResults = new GroupBox
            {
                Text = "Estimated Profit Breakdown",
                Location = new Point(20, 190),
                Size = new Size(390, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(gbResults);

            // eBay Result
            lblEbayResult = new Label
            {
                Text = "eBay: $0.00",
                Location = new Point(20, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            gbResults.Controls.Add(lblEbayResult);

            // Poshmark Result
            lblPoshmarkResult = new Label
            {
                Text = "Poshmark: $0.00",
                Location = new Point(20, 75),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            gbResults.Controls.Add(lblPoshmarkResult);

            // Depop Result
            lblDepopResult = new Label
            {
                Text = "Depop: $0.00",
                Location = new Point(20, 115),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            gbResults.Controls.Add(lblDepopResult);

            // Add detail labels for breakdown
            var lblEbayDetail = new Label
            {
                Text = "Fee: 13.25% + $0.30 | Shipping: Buyer pays",
                Location = new Point(20, 55),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            gbResults.Controls.Add(lblEbayDetail);

            var lblPoshmarkDetail = new Label
            {
                Text = "Fee: 20% (>$15) / $2.95 (<$15) | Shipping: Buyer pays (label provided)",
                Location = new Point(20, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            gbResults.Controls.Add(lblPoshmarkDetail);

            var lblDepopDetail = new Label
            {
                Text = "Fee: 10% + $0.30 | Shipping: Seller pays (buyer pays shipping to you)",
                Location = new Point(20, 135),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            gbResults.Controls.Add(lblDepopDetail);

            // Note label
            var lblNote = new Label
            {
                Text = "Note: Calculations assume clothing category. Fees subject to change.",
                Location = new Point(20, 165),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.DarkGray
            };
            gbResults.Controls.Add(lblNote);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TxtNumeric_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Allow digits, backspace, decimal point
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // Only allow one decimal point
            if (e.KeyChar == '.' && sender is TextBox txt && txt.Text.Contains("."))
            {
                e.Handled = true;
            }
        }

        private void BtnCalculate_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtListingPrice.Text, out decimal listingPrice) || listingPrice <= 0)
            {
                MessageBox.Show("Please enter a valid listing price.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtListingPrice.Focus();
                return;
            }

            if (!decimal.TryParse(txtShippingPrice.Text, out decimal shippingPrice) || shippingPrice < 0)
            {
                MessageBox.Show("Please enter a valid shipping price (0 or greater).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtShippingPrice.Focus();
                return;
            }

            if (!decimal.TryParse(txtCost.Text, out decimal cost) || cost < 0)
            {
                MessageBox.Show("Please enter a valid cost/COG (0 or greater).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCost.Focus();
                return;
            }

            // Calculate profits
            var ebayProfit = CalculateEbayProfit(listingPrice, shippingPrice, cost);
            var poshmarkProfit = CalculatePoshmarkProfit(listingPrice, shippingPrice, cost);
            var depopProfit = CalculateDepopProfit(listingPrice, shippingPrice, cost);

            lblEbayResult.Text = $"eBay: {ebayProfit:C2}";
            lblEbayResult.ForeColor = ebayProfit >= 0 ? Color.Green : Color.Red;

            lblPoshmarkResult.Text = $"Poshmark: {poshmarkProfit:C2}";
            lblPoshmarkResult.ForeColor = poshmarkProfit >= 0 ? Color.Green : Color.Red;

            lblDepopResult.Text = $"Depop: {depopProfit:C2}";
            lblDepopResult.ForeColor = depopProfit >= 0 ? Color.Green : Color.Red;
        }

        /// <summary>
        /// eBay: 13.25% final value fee + $0.30 per order for clothing & accessories
        /// Shipping: Buyer pays shipping (you receive shipping amount but pay actual shipping cost separately)
        /// We assume shipping price is what buyer pays, and you pay actual shipping cost
        /// Net = ListingPrice - (ListingPrice * 13.25%) - $0.30 - Cost
        /// Note: eBay charges fee on total (item + shipping), but for clothing category
        /// </summary>
        private decimal CalculateEbayProfit(decimal listingPrice, decimal shippingPrice, decimal cost)
        {
            // eBay charges final value fee on total amount (item + shipping) for most categories
            // Clothing & Accessories: 13.25% + $0.30 per order
            decimal totalSale = listingPrice + shippingPrice;
            decimal fee = (totalSale * 0.1360m) + 0.40m;
            decimal netProceeds = listingPrice - fee;
            decimal profit = netProceeds - cost;
            return Math.Round(profit, 2);
        }

        /// <summary>
        /// Poshmark: 20% fee for sales $15+, flat $2.95 for sales under $15
        /// Shipping: Buyer pays flat rate shipping (USPS Priority Mail label provided by Poshmark)
        /// Seller doesn't pay shipping - Poshmark provides label
        /// Net = ListingPrice - Fee - Cost (shipping is handled by Poshmark, buyer pays it)
        /// </summary>
        private decimal CalculatePoshmarkProfit(decimal listingPrice, decimal shippingPrice, decimal cost)
        {
            decimal fee = listingPrice >= 15m ? (listingPrice * 0.20m) : 2.95m;
            decimal netProceeds = listingPrice - fee;
            decimal profit = netProceeds - cost;
            return Math.Round(profit, 2);
        }

        /// <summary>
        /// Depop: 10% marketplace fee + $0.30 payment processing fee
        /// Shipping: Buyer pays shipping, seller ships item
        /// You receive (listingPrice + shippingPrice) from buyer
        /// Fee = (listingPrice * 10%) + $0.30
        /// Net = listingPrice + shippingPrice - fee - cost - actualShippingCost
        /// We'll assume actualShippingCost = shippingPrice for simplicity (or close to it)
        /// </summary>
        private decimal CalculateDepopProfit(decimal listingPrice, decimal shippingPrice, decimal cost)
        {
            // Depop fee is on item price only (not shipping)
            decimal fee = (listingPrice * 0.10m) + 0.30m;
            decimal netProceeds = listingPrice + shippingPrice - fee;
            // Assume you pay shipping cost equal to what buyer paid (simplification)
            decimal profit = netProceeds - cost - shippingPrice;
            return Math.Round(profit, 2);
        }
    }
}