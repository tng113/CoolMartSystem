using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormPOS : Form
    {
        // 4 Thẻ KPI
        private Label lblTotalRevenue = null!;
        private Label lblOrderCount = null!;
        private Label lblSoldItems = null!;
        private Label lblLowStock = null!;


        // Khu vực giỏ hàng
        private TextBox txtBarcodeScan = null!;
        private DataGridView dgvCart = null!;
        private readonly List<PosCartItem> _cartItems = new();

        // Khu vực hóa đơn bên phải
        private TextBox txtCustomerPhone = null!;
        private NumericUpDown nudUsePoints = null!;
        private ComboBox cboVoucher = null!;
        private Label lblSubTotal = null!;
        private Label lblDiscount = null!;
        private Label lblVat = null!;
        private Label lblGrandTotal = null!;
        private NumericUpDown? nudCustomerPaid;   // Tiền khách đưa
        private Label? lblChange;                 // Tiền thối lại

        // Trạng thái thanh toán
        private string _selectedPaymentMethod = "Tiền mặt";
        private decimal _totalAmount = 0;
        private decimal _voucherDiscount = 0;   // Giảm giá voucher đang áp dụng (gửi lên API)
        private int _customerPointsAvailable = 0;
        private int _currentCustomerId = 0;
        public FormPOS()
        {
            this.KeyPreview = true;
            InitializeComponentProgrammatically();
            this.KeyDown += FormPOS_KeyDown;
            this.Load += async (s, e) => {
                await LoadKpiDataAsync();
                txtBarcodeScan.Focus();
            };
        }

        private void InitializeComponentProgrammatically()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // 1. Header trên cùng (Breadcrumbs & Real-time Clock)
            Panel pnlTopHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(15, 8, 15, 0) };
            Label lblBreadcrumbs = new Label { Text = "Trang chủ  >  POS", Location = new Point(15, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F) };
            Label lblMainTitle = new Label { Text = "THU NGÂN & BÁN HÀNG", Location = new Point(13, 27), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            Label lblClock = new Label { Text = DateTime.Now.ToString("dddd, MMM d, yyyy, HH:mm:ss"), Dock = DockStyle.Right, Width = 350, TextAlign = ContentAlignment.TopRight, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9F) };
            System.Windows.Forms.Timer tmrClock = new System.Windows.Forms.Timer { Interval = 1000, Enabled = true }; tmrClock.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dddd, MMM d, yyyy, HH:mm:ss");

            pnlTopHeader.Controls.Add(lblBreadcrumbs);
            pnlTopHeader.Controls.Add(lblMainTitle);
            pnlTopHeader.Controls.Add(lblClock);

            // 2. 4 Thẻ KPI lớn trên đầu trang
            TableLayoutPanel tlpCards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 95, ColumnCount = 4, RowCount = 1, Padding = new Padding(10, 8, 10, 5) };
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            lblTotalRevenue = CreateKpiCard(tlpCards, "TỔNG DOANH THU (Hôm nay)", "0 VND", "+5%", Color.FromArgb(24, 30, 48), "📈");
            lblOrderCount = CreateKpiCard(tlpCards, "HÓA ĐƠN ĐÃ LẬP", "0", "Hôm nay", Color.FromArgb(24, 30, 48), "📄");
            lblSoldItems = CreateKpiCard(tlpCards, "MẶT HÀNG ĐÃ BÁN", "0", "Sản phẩm", Color.FromArgb(24, 30, 48), "📦");
            lblLowStock = CreateKpiCard(tlpCards, "TỒN KHO THẤP", "0 SP", "màu vàng", Color.FromArgb(253, 126, 20), "⚠️");

            // 3. Panel vùng làm việc trung tâm chia tỷ lệ 7:3
            Panel pnlMainWorkspace = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 5, 12, 10) };

            // =================== PANEL BÊN PHẢI (350px) - THÔNG TIN HÓA ĐƠN ===================
            Panel pnlRightInvoice = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350, // Tăng nhẹ từ 330 lên 350 để nút bấm thoáng chữ
                BackColor = Color.White,
                Padding = new Padding(15),
                Margin = new Padding(10, 0, 0, 0)
            };

            Label lblInvoiceTitle = new Label
            {
                Text = "THÔNG TIN HÓA ĐƠN",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 30, 48),
                Dock = DockStyle.Top,
                Height = 35 // Tăng chiều cao để không bị đè vào dòng dưới
            };
            pnlRightInvoice.Controls.Add(lblInvoiceTitle);

            // Bắt đầu tọa độ y sau tiêu đề thoáng hơn
            int y = 45;
            Label lblC1 = new Label
            {
                Text = "Khách hàng:",
                Location = new Point(12, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 30, 48)
            };
            pnlRightInvoice.Controls.Add(lblC1);
            y += 25;

            Label lblPhone = new Label { Text = "SĐT:", Location = new Point(12, y + 4), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            txtCustomerPhone = new TextBox { Location = new Point(85, y), Width = 230, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "Nhập SĐT khách (Enter)..." };
            txtCustomerPhone.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await SearchCustomerByPhoneAsync(); };
            pnlRightInvoice.Controls.Add(lblPhone);
            pnlRightInvoice.Controls.Add(txtCustomerPhone);
            y += 35;

            Label lblPoints = new Label { Text = "Điểm tích lũy:", Location = new Point(12, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            nudUsePoints = new NumericUpDown { Location = new Point(110, y), Width = 205, Font = new Font("Segoe UI", 9.5F), Maximum = 100000 };
            nudUsePoints.ValueChanged += (s, e) => RecalculateBill();
            pnlRightInvoice.Controls.Add(lblPoints);
            pnlRightInvoice.Controls.Add(nudUsePoints);
            y += 35;

            Label lblVoucher = new Label { Text = "Voucher:", Location = new Point(12, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            cboVoucher = new ComboBox { Location = new Point(85, y), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cboVoucher.Items.AddRange(new object[] { "Không áp dụng", "GIAM10K (Giảm 10.000 đ)", "GIAM20K (Giảm 20.000 đ)" });
            cboVoucher.SelectedIndex = 0;
            cboVoucher.SelectedIndexChanged += (s, e) => RecalculateBill();
            pnlRightInvoice.Controls.Add(lblVoucher);
            pnlRightInvoice.Controls.Add(cboVoucher);
            y += 45;

            // Bảng tóm tắt tiền tệ
            lblSubTotal = CreateSummaryRow(pnlRightInvoice, "Tổng tiền hàng:", "0 VND", ref y);
            lblDiscount = CreateSummaryRow(pnlRightInvoice, "Giảm giá:", "0 VND", ref y);
            lblVat = CreateSummaryRow(pnlRightInvoice, "VAT (8%):", "0 VND", ref y);
            y += 15;

            // Dòng TỔNG CỘNG: (Tăng chiều cao lên 35px để không bị cắt nửa chữ)
            Label lblGTitle = new Label
            {
                Text = "TỔNG CỘNG:",
                Location = new Point(12, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 30, 48)
            };
            lblGrandTotal = new Label
            {
                Text = "0 VND",
                Location = new Point(140, y - 3),
                Width = 175,
                Height = 35, // Cho đủ chiều cao hiển thị font to
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69)
            };
            pnlRightInvoice.Controls.Add(lblGTitle);
            pnlRightInvoice.Controls.Add(lblGrandTotal);
            y += 42;

            // Tiền khách đưa + Tiền thối lại
            Label lblPaid = new Label { Text = "Khách đưa:", Location = new Point(12, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            nudCustomerPaid = new NumericUpDown
            {
                Location = new Point(110, y),
                Width = 205,
                Font = new Font("Segoe UI", 9.5F),
                Maximum = 1000000000,
                Increment = 10000,
                ThousandsSeparator = true
            };
            nudCustomerPaid.ValueChanged += (s, e) => UpdateChange();
            pnlRightInvoice.Controls.Add(lblPaid);
            pnlRightInvoice.Controls.Add(nudCustomerPaid);
            y += 30;

            Label lblChangeTitle = new Label { Text = "Tiền thối:", Location = new Point(12, y + 4), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblChange = new Label
            {
                Text = "0 VND",
                Location = new Point(110, y),
                Width = 205,
                Height = 26,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            pnlRightInvoice.Controls.Add(lblChangeTitle);
            pnlRightInvoice.Controls.Add(lblChange);
            y += 36;

            // 3 Nút Phương thức thanh toán
            TableLayoutPanel tlpPayMethods = new TableLayoutPanel
            {
                Location = new Point(12, y),
                Width = 305,
                Height = 52,
                ColumnCount = 3,
                RowCount = 1
            };
            tlpPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlpPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlpPayMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            Button btnCash = CreatePaymentButton("💵 Tiền mặt\n(F8)", () => SetPaymentMethod("Tiền mặt"));
            Button btnTransfer = CreatePaymentButton("📱 C/khoản\n(F9)", () => SetPaymentMethod("Chuyển khoản"));
            Button btnCard = CreatePaymentButton("💳 Thẻ POS", () => SetPaymentMethod("Thẻ POS"));

            tlpPayMethods.Controls.Add(btnCash, 0, 0);
            tlpPayMethods.Controls.Add(btnTransfer, 1, 0);
            tlpPayMethods.Controls.Add(btnCard, 2, 0);
            pnlRightInvoice.Controls.Add(tlpPayMethods);
            y += 62;

            // Nút Lớn: THANH TOÁN & IN HÓA ĐƠN (F12) - Bật UseMnemonic = false để hiện đủ dấu &
            Button btnCheckoutF12 = new Button
            {
                Text = "THANH TOÁN & IN HÓA ĐƠN (F12)",
                Location = new Point(12, y),
                Width = 305,
                Height = 46,
                BackColor = Color.FromArgb(24, 30, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseMnemonic = false // Hiện nguyên vẹn chữ "&"
            };
            btnCheckoutF12.FlatAppearance.BorderSize = 0;
            btnCheckoutF12.Click += async (s, e) => await ProcessCheckoutAsync();
            pnlRightInvoice.Controls.Add(btnCheckoutF12);

            // =================== PANEL BÊN TRÁI (70%) - GIỎ HÀNG ===================
            Panel pnlLeftCart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 0, 10, 0) };
            Label lblCartTitle = new Label { Text = "GIỎ HÀNG", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48), Dock = DockStyle.Top, Height = 25 };

            // Thanh tìm kiếm quét Barcode + Nút Barcode, Menu, Xóa hết
            Panel pnlBarScan = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(0, 4, 0, 6) };
            txtBarcodeScan = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), PlaceholderText = "🔍 Quét mã vạch hoặc nhập tên sản phẩm (Enter để thêm)..." };
            txtBarcodeScan.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await ScanBarcodeAsync(); };

            FlowLayoutPanel flpTools = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 135, Height = 34, WrapContents = false };
            Button btnToolBarcode = CreateIconButton("🏷️", (s, e) => txtBarcodeScan.Focus());
            Button btnToolMenu = CreateIconButton("📑", (s, e) => MessageBox.Show("Chức năng nạp danh mục nhanh.", "Tiện ích"));
            Button btnClearCart = CreateIconButton("🗑️", (s, e) => ClearCart());
            flpTools.Controls.AddRange(new Control[] { btnToolBarcode, btnToolMenu, btnClearCart });

            pnlBarScan.Controls.Add(txtBarcodeScan);
            pnlBarScan.Controls.Add(flpTools);

            // Bảng danh sách giỏ hàng
            dgvCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                Font = new Font("Segoe UI", 9.2F, FontStyle.Regular)
            };
            SetupCartGridColumns();
            dgvCart.CellContentClick += DgvCart_CellContentClick;
            dgvCart.CellValueChanged += (s, e) => RecalculateBill();

            pnlLeftCart.Controls.Add(dgvCart);
            pnlLeftCart.Controls.Add(pnlBarScan);
            pnlLeftCart.Controls.Add(lblCartTitle);

            // Gắn các Panel vào Workspace
            pnlMainWorkspace.Controls.Add(pnlLeftCart);
            pnlMainWorkspace.Controls.Add(pnlRightInvoice);

            this.Controls.Add(pnlMainWorkspace);
            this.Controls.Add(tlpCards);
            this.Controls.Add(pnlTopHeader);
        }

        private Label CreateKpiCard(TableLayoutPanel tlp, string title, string val, string subtext, Color accent, string icon)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(4) };
            Label lblT = new Label { Text = title, Location = new Point(14, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.DimGray };
            Label lblV = new Label { Text = val, Location = new Point(14, 30), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = accent };
            Label lblSub = new Label { Text = subtext, Location = new Point(16, 65), AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray };
            Label lblIcon = new Label { Text = icon, Location = new Point(card.Width - 45, 18), AutoSize = true, Font = new Font("Segoe UI", 18F), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            card.Controls.AddRange(new Control[] { lblT, lblV, lblSub, lblIcon });
            tlp.Controls.Add(card);
            return lblV;
        }

        private Label CreateSummaryRow(Panel pnl, string title, string defaultVal, ref int y)
        {
            Label lblT = new Label { Text = title, Location = new Point(12, y), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9F) };
            Label lblV = new Label { Text = defaultVal, Location = new Point(140, y), Width = 165, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            y += 24;
            return lblV;
        }

        private Button CreatePaymentButton(string text, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245),
                ForeColor = Color.FromArgb(33, 37, 41),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btn.Click += (s, e) => onClick();
            return btn;
        }


        private Button CreateIconButton(string icon, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = icon,
                Width = 36,
                Height = 30,
                BackColor = Color.FromArgb(24, 30, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void SetupCartGridColumns()
        {
            dgvCart.Columns.Clear();
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 30, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Barcode", HeaderText = "Mã vạch", FillWeight = 85, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Tên hàng hóa", FillWeight = 160, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "ĐVT", FillWeight = 40, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Đơn giá", FillWeight = 75, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Số lượng", FillWeight = 50 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Discount", HeaderText = "Giảm giá (%)", FillWeight = 60, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Thành tiền", FillWeight = 85, ReadOnly = true });

            DataGridViewButtonColumn colDel = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "Xóa",
                Text = "❌",
                UseColumnTextForButtonValue = true,
                FillWeight = 35,
                FlatStyle = FlatStyle.Flat
            };
            dgvCart.Columns.Add(colDel);

            dgvCart.Columns["Price"].DefaultCellStyle.Format = "N0 đ";
            dgvCart.Columns["Total"].DefaultCellStyle.Format = "N0 đ";
            dgvCart.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvCart.Columns["Price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCart.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void FormPOS_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F8) { SetPaymentMethod("Tiền mặt"); e.Handled = true; }
            if (e.KeyCode == Keys.F9) { SetPaymentMethod("Chuyển khoản"); e.Handled = true; }
            if (e.KeyCode == Keys.F12) { _ = ProcessCheckoutAsync(); e.Handled = true; }
        }

        private void SetPaymentMethod(string method)
        {
            _selectedPaymentMethod = method;
            MessageBox.Show($"Phương thức thanh toán đã chọn: {_selectedPaymentMethod}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task ScanBarcodeAsync()
        {
            string keyword = txtBarcodeScan.Text.Trim();
            if (string.IsNullOrEmpty(keyword)) return;

            try
            {
                // Gọi API tra cứu nhanh bằng ADO.NET thuần (Tiêu chí 19)
                var product = await ApiClientService.Client.GetFromJsonAsync<PosProductDto>($"ado-products/barcode/{Uri.EscapeDataString(keyword)}");

                if (product != null)
                {
                    var exist = _cartItems.Find(x => x.Barcode == product.Barcode);
                    if (exist != null)
                    {
                        exist.Quantity++;
                    }
                    else
                    {
                        string unit = product.ProductName.Contains("Chai") ? "Chai" : (product.ProductName.Contains("Lon") ? "Lon" : "Cây");
                        _cartItems.Add(new PosCartItem
                        {
                            ProductId = product.ProductId,
                            Barcode = product.Barcode,
                            ProductName = product.ProductName,
                            Unit = unit,
                            Price = product.Price,
                            Quantity = 1,
                            DiscountPercent = 0
                        });
                    }

                    txtBarcodeScan.Clear();
                    RefreshCartGrid();
                }
                else
                {
                    MessageBox.Show($"Không tìm thấy sản phẩm với mã: {keyword}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                MessageBox.Show($"Không tìm thấy mặt hàng '{keyword}' trong cơ sở dữ liệu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshCartGrid()
        {
            dgvCart.Rows.Clear();
            int stt = 1;
            foreach (var item in _cartItems)
            {
                decimal total = item.Price * item.Quantity * (1 - (item.DiscountPercent / 100m));
                dgvCart.Rows.Add(stt++, item.Barcode, item.ProductName, item.Unit, item.Price, item.Quantity, item.DiscountPercent, total);
            }
            RecalculateBill();
        }

        private void DgvCart_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvCart.Columns["Delete"].Index)
            {
                _cartItems.RemoveAt(e.RowIndex);
                RefreshCartGrid();
            }
        }

        private void RecalculateBill()
        {
            decimal subTotal = 0;
            foreach (var item in _cartItems)
            {
                subTotal += item.Price * item.Quantity;
            }

            // Giảm trừ voucher
            decimal voucherDiscount = 0;
            if (cboVoucher.SelectedIndex == 1) voucherDiscount = 10000;
            else if (cboVoucher.SelectedIndex == 2) voucherDiscount = 20000;

            // Giảm trừ điểm tích lũy: 1 điểm = 1.000 VNĐ (Tiêu chí 48)
            decimal pointsDiscount = nudUsePoints.Value * 1000;
            decimal totalDiscount = voucherDiscount + pointsDiscount;
            _voucherDiscount = voucherDiscount;

            // Tính thuế VAT 8%
            decimal vat = Math.Round(subTotal * 0.08m, 0);

            _totalAmount = subTotal - totalDiscount + vat;
            if (_totalAmount < 0) _totalAmount = 0;

            lblSubTotal.Text = subTotal.ToString("N0") + " VND";
            lblDiscount.Text = "-" + totalDiscount.ToString("N0") + " VND";
            lblVat.Text = vat.ToString("N0") + " VND";
            lblGrandTotal.Text = _totalAmount.ToString("N0") + " VND";
            UpdateChange();
        }

        // Tính tiền thối lại cho khách = Khách đưa - Tổng cộng
        private void UpdateChange()
        {
            if (nudCustomerPaid == null || lblChange == null) return;

            decimal paid = nudCustomerPaid.Value;
            if (paid <= 0)
            {
                lblChange.Text = "0 VND";
                lblChange.ForeColor = Color.Gray;
            }
            else if (paid < _totalAmount)
            {
                lblChange.Text = "Chưa đủ " + (_totalAmount - paid).ToString("N0") + " VND";
                lblChange.ForeColor = Color.FromArgb(220, 53, 69);
            }
            else
            {
                lblChange.Text = (paid - _totalAmount).ToString("N0") + " VND";
                lblChange.ForeColor = Color.FromArgb(40, 167, 69);
            }
        }

        private async Task SearchCustomerByPhoneAsync()
        {
            string rawPhone = txtCustomerPhone.Text.Trim();
            if (string.IsNullOrEmpty(rawPhone)) return;

            try
            {
                var cust = await ApiClientService.Client.GetFromJsonAsync<CustomerSearchDto>($"customers/by-phone/{Uri.EscapeDataString(rawPhone)}");
                if (cust != null)
                {
                    _currentCustomerId = cust.CustomerId;
                    _customerPointsAvailable = cust.RewardPoints;
                    nudUsePoints.Maximum = _customerPointsAvailable;
                    nudUsePoints.Value = 0;

                    MessageBox.Show($"Khách hàng: {cust.CustomerName}\nĐiểm hiện có: {cust.RewardPoints} điểm.", "Tìm thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                _currentCustomerId = 0;
                MessageBox.Show($"Không tìm thấy khách hàng thành viên có SĐT: {rawPhone}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private async Task ProcessCheckoutAsync()
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống! Vui lòng quét sản phẩm trước khi thanh toán.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Thanh toán tiền mặt: nếu có nhập tiền khách đưa thì phải đủ tiền
            if (_selectedPaymentMethod == "Tiền mặt" && nudCustomerPaid != null
                && nudCustomerPaid.Value > 0 && nudCustomerPaid.Value < _totalAmount)
            {
                MessageBox.Show($"Khách đưa chưa đủ tiền! Còn thiếu {(_totalAmount - nudCustomerPaid.Value):N0} VND.",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            decimal changeToReturn = (nudCustomerPaid != null && nudCustomerPaid.Value > _totalAmount)
                ? nudCustomerPaid.Value - _totalAmount : 0;

            // Đóng gói danh sách món ăn đúng chuẩn CartItemDto bên API
            var itemsDto = new List<object>();
            foreach (var item in _cartItems)
            {
                itemsDto.Add(new
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            // Chuẩn bị CheckoutRequestDto khớp hoàn toàn với Backend API
            var checkoutPayload = new
            {
                CashierUsername = string.IsNullOrWhiteSpace(SessionManager.CurrentUsername) ? "admin01" : SessionManager.CurrentUsername,
                CustomerId = _currentCustomerId,
                PointsToRedeem = (int)nudUsePoints.Value,
                VoucherDiscount = _voucherDiscount,
                Items = itemsDto
            };

            try
            {
                var res = await ApiClientService.Client.PostAsJsonAsync("checkout", checkoutPayload);

                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        $"Thanh toán thành công qua [{_selectedPaymentMethod}]!\nTổng tiền: {_totalAmount:N0} VND"
                        + (changeToReturn > 0 ? $"\nTiền thối lại khách: {changeToReturn:N0} VND" : "")
                        + "\nĐã xuất và in hóa đơn thành công.",
                        "Hoàn tất đơn hàng",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    ClearCart();
                    _currentCustomerId = 0;
                    await LoadKpiDataAsync();
                }
                else
                {
                    string errJson = await res.Content.ReadAsStringAsync();
                    MessageBox.Show($"GIAO DỊCH BỊ HỦY (ROLLBACK):\n{errJson}", "Lỗi xử lý giao dịch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối máy chủ thanh toán: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearCart()
        {
            _cartItems.Clear();
            dgvCart.Rows.Clear();
            txtBarcodeScan.Clear();
            txtCustomerPhone.Clear();
            nudUsePoints.Value = 0;
            if (nudCustomerPaid != null) nudCustomerPaid.Value = 0;
            cboVoucher.SelectedIndex = 0;
            RecalculateBill();
            txtBarcodeScan.Focus();
            _currentCustomerId = 0;
        }

        private async Task LoadKpiDataAsync()
        {
            try
            {
                var stats = await ApiClientService.Client.GetFromJsonAsync<PosKpiDto>("reports/dashboard-stats");
                if (stats != null)
                {
                    lblTotalRevenue.Text = stats.TodayRevenue >= 1000000m
                        ? (stats.TodayRevenue / 1000000m).ToString("0.0") + "M VND"
                        : stats.TodayRevenue.ToString("N0") + " VND";
                    lblOrderCount.Text = stats.TodayOrderCount.ToString();
                    lblSoldItems.Text = stats.TodaySoldItems.ToString();
                    lblLowStock.Text = stats.LowStockCount + " SP";
                }
            }
            catch
            {
                // Không kết nối được API: hiển thị 0 (không dùng số liệu giả)
                lblTotalRevenue.Text = "0 VND";
                lblOrderCount.Text = "0";
                lblSoldItems.Text = "0";
                lblLowStock.Text = "0 SP";
            }
        }
    }

    public class PosCartItem
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cái";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class PosProductDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }

    public class CustomerSearchDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int RewardPoints { get; set; }
        public string MembershipRank { get; set; } = "Chuẩn";
    }

    public class PosKpiDto
    {
        public decimal TodayRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public int TodaySoldItems { get; set; }
        public int LowStockCount { get; set; }
    }
}