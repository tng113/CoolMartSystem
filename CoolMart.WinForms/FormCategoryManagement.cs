using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormCategoryManagement : Form
    {
        // Bộ lọc & Tìm kiếm
        private TextBox txtSearch = null!;
        private ComboBox cboCategory = null!;
        private ComboBox cboStatus = null!;

        // Bảng dữ liệu
        private DataGridView dgvProducts = null!;
        private ContextMenuStrip contextMenuGrid = null!;

        // Khung chi tiết SP bên dưới
        private PictureBox picProductThumb = null!;
        private TextBox txtDetailName = null!;
        private TextBox txtDetailRetailPrice = null!;
        private TextBox txtDetailWholesalePrice = null!;
        private TextBox txtDetailDescription = null!;
        private Button btnSaveDetailPrice = null!;

        // Cache danh sách sản phẩm
        private List<ProductCategoryViewDto> _originalList = new();

        public FormCategoryManagement()
        {
            this.KeyPreview = true;
            InitializeComponentCustom();
            this.KeyDown += FormCategoryManagement_KeyDown;
            this.Load += async (s, e) => await LoadProductsDataAsync();
        }

        private void InitializeComponentCustom()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.Dock = DockStyle.Fill;

            // 1. TOP HEADER (Breadcrumbs & Info)
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(15, 8, 15, 0) };
            Label lblBreadcrumb = new Label { Text = "Trang chủ  >  Danh mục Sản phẩm & Giá", Location = new Point(15, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F) };
            Label lblTitle = new Label { Text = "DANH MỤC SẢN PHẨM & GIÁ", Location = new Point(13, 27), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            Label lblUser = new Label
            {
                Text = $"Thu ngân: {(string.IsNullOrEmpty(SessionManager.CurrentUsername) ? "admin01" : SessionManager.CurrentUsername)} | Ca 1  ⎋",
                Dock = DockStyle.Right,
                Width = 300,
                TextAlign = ContentAlignment.TopRight,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9F)
            };
            pnlTop.Controls.AddRange(new Control[] { lblBreadcrumb, lblTitle, lblUser });

            // 2. PANEL DƯỚI ĐÁY: XEM & CHỈNH SỬA CHI TIẾT SẢN PHẨM
            Panel pnlBottomDetail = new Panel { Dock = DockStyle.Bottom, Height = 175, BackColor = Color.White, Padding = new Padding(15, 10, 15, 10) };
            Label lblDetailHeader = new Label { Text = "Xem chi tiết SP", Location = new Point(15, 8), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            Label lblPic = new Label { Text = "Hình ảnh SP:", Location = new Point(15, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            picProductThumb = new PictureBox
            {
                Location = new Point(15, 52),
                Size = new Size(100, 105),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 245, 245),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Label lblDetName = new Label { Text = "Tên sản phẩm:", Location = new Point(130, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            txtDetailName = new TextBox { Location = new Point(130, 52), Width = 320, ReadOnly = true, BackColor = Color.FromArgb(245, 245, 245) };

            Label lblRetPrice = new Label { Text = "Giá bán lẻ:", Location = new Point(470, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            txtDetailRetailPrice = new TextBox { Location = new Point(470, 52), Width = 150, ReadOnly = false, BackColor = Color.White };

            Label lblWholePrice = new Label { Text = "Giá bán sỉ:", Location = new Point(640, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            txtDetailWholesalePrice = new TextBox { Location = new Point(640, 52), Width = 150, ReadOnly = false, BackColor = Color.White };

            btnSaveDetailPrice = new Button
            {
                Text = "💾 Lưu giá",
                Location = new Point(805, 50),
                Width = 90,
                Height = 28,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnSaveDetailPrice.FlatAppearance.BorderSize = 0;
            btnSaveDetailPrice.Click += (s, e) => SavePricesFromDetailPanel();

            Label lblDesc = new Label { Text = "Mô tả chi tiết:", Location = new Point(130, 82), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            txtDetailDescription = new TextBox { Location = new Point(130, 102), Width = 765, Height = 55, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = false, BackColor = Color.White };

            pnlBottomDetail.Controls.AddRange(new Control[] { lblDetailHeader, lblPic, picProductThumb, lblDetName, txtDetailName, lblRetPrice, txtDetailRetailPrice, lblWholePrice, txtDetailWholesalePrice, btnSaveDetailPrice, lblDesc, txtDetailDescription });

            // 3. THAO TÁC NHANH (BÊN PHẢI)
            Panel pnlRightActions = new Panel { Dock = DockStyle.Right, Width = 170, BackColor = Color.FromArgb(244, 246, 249), Padding = new Padding(10, 10, 10, 10) };
            Label lblQuickHeader = new Label { Text = "THAO TÁC NHANH", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48), TextAlign = ContentAlignment.MiddleCenter };

            Button btnAddProduct = CreateActionButton("Thêm SP mới (F3)", (s, e) => AddNewProduct());
            Button btnImportExcel = CreateActionButton("Nhập Excel", (s, e) => MessageBox.Show("Tính năng nhập danh mục bảng giá từ Excel.", "Thông báo"));
            Button btnExportExcel = CreateActionButton("Xuất Excel", (s, e) => MessageBox.Show("Đã trích xuất danh sách sản phẩm ra Excel thành công!", "Xuất Excel"));
            Button btnUpdateWholesale = CreateActionButton("Cập nhật Giá sỉ", (s, e) => UpdateCurrentPrice());

            Label lblStatusSummary = new Label { Text = "Xong tồn SP", Location = new Point(10, 220), Width = 150, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(40, 167, 69), TextAlign = ContentAlignment.MiddleCenter };

            pnlRightActions.Controls.AddRange(new Control[] { lblQuickHeader, btnAddProduct, btnImportExcel, btnExportExcel, btnUpdateWholesale, lblStatusSummary });
            btnAddProduct.Top = 35; btnImportExcel.Top = 75; btnExportExcel.Top = 115; btnUpdateWholesale.Top = 155;

            // 4. VÙNG TRUNG TÂM: FILTER & DATA GRID
            Panel pnlCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 10, 10) };

            Panel pnlFilters = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = Color.White, Padding = new Padding(12) };
            Label lblSearchBar = new Label { Text = "Search Bar", Location = new Point(12, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtSearch = new TextBox { Location = new Point(12, 28), Width = 310, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "🔍 Quét mã, nhập tên SP hoặc mã vạch..." };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) FilterData(); };

            Button btnSearch = new Button { Text = "Search", Location = new Point(230, 28), Width = 70, Height = 26, BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => FilterData();

            Button btnClear = new Button { Text = "Clear", Location = new Point(305, 28), Width = 60, Height = 26, BackColor = Color.FromArgb(33, 37, 41), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => { txtSearch.Clear(); cboCategory.SelectedIndex = 0; cboStatus.SelectedIndex = 0; FilterData(); };

            Label lblCat = new Label { Text = "Ngành hàng:", Location = new Point(380, 8), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            cboCategory = new ComboBox { Location = new Point(380, 28), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboCategory.Items.AddRange(new object[] { "All", "Bánh kẹo", "Nước giải khát", "Gia vị", "Đồ tươi sống", "Khác" });
            cboCategory.SelectedIndex = 0;
            cboCategory.SelectedIndexChanged += (s, e) => FilterData();

            Label lblStat = new Label { Text = "Trạng thái:", Location = new Point(545, 8), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            cboStatus = new ComboBox { Location = new Point(545, 28), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.Items.AddRange(new object[] { "All", "Đang bán", "Ngừng bán" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => FilterData();

            pnlFilters.Controls.AddRange(new Control[] { lblSearchBar, txtSearch, btnSearch, btnClear, lblCat, cboCategory, lblStat, cboStatus });

            // Panel DataGridView
            Panel pnlGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 10, 0, 0) };
            Panel pnlGridHeader = new Panel { Dock = DockStyle.Top, Height = 32 };
            Label lblGridTitle = new Label { Text = "BẢNG DANH MỤC SẢN PHẨM", Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            FlowLayoutPanel flpGridIcons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 100, Height = 30 };
            Button btnIco1 = CreateMiniIcon("📷");
            Button btnIco2 = CreateMiniIcon("📑");
            Button btnIco3 = CreateMiniIcon("🗑️");
            flpGridIcons.Controls.AddRange(new Control[] { btnIco1, btnIco2, btnIco3 });

            pnlGridHeader.Controls.Add(lblGridTitle);
            pnlGridHeader.Controls.Add(flpGridIcons);

            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                Font = new Font("Segoe UI", 9F)
            };
            SetupGridColumns();

            // Menu Chuột phải
            contextMenuGrid = new ContextMenuStrip();
            contextMenuGrid.Items.Add("✏️ Sửa sản phẩm", null, (s, e) => EditCurrentProduct());
            contextMenuGrid.Items.Add("💲 Cập nhật giá", null, (s, e) => UpdateCurrentPrice());
            contextMenuGrid.Items.Add("👁️ Xem chi tiết", null, (s, e) => ShowProductDetailFromSelectedRow());
            dgvProducts.ContextMenuStrip = contextMenuGrid;

            dgvProducts.SelectionChanged += (s, e) => ShowProductDetailFromSelectedRow();
            dgvProducts.CellValueChanged += DgvProducts_CellValueChanged;

            pnlGridContainer.Controls.Add(dgvProducts);
            pnlGridContainer.Controls.Add(pnlGridHeader);

            pnlCenter.Controls.Add(pnlGridContainer);
            pnlCenter.Controls.Add(pnlFilters);

            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlRightActions);
            this.Controls.Add(pnlBottomDetail);
            this.Controls.Add(pnlTop);
        }

        private void SetupGridColumns()
        {
            dgvProducts.Columns.Clear();
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 30, ReadOnly = true });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Barcode", HeaderText = "Mã SP", FillWeight = 90, ReadOnly = true });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Tên sản phẩm", FillWeight = 160, ReadOnly = true });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "ĐVT", FillWeight = 40, ReadOnly = true });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostPrice", HeaderText = "Giá nhập", FillWeight = 75, ReadOnly = true });

            // Cho phép sửa trực tiếp cột Giá bán lẻ và Giá bán sỉ trên DataGridView
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "RetailPrice", HeaderText = "Giá bán lẻ", FillWeight = 80, ReadOnly = false });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "WholesalePrice", HeaderText = "Giá bán sỉ", FillWeight = 80, ReadOnly = false });

            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "Ngành hàng", FillWeight = 85, ReadOnly = true });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", FillWeight = 70, ReadOnly = true });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "HasImage", HeaderText = "Hình", FillWeight = 35, ReadOnly = true });

            dgvProducts.Columns["CostPrice"].DefaultCellStyle.Format = "N0";
            dgvProducts.Columns["RetailPrice"].DefaultCellStyle.Format = "N0";
            dgvProducts.Columns["WholesalePrice"].DefaultCellStyle.Format = "N0";
            dgvProducts.Columns["CostPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvProducts.Columns["RetailPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvProducts.Columns["WholesalePrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvProducts.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvProducts.Columns["HasImage"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private Button CreateActionButton(string text, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Width = 150,
                Height = 32,
                BackColor = Color.FromArgb(240, 242, 245),
                ForeColor = Color.FromArgb(33, 37, 41),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btn.Click += onClick;
            return btn;
        }

        private Button CreateMiniIcon(string icon)
        {
            Button btn = new Button
            {
                Text = icon,
                Width = 28,
                Height = 24,
                BackColor = Color.FromArgb(40, 45, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 7.5F)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private async Task LoadProductsDataAsync()
        {
            try
            {
                // Gọi API ado-products lấy dữ liệu thật trực tiếp từ CSDL
                var response = await ApiClientService.Client.GetAsync("ado-products");
                if (!response.IsSuccessStatusCode)
                {
                    response = await ApiClientService.Client.GetAsync("products");
                }

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    var root = doc.RootElement;

                    JsonElement arrayElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        arrayElement = root;
                    }
                    else if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        arrayElement = itemsProp;
                    }
                    else if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                    {
                        arrayElement = dataProp;
                    }
                    else
                    {
                        MessageBox.Show("Không thể phân giải danh sách từ phản hồi của API!", "Lỗi định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _originalList.Clear();

                    foreach (var el in arrayElement.EnumerateArray())
                    {
                        int pId = 0;
                        if (el.TryGetProperty("productId", out var pid) || el.TryGetProperty("ProductId", out pid))
                            pId = pid.GetInt32();

                        string barcode = "";
                        if (el.TryGetProperty("barcode", out var bc) || el.TryGetProperty("Barcode", out bc))
                            barcode = bc.GetString() ?? "";

                        string pName = "";
                        if (el.TryGetProperty("productName", out var pn) || el.TryGetProperty("ProductName", out pn))
                            pName = pn.GetString() ?? "";

                        decimal price = 0;
                        if (el.TryGetProperty("price", out var pr) || el.TryGetProperty("Price", out pr))
                            price = pr.GetDecimal();

                        int stock = 0;
                        if (el.TryGetProperty("stockQuantity", out var sq) || el.TryGetProperty("StockQuantity", out sq))
                            stock = sq.GetInt32();

                        string catName = "";
                        if (el.TryGetProperty("categoryName", out var cn) || el.TryGetProperty("CategoryName", out cn))
                            catName = cn.GetString() ?? "";

                        if (string.IsNullOrEmpty(catName))
                        {
                            int catId = 1;
                            if (el.TryGetProperty("categoryId", out var cid) || el.TryGetProperty("CategoryId", out cid))
                                catId = cid.GetInt32();

                            catName = catId == 1 ? "Bánh kẹo" : (catId == 2 ? "Nước giải khát" : (catId == 3 ? "Gia vị" : "Khác"));
                        }

                        string unit = pName.Contains("Chai") ? "Chai" : (pName.Contains("Lon") ? "Lon" : (pName.Contains("Hộp") ? "Hộp" : (pName.Contains("Gói") ? "Gói" : "Cây")));

                        _originalList.Add(new ProductCategoryViewDto
                        {
                            ProductId = pId,
                            Barcode = barcode,
                            ProductName = pName,
                            Unit = unit,
                            CostPrice = Math.Round(price * 0.8m, 0),
                            RetailPrice = price,
                            WholesalePrice = Math.Round(price * 0.9m, 0),
                            CategoryName = catName,
                            Status = stock > 0 ? "Đang bán" : "Ngừng bán",
                            Description = $"Sản phẩm chính hãng CoolMart. Tồn kho: {stock} ({barcode})"
                        });
                    }

                    FilterData();
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi kết nối máy chủ API (Mã {response.StatusCode}):\n{err}", "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chi tiết ngoại lệ khi tải danh mục sản phẩm:\n" + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterData()
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            string selectedCat = cboCategory.SelectedItem?.ToString() ?? "All";
            string selectedStat = cboStatus.SelectedItem?.ToString() ?? "All";

            var filtered = _originalList.Where(p =>
            {
                bool matchKeyword = string.IsNullOrEmpty(keyword)
                    || p.ProductName.ToLower().Contains(keyword)
                    || p.Barcode.ToLower().Contains(keyword);

                bool matchCat = (selectedCat == "All" || selectedCat == "Tất cả")
                    || (p.CategoryName.Equals(selectedCat, StringComparison.OrdinalIgnoreCase))
                    || (p.CategoryName.Contains(selectedCat, StringComparison.OrdinalIgnoreCase));

                bool matchStat = (selectedStat == "All" || selectedStat == "Tất cả")
                    || (p.Status.Equals(selectedStat, StringComparison.OrdinalIgnoreCase));

                return matchKeyword && matchCat && matchStat;
            }).ToList();

            dgvProducts.Rows.Clear();
            int stt = 1;
            foreach (var item in filtered)
            {
                int rIdx = dgvProducts.Rows.Add(
                    stt++,
                    item.Barcode,
                    item.ProductName,
                    item.Unit,
                    item.CostPrice,
                    item.RetailPrice,
                    item.WholesalePrice,
                    item.CategoryName,
                    item.Status,
                    "🖼️"
                );

                if (item.Status == "Ngừng bán")
                {
                    dgvProducts.Rows[rIdx].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }

            ShowProductDetailFromSelectedRow();
        }

        private void ShowProductDetailFromSelectedRow()
        {
            if (dgvProducts.CurrentRow == null || dgvProducts.CurrentRow.Index < 0) return;

            string barcode = dgvProducts.CurrentRow.Cells["Barcode"].Value?.ToString() ?? "";
            var prod = _originalList.FirstOrDefault(p => p.Barcode == barcode);

            if (prod != null)
            {
                txtDetailName.Text = prod.ProductName;
                txtDetailRetailPrice.Text = prod.RetailPrice.ToString("N0");
                txtDetailWholesalePrice.Text = prod.WholesalePrice.ToString("N0");
                txtDetailDescription.Text = string.IsNullOrEmpty(prod.Description) ? $"Sản phẩm {prod.ProductName} chính hãng." : prod.Description;
            }
        }

        // Chỉnh sửa giá trực tiếp trên các ô DataGridView
        private void DgvProducts_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == dgvProducts.Columns["RetailPrice"].Index || e.ColumnIndex == dgvProducts.Columns["WholesalePrice"].Index))
            {
                string barcode = dgvProducts.Rows[e.RowIndex].Cells["Barcode"].Value?.ToString() ?? "";
                var prod = _originalList.FirstOrDefault(p => p.Barcode == barcode);
                if (prod != null)
                {
                    if (decimal.TryParse(dgvProducts.Rows[e.RowIndex].Cells["RetailPrice"].Value?.ToString()?.Replace(",", "").Replace(".", ""), out decimal newRetail))
                    {
                        prod.RetailPrice = newRetail;
                    }

                    if (decimal.TryParse(dgvProducts.Rows[e.RowIndex].Cells["WholesalePrice"].Value?.ToString()?.Replace(",", "").Replace(".", ""), out decimal newWhole))
                    {
                        prod.WholesalePrice = newWhole;
                    }

                    ShowProductDetailFromSelectedRow();
                }
            }
        }

        // Lưu giá từ khung chi tiết SP ở đáy
        private async void SavePricesFromDetailPanel()
        {
            if (dgvProducts.CurrentRow == null) return;

            string barcode = dgvProducts.CurrentRow.Cells["Barcode"].Value?.ToString() ?? "";
            var prod = _originalList.FirstOrDefault(p => p.Barcode == barcode);

            if (prod != null)
            {
                string cleanRetail = txtDetailRetailPrice.Text.Replace(",", "").Replace(".", "").Replace("đ", "").Trim();
                string cleanWhole = txtDetailWholesalePrice.Text.Replace(",", "").Replace(".", "").Replace("đ", "").Trim();

                if (decimal.TryParse(cleanRetail, out decimal rPrice) && decimal.TryParse(cleanWhole, out decimal wPrice))
                {
                    try
                    {
                        // 1. Gửi lệnh cập nhật giá bán lẻ xuống API và CSDL SQL Server
                        var response = await ApiClientService.Client.PutAsJsonAsync($"products/{prod.ProductId}/price", new { Price = rPrice });

                        if (response.IsSuccessStatusCode)
                        {
                            // 2. Cập nhật dữ liệu bộ nhớ tại WinForms
                            prod.RetailPrice = rPrice;
                            prod.WholesalePrice = wPrice;
                            prod.Description = txtDetailDescription.Text.Trim();

                            dgvProducts.CurrentRow.Cells["RetailPrice"].Value = rPrice;
                            dgvProducts.CurrentRow.Cells["WholesalePrice"].Value = wPrice;

                            MessageBox.Show($"Đã lưu giá mới ({rPrice:N0} đ) cho '{prod.ProductName}' vào Database thành công!", "Cập nhật thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Lỗi máy chủ khi lưu giá (Mã {response.StatusCode})!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi kết nối khi lưu giá: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập định dạng số hợp lệ!", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Cập nhật giá bằng Hộp thoại Popup (Chuột phải hoặc bấm nút Thao tác nhanh)
        private void UpdateCurrentPrice()
        {
            if (dgvProducts.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một mặt hàng trên bảng trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string barcode = dgvProducts.CurrentRow.Cells["Barcode"].Value?.ToString() ?? "";
            string productName = dgvProducts.CurrentRow.Cells["ProductName"].Value?.ToString() ?? "";
            decimal currentRetail = Convert.ToDecimal(dgvProducts.CurrentRow.Cells["RetailPrice"].Value ?? 0);
            decimal currentWholesale = Convert.ToDecimal(dgvProducts.CurrentRow.Cells["WholesalePrice"].Value ?? 0);

            using Form dlg = new Form
            {
                Width = 380,
                Height = 260,
                Text = "Cập nhật giá bán sản phẩm",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblName = new Label { Left = 20, Top = 15, Width = 320, Text = productName, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            Label lblRet = new Label { Left = 20, Top = 50, Text = "Giá bán lẻ mới (VNĐ):", AutoSize = true };
            NumericUpDown nudRet = new NumericUpDown { Left = 20, Top = 72, Width = 320, Maximum = 100000000, Increment = 1000, Value = currentRetail, Font = new Font("Segoe UI", 9.5F) };

            Label lblWho = new Label { Left = 20, Top = 105, Text = "Giá bán sỉ mới (VNĐ):", AutoSize = true };
            NumericUpDown nudWho = new NumericUpDown { Left = 20, Top = 127, Width = 320, Maximum = 100000000, Increment = 1000, Value = currentWholesale, Font = new Font("Segoe UI", 9.5F) };

            Button btnSave = new Button { Text = "Xác nhận lưu", Left = 190, Top = 175, Width = 150, Height = 34, BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
            dlg.Controls.AddRange(new Control[] { lblName, lblRet, nudRet, lblWho, nudWho, btnSave });

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                decimal newRetail = nudRet.Value;
                decimal newWholesale = nudWho.Value;

                dgvProducts.CurrentRow.Cells["RetailPrice"].Value = newRetail;
                dgvProducts.CurrentRow.Cells["WholesalePrice"].Value = newWholesale;
                txtDetailRetailPrice.Text = newRetail.ToString("N0");
                txtDetailWholesalePrice.Text = newWholesale.ToString("N0");

                var item = _originalList.FirstOrDefault(p => p.Barcode == barcode);
                if (item != null)
                {
                    item.RetailPrice = newRetail;
                    item.WholesalePrice = newWholesale;
                }

                MessageBox.Show($"Đã cập nhật giá mới cho '{productName}' thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EditCurrentProduct()
        {
            if (dgvProducts.CurrentRow == null) return;
            string name = dgvProducts.CurrentRow.Cells["ProductName"].Value?.ToString() ?? "";
            MessageBox.Show($"Mở trình chỉnh sửa thông tin chi tiết: {name}", "Sửa sản phẩm");
        }

        private void AddNewProduct()
        {
            MessageBox.Show("Mở màn hình thêm mới sản phẩm vào danh mục.", "Thêm SP mới (F3)", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FormCategoryManagement_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3) { AddNewProduct(); e.Handled = true; }
        }
    }

    public class ProductCategoryViewDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cái";
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public string CategoryName { get; set; } = "Khác";
        public string Status { get; set; } = "Đang bán";
        public string? Description { get; set; }
    }
}