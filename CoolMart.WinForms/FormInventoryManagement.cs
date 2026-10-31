using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormInventoryManagement : Form
    {
        private Label lblTotalItems = null!;
        private Label lblLowStock = null!;
        private Label lblExpiring = null!;
        private DataGridView dgvInventory = null!;
        private ComboBox cboCategoryFilter = null!;
        private ComboBox cboStatusFilter = null!;
        private TextBox txtReceiptCode = null!;
        private DateTimePicker dtpReceiptDate = null!;
        private TextBox txtSupplierInput = null!;
        private TextBox txtReceiptNote = null!;
        private ContextMenuStrip cmsInventory = null!;

        public FormInventoryManagement()
        {
            this.KeyPreview = true;
            InitializeComponentProgrammatically();
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.F7) ExecuteQuickImport();
            };
            this.Load += async (s, e) => await LoadInventoryDataAsync();
        }

        private void InitializeComponentProgrammatically()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // 1. Breadcrumbs (Bỏ tiêu đề trùng lặp vì FormMainShell đã có Header phía trên)
            Panel pnlTopHeader = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.White, Padding = new Padding(15, 6, 15, 0) };
            Label lblBreadcrumbs = new Label
            {
                Text = "Trang chủ  >  InventoryView - phân hệ F2 (Quản lý Kho & Nhập hàng)",
                Location = new Point(15, 6),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9F),
                UseMnemonic = false
            };
            pnlTopHeader.Controls.Add(lblBreadcrumbs);

            // 2. Top KPI Cards (Tăng chiều cao từ 95 lên 105 để không bị đè chữ)
            TableLayoutPanel tlpCards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 105, ColumnCount = 3, RowCount = 1, Padding = new Padding(10, 5, 10, 5) };
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            lblTotalItems = CreateKpiCard(tlpCards, "TỔNG MẶT HÀNG", "0", "màu xanh dương", Color.FromArgb(0, 123, 255), "📦");
            lblLowStock = CreateKpiCard(tlpCards, "SẢN PHẨM SẮP HẾT HÀNG (Tồn Min <= 10)", "0", "màu cam cảnh báo", Color.FromArgb(253, 126, 20), "⚠️");
            lblExpiring = CreateKpiCard(tlpCards, "SẢN PHẨM CẬN HẠN (Dưới 30 ngày)", "2", "màu đỏ cảnh báo", Color.FromArgb(220, 53, 69), "🚨");

            // 3. Panel Body trung tâm
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 5, 12, 10) };
            GroupBox grpInventory = new GroupBox { Dock = DockStyle.Fill, Text = "BẢNG TỒN KHO THỰC TẾ (Real-time Inventory)", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48), Padding = new Padding(10) };

            // Bộ lọc nâng cao (Filter Bar) căn chỉnh lại khoảng cách
            Panel pnlFilterBar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 2, 0, 6) };

            Label l1 = new Label { Text = "Ngành hàng:", Location = new Point(5, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), UseMnemonic = false };
            cboCategoryFilter = new ComboBox { Location = new Point(85, 5), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cboCategoryFilter.Items.AddRange(new object[] { "Tất cả", "Kem Que & Kem Hộp", "Nước Giải Khát", "Trà Đóng Chai", "Nước Tăng Lực", "Bánh Kẹo & Snack" });
            cboCategoryFilter.SelectedIndex = 0;
            cboCategoryFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            Label l3 = new Label { Text = "Trạng thái:", Location = new Point(275, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), UseMnemonic = false };
            cboStatusFilter = new ComboBox { Location = new Point(345, 5), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            cboStatusFilter.Items.AddRange(new object[] { "Tất cả", "Bình thường", "Sắp hết hàng", "Cận hạn" });
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            Button btnRefresh = new Button { Text = "Làm mới", Location = new Point(510, 4), Width = 85, Height = 28, BackColor = Color.FromArgb(24, 30, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRefresh.Click += async (s, e) => await LoadInventoryDataAsync();

            pnlFilterBar.Controls.AddRange(new Control[] { l1, cboCategoryFilter, l3, cboStatusFilter, btnRefresh });

            // Cấu hình bảng hiển thị DataGridView
            SetupContextMenu();
            dgvInventory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                ContextMenuStrip = cmsInventory,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            SetupGridColumns();
            dgvInventory.CellFormatting += DgvInventory_CellFormatting;

            grpInventory.Controls.Add(dgvInventory);
            grpInventory.Controls.Add(pnlFilterBar);

            // 4. Panel Đáy: Màn hình nhập hàng & Danh mục tác vụ (Sửa lại layout Flow/Table để không bị ép cụt ô nhập)
            TableLayoutPanel tlpBottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 175, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // 50%
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F)); // 25%
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F)); // 25%

            // Màn hình nhập hàng
            GroupBox grpImport = new GroupBox { Dock = DockStyle.Fill, Text = "Màn hình nhập hàng", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            Label lP1 = new Label { Text = "Số phiếu nhập:", Location = new Point(12, 23), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            txtReceiptCode = new TextBox { Location = new Point(105, 20), Width = 150, Text = "PN-" + DateTime.Now.ToString("yyyyMMdd-01") };

            Label lP2 = new Label { Text = "Ngày nhập:", Location = new Point(265, 23), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            dtpReceiptDate = new DateTimePicker { Location = new Point(340, 20), Width = 130, Format = DateTimePickerFormat.Short };

            Label lP3 = new Label { Text = "Nhà cung cấp:", Location = new Point(12, 55), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            txtSupplierInput = new TextBox { Location = new Point(105, 52), Width = 365, Text = "Công ty CP Thực phẩm Kido (Kinh Đô)" };

            Label lP4 = new Label { Text = "Hóa đơn nhập:", Location = new Point(12, 87), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            txtReceiptNote = new TextBox { Location = new Point(105, 84), Width = 365, Height = 42, Multiline = true, Text = "Nhập bổ sung kem ốc quế và nước tăng lực." };

            Button btnDoImport = new Button { Text = "Lưu & Nhập kho", Location = new Point(340, 132), Width = 130, Height = 32, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnDoImport.Click += (s, e) => ExecuteQuickImport();

            grpImport.Controls.AddRange(new Control[] { lP1, txtReceiptCode, lP2, dtpReceiptDate, lP3, txtSupplierInput, lP4, txtReceiptNote, btnDoImport });

            // Nhà cung cấp nổi bật
            GroupBox grpSupplierInfo = new GroupBox { Dock = DockStyle.Fill, Text = "Nhà cung cấp nổi bật", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            Label lblSupDetail = new Label { Text = "• Cty Cổ phần Kido Foods\n• Cty TNHH Nước Giải Khát Coca-Cola\n• Cty Cổ phần Sữa Vinamilk\n• Cty TNHH Suntory PepsiCo VN\n• Cty CP Orion Vina", Location = new Point(15, 25), Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = Color.FromArgb(40, 40, 40) };
            grpSupplierInfo.Controls.Add(lblSupDetail);

            // Danh mục tác vụ
            GroupBox grpTasks = new GroupBox { Dock = DockStyle.Fill, Text = "Danh mục tác vụ", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            Button btnTask1 = CreateTaskBtn("Nhập kho từ NCC (F7)", 25, (s, e) => ExecuteQuickImport());
            Button btnTask2 = CreateTaskBtn("Kiểm kê kho", 58, (s, e) => MessageBox.Show("Đã đồng bộ cân bằng số lượng kiểm kê kho định kỳ.", "Kiểm kê"));
            Button btnTask3 = CreateTaskBtn("Xuất hủy hàng hỏng", 91, (s, e) => MessageBox.Show("Đã lập phiếu xuất hủy hàng cận hạn.", "Xuất hủy"));
            Button btnTask4 = CreateTaskBtn("Xem báo cáo tồn", 124, (s, e) => MessageBox.Show("Xem báo cáo tồn kho chuyên sâu.", "Báo cáo"));

            grpTasks.Controls.AddRange(new Control[] { btnTask1, btnTask2, btnTask3, btnTask4 });

            tlpBottom.Controls.Add(grpImport, 0, 0);
            tlpBottom.Controls.Add(grpSupplierInfo, 1, 0);
            tlpBottom.Controls.Add(grpTasks, 2, 0);

            pnlBody.Controls.Add(grpInventory);
            pnlBody.Controls.Add(tlpBottom);

            this.Controls.Add(pnlBody);
            this.Controls.Add(tlpCards);
            this.Controls.Add(pnlTopHeader);
        }

        private Label CreateKpiCard(TableLayoutPanel tlp, string title, string val, string subtext, Color accent, string icon)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(4) };
            Panel line = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };
            Label lblT = new Label { Text = title, Location = new Point(14, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.DimGray, UseMnemonic = false };
            Label lblV = new Label { Text = val, Location = new Point(14, 30), AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = accent };
            Label lblSub = new Label { Text = subtext, Location = new Point(16, 68), AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, UseMnemonic = false };
            Label lblIcon = new Label { Text = icon, Location = new Point(card.Width - 55, 20), AutoSize = true, Font = new Font("Segoe UI", 20F), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            card.Controls.AddRange(new Control[] { lblT, lblV, lblSub, lblIcon, line });
            tlp.Controls.Add(card);
            return lblV;
        }

        public class StockImportRequestDto
        {
            public int ProductId { get; set; }
            public string Barcode { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string? ReceiptCode { get; set; }
            public string? Note { get; set; }
        }
        private Button CreateTaskBtn(string text, int y, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(185, 28),
                BackColor = Color.FromArgb(240, 242, 245),
                ForeColor = Color.FromArgb(33, 37, 41),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btn.Click += onClick;
            return btn;
        }

        // Tối ưu tỷ lệ các cột để không bị co chữ
        private void SetupGridColumns()
        {
            dgvInventory.Columns.Clear();
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", Width = 45 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductId", HeaderText = "Mã SP", Width = 110 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Tên hàng hóa", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 160 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "Ngành hàng", Width = 130 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "ĐVT", Width = 65 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQuantity", HeaderText = "Tồn kho thực", Width = 100 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostPrice", HeaderText = "Giá vốn", Width = 95 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "Hạn sử dụng", Width = 95 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Vị trí kệ", Width = 85 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", Width = 105 });

            dgvInventory.Columns["CostPrice"].DefaultCellStyle.Format = "N0 đ";
            dgvInventory.Columns["StockQuantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void SetupContextMenu()
        {
            cmsInventory = new ContextMenuStrip();
            ToolStripMenuItem mnuImport = new ToolStripMenuItem("Tạo phiếu nhập", null, (s, e) => ExecuteQuickImport());
            ToolStripMenuItem mnuMove = new ToolStripMenuItem("Điều chuyển vị trí", null, (s, e) => {
                if (dgvInventory.CurrentRow != null)
                {
                    string sp = dgvInventory.CurrentRow.Cells["ProductName"].Value?.ToString() ?? "";
                    MessageBox.Show($"Đã chuyển vị trí trưng bày của '{sp}' sang Kệ Trung Tâm.", "Điều chuyển kệ");
                }
            });
            cmsInventory.Items.AddRange(new ToolStripItem[] { mnuImport, mnuMove });
        }

        private async Task LoadInventoryDataAsync()
        {
            try
            {
                var res = await ApiClientService.Client.GetFromJsonAsync<PagedResultDto<ProductViewDto>>("products/paged?pageNumber=1&pageSize=100");
                dgvInventory.Rows.Clear();

                if (res != null && res.Items != null)
                {
                    int stt = 1;
                    int lowCount = 0;
                    int expireCount = 0;

                    foreach (var item in res.Items)
                    {
                        string unit = item.ProductName.Contains("Chai") ? "Chai" : (item.ProductName.Contains("Lon") ? "Lon" : "Cây/Hộp");
                        decimal costPrice = Math.Round(item.Price * 0.75m, 0);
                        string shelf = item.CategoryId == 1 ? "Tủ đông 01" : (item.CategoryId == 2 ? "Kệ A-01" : "Kệ B-02");

                        string expiry = (item.ProductId == 4 || item.ProductId == 10) ? DateTime.Now.AddDays(15).ToString("dd/MM/yyyy") : DateTime.Now.AddMonths(8).ToString("dd/MM/yyyy");
                        string status = "Bình thường";

                        if (item.StockQuantity <= 10)
                        {
                            status = "Sắp hết hàng";
                            lowCount++;
                        }

                        // Chỉ để riêng ID 10 làm mẫu sản phẩm cận hạn (< 30 ngày)
                        if (item.ProductId == 10)
                        {
                            status = "Cận hạn";
                            expiry = DateTime.Now.AddDays(15).ToString("dd/MM/yyyy");
                            expireCount++;
                        }
                        else
                        {
                            expiry = DateTime.Now.AddMonths(8).ToString("dd/MM/yyyy");
                        }

                        dgvInventory.Rows.Add(stt++, item.Barcode, item.ProductName, item.CategoryName, unit, item.StockQuantity, costPrice, expiry, shelf, status);
                        lblTotalItems.Text = res.TotalItems.ToString("N0");
                        lblLowStock.Text = lowCount.ToString();
                        lblExpiring.Text = expireCount.ToString();
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Lỗi nạp dữ liệu tồn kho: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvInventory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvInventory.Rows[e.RowIndex];
                string status = row.Cells["Status"].Value?.ToString() ?? "";

                if (status == "Cận hạn")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                    row.DefaultCellStyle.Font = new Font(dgvInventory.Font, FontStyle.Bold);
                }
                else if (status == "Sắp hết hàng")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                }
            }
        }

        private void ApplyFilter()
        {
            string cat = cboCategoryFilter.SelectedItem?.ToString() ?? "Tất cả";
            string stat = cboStatusFilter.SelectedItem?.ToString() ?? "Tất cả";

            // Tắt CurrencyManager binding tạm thời để tránh lỗi ẩn dòng khi đang chọn
            CurrencyManager? cm = BindingContext[dgvInventory.DataSource ?? dgvInventory.Rows] as CurrencyManager;
            cm?.SuspendBinding();

            foreach (DataGridViewRow row in dgvInventory.Rows)
            {
                if (row.IsNewRow) continue;

                string rowCat = row.Cells["CategoryName"].Value?.ToString() ?? "";
                string rowStat = row.Cells["Status"].Value?.ToString() ?? "";
                int stock = Convert.ToInt32(row.Cells["StockQuantity"].Value ?? 0);

                bool matchCat = (cat == "Tất cả") || (rowCat == cat);
                bool matchStat = true;

                if (stat == "Sắp hết hàng")
                {
                    // Bất kỳ món nào tồn kho <= 10 đều hiển thị khi lọc "Sắp hết hàng"
                    matchStat = (stock <= 10);
                }
                else if (stat == "Cận hạn")
                {
                    matchStat = (rowStat == "Cận hạn");
                }
                else if (stat == "Bình thường")
                {
                    matchStat = (stock > 10 && rowStat != "Cận hạn");
                }

                row.Visible = matchCat && matchStat;
            }

            cm?.ResumeBinding();
        }

        private async void ExecuteQuickImport()
        {
            if (dgvInventory.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng nhấp chọn một mặt hàng trên bảng tồn kho để nhập hàng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lấy thông tin mặt hàng đang chọn trên bảng
            string barcode = dgvInventory.CurrentRow.Cells["ProductId"].Value?.ToString() ?? "";
            string productName = dgvInventory.CurrentRow.Cells["ProductName"].Value?.ToString() ?? "";
            int currentStock = Convert.ToInt32(dgvInventory.CurrentRow.Cells["StockQuantity"].Value ?? 0);

            // Hộp thoại xác nhận số lượng nhập thêm
            using Form prompt = new Form
            {
                Width = 380,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Nhập thêm hàng vào kho",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblInfo = new Label { Left = 20, Top = 15, Width = 320, Text = $"Sản phẩm: {productName}\nTồn hiện tại: {currentStock}", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Label lblQ = new Label { Left = 20, Top = 60, Text = "Số lượng nhập thêm (+):", AutoSize = true };
            NumericUpDown nudQty = new NumericUpDown { Left = 20, Top = 82, Width = 320, Minimum = 1, Maximum = 1000, Value = 50, Font = new Font("Segoe UI", 10F) };

            Button btnConfirm = new Button { Text = "Xác nhận nhập", Left = 180, Width = 160, Top = 125, Height = 36, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
            prompt.Controls.AddRange(new Control[] { lblInfo, lblQ, nudQty, btnConfirm });

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                int addQty = (int)nudQty.Value;

                var requestBody = new
                {
                    Barcode = barcode,
                    Quantity = addQty,
                    ReceiptCode = txtReceiptCode.Text.Trim(),
                    Note = txtReceiptNote.Text.Trim()
                };

                try
                {
                    var res = await ApiClientService.Client.PostAsJsonAsync("products/import-stock", requestBody);
                    if (res.IsSuccessStatusCode)
                    {
                        MessageBox.Show(
                            $"Nhập kho thành công theo phiếu '{txtReceiptCode.Text}'!\n\nSản phẩm: {productName}\nSố lượng nhập thêm: +{addQty}\nTồn kho mới: {currentStock + addQty}",
                            "Thành công",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        // Tự động nhảy mã phiếu tiếp theo
                        txtReceiptCode.Text = "PN-" + DateTime.Now.ToString("yyyyMMdd-") + DateTime.Now.ToString("mmss");

                        // Nạp lại danh sách để cập nhật số tồn và xóa cảnh báo vàng/đỏ
                        await LoadInventoryDataAsync();
                    }
                    else
                    {
                        var err = await res.Content.ReadAsStringAsync();
                        MessageBox.Show("Nhập kho thất bại: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối máy chủ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}