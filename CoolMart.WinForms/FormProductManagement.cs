using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormProductManagement : Form
    {
        private DataGridView dgvProducts = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboCategory = null!;
        private TextBox txtBarcode = null!;
        private TextBox txtProductName = null!;
        private NumericUpDown nudPrice = null!;
        private NumericUpDown nudStock = null!;
        private Label lblPaging = null!;
        private Button btnPrev = null!;
        private Button btnNext = null!;

        private int _currentPage = 1;
        private readonly int _pageSize = 8;
        private int _totalPages = 1;
        private int _selectedProductId = 0;

        public FormProductManagement()
        {
            InitializeComponentProgrammatically();
            this.Load += async (s, e) => {
                await LoadCategoriesComboAsync();
                await LoadProductsPagedAsync();
            };
        }

        private void InitializeComponentProgrammatically()
        {
            this.BackColor = Color.White;

            // Panel nhập liệu chi tiết bên phải (Right Sidebar)
            Panel pnlInputs = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = Color.FromArgb(248, 249, 250), Padding = new Padding(15) };

            Label lblPanelTitle = new Label { Text = "THÔNG TIN HÀNG HÓA", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 40, ForeColor = Color.FromArgb(24, 30, 48) };
            pnlInputs.Controls.Add(lblPanelTitle);

            int startY = 50;
            txtBarcode = CreateInput(pnlInputs, "Mã vạch (Barcode):", ref startY);
            txtProductName = CreateInput(pnlInputs, "Tên sản phẩm:", ref startY);

            Label lblCat = new Label { Text = "Danh mục nhóm hàng:", Location = new Point(15, startY), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            cboCategory = new ComboBox { Location = new Point(15, startY + 22), Width = 285, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            pnlInputs.Controls.Add(lblCat);
            pnlInputs.Controls.Add(cboCategory);
            startY += 60;

            Label lblPrc = new Label { Text = "Giá bán (VNĐ):", Location = new Point(15, startY), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            nudPrice = new NumericUpDown { Location = new Point(15, startY + 22), Width = 285, Maximum = 10000000, Increment = 1000, Font = new Font("Segoe UI", 10) };
            pnlInputs.Controls.Add(lblPrc);
            pnlInputs.Controls.Add(nudPrice);
            startY += 60;

            Label lblStk = new Label { Text = "Số lượng tồn kho:", Location = new Point(15, startY), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            nudStock = new NumericUpDown { Location = new Point(15, startY + 22), Width = 285, Maximum = 10000, Font = new Font("Segoe UI", 10) };
            pnlInputs.Controls.Add(lblStk);
            pnlInputs.Controls.Add(nudStock);
            startY += 70;

            // Nút Thêm, Sửa, Xóa, Làm mới
            Button btnAdd = CreateActionButton("➕ Thêm mới", Color.FromArgb(40, 167, 69), new Point(15, startY), async (s, e) => await CreateProductAsync());
            Button btnUpdate = CreateActionButton("✏️ Cập nhật", Color.FromArgb(0, 123, 255), new Point(165, startY), async (s, e) => await UpdateProductAsync());
            startY += 45;
            Button btnDelete = CreateActionButton("🗑️ Xóa bỏ", Color.FromArgb(220, 53, 69), new Point(15, startY), async (s, e) => await DeleteProductAsync());
            Button btnClear = CreateActionButton("🔄 Làm mới", Color.FromArgb(108, 117, 125), new Point(165, startY), (s, e) => ClearInputs());
            startY += 55;
            Button btnExport = CreateActionButton("📥 Xuất file CSV (Excel)", Color.FromArgb(23, 162, 184), new Point(15, startY), (s, e) => ExportToCsv());
            btnExport.Width = 285;

            pnlInputs.Controls.Add(btnAdd);
            pnlInputs.Controls.Add(btnUpdate);
            pnlInputs.Controls.Add(btnDelete);
            pnlInputs.Controls.Add(btnClear);
            pnlInputs.Controls.Add(btnExport);

            // Panel thanh công cụ tìm kiếm bên trên
            Panel pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(15, 12, 15, 10), BackColor = Color.White };
            txtSearch = new TextBox { Width = 280, Font = new Font("Segoe UI", 10), Location = new Point(15, 15), PlaceholderText = "Tìm theo tên hoặc mã vạch..." };
            Button btnSearch = new Button { Text = "🔍 Tìm kiếm", Location = new Point(305, 14), Height = 32, Width = 95, BackColor = Color.FromArgb(24, 30, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSearch.Click += async (s, e) => { _currentPage = 1; await LoadProductsPagedAsync(); };

            pnlTopBar.Controls.Add(txtSearch);
            pnlTopBar.Controls.Add(btnSearch);

            // Panel điều hướng phân trang bên dưới
            Panel pnlBottomBar = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.WhiteSmoke, Padding = new Padding(15, 10, 15, 10) };
            btnPrev = new Button { Text = "◀ Trang trước", Width = 110, Height = 32, Location = new Point(15, 9), FlatStyle = FlatStyle.Flat };
            lblPaging = new Label { Text = "Trang 1 / 1", AutoSize = false, Width = 220, Height = 32, Location = new Point(135, 9), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnNext = new Button { Text = "Trang sau ▶", Width = 110, Height = 32, Location = new Point(365, 9), FlatStyle = FlatStyle.Flat };

            btnPrev.Click += async (s, e) => { if (_currentPage > 1) { _currentPage--; await LoadProductsPagedAsync(); } };
            btnNext.Click += async (s, e) => { if (_currentPage < _totalPages) { _currentPage++; await LoadProductsPagedAsync(); } };

            pnlBottomBar.Controls.Add(btnPrev);
            pnlBottomBar.Controls.Add(lblPaging);
            pnlBottomBar.Controls.Add(btnNext);

            // Bảng danh sách DataGridView
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
                ReadOnly = true
            };
            dgvProducts.CellClick += DgvProducts_CellClick;

            // Tiêu chí 44: Tô màu đỏ cam cảnh báo mặt hàng tồn kho <= 10
            dgvProducts.CellFormatting += (s, e) => {
                if (e.RowIndex >= 0 && dgvProducts.Columns[e.ColumnIndex].HeaderText == "Tồn Kho" && e.Value != null)
                {
                    if (int.TryParse(e.Value.ToString(), out int qty) && qty <= 10)
                    {
                        e.CellStyle.BackColor = Color.LightCoral;
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.Font = new Font(dgvProducts.Font, FontStyle.Bold);
                    }
                }
            };
            this.Controls.Add(dgvProducts);
            this.Controls.Add(pnlBottomBar);
            this.Controls.Add(pnlTopBar);
            this.Controls.Add(pnlInputs);
        }

        private TextBox CreateInput(Panel pnl, string labelText, ref int y)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txt = new TextBox { Location = new Point(15, y + 22), Width = 285, Font = new Font("Segoe UI", 10) };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            y += 60;
            return txt;
        }

        private Button CreateActionButton(string text, Color bg, Point loc, EventHandler click)
        {
            Button btn = new Button { Text = text, Location = loc, Width = 135, Height = 36, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += click;
            return btn;
        }

        private async Task LoadCategoriesComboAsync()
        {
            try
            {
                var cats = await ApiClientService.Client.GetFromJsonAsync<List<CategoryItemDto>>("categories");
                if (cats != null)
                {
                    cboCategory.DataSource = cats;
                    cboCategory.DisplayMember = "CategoryName";
                    cboCategory.ValueMember = "CategoryId";
                }
            }
            catch { }
        }

        private async Task LoadProductsPagedAsync()
        {
            try
            {
                string search = txtSearch.Text.Trim();
                string url = $"products/paged?keyword={Uri.EscapeDataString(search)}&pageNumber={_currentPage}&pageSize={_pageSize}";
                var res = await ApiClientService.Client.GetFromJsonAsync<PagedResultDto<ProductViewDto>>(url);
                if (res != null)
                {
                    _currentPage = res.CurrentPage;
                    _totalPages = res.TotalPages == 0 ? 1 : res.TotalPages;

                    dgvProducts.DataSource = res.Items;
                    FormatGridColumns();

                    lblPaging.Text = $"Trang {_currentPage} / {_totalPages} (Tổng: {res.TotalItems} SP)";
                    btnPrev.Enabled = _currentPage > 1;
                    btnNext.Enabled = _currentPage < _totalPages;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy danh sách sản phẩm: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatGridColumns()
        {
            if (dgvProducts.Columns["ProductId"] != null) dgvProducts.Columns["ProductId"].HeaderText = "Mã ID";
            if (dgvProducts.Columns["Barcode"] != null) dgvProducts.Columns["Barcode"].HeaderText = "Mã Vạch";
            if (dgvProducts.Columns["ProductName"] != null) dgvProducts.Columns["ProductName"].HeaderText = "Tên Sản Phẩm";
            if (dgvProducts.Columns["Price"] != null)
            {
                dgvProducts.Columns["Price"].HeaderText = "Đơn Giá";
                dgvProducts.Columns["Price"].DefaultCellStyle.Format = "N0 đ";
            }
            if (dgvProducts.Columns["StockQuantity"] != null) dgvProducts.Columns["StockQuantity"].HeaderText = "Tồn Kho";
            if (dgvProducts.Columns["CategoryName"] != null) dgvProducts.Columns["CategoryName"].HeaderText = "Nhóm Hàng";
            if (dgvProducts.Columns["CategoryId"] != null) dgvProducts.Columns["CategoryId"].Visible = false;
        }

        private void DgvProducts_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvProducts.Rows[e.RowIndex].DataBoundItem is ProductViewDto item)
            {
                _selectedProductId = item.ProductId;
                txtBarcode.Text = item.Barcode;
                txtProductName.Text = item.ProductName;
                nudPrice.Value = item.Price;
                nudStock.Value = item.StockQuantity;
                cboCategory.SelectedValue = item.CategoryId;
            }
        }

        private async Task CreateProductAsync()
        {
            if (string.IsNullOrWhiteSpace(txtProductName.Text)) return;
            var prod = new { Barcode = txtBarcode.Text.Trim(), ProductName = txtProductName.Text.Trim(), Price = nudPrice.Value, StockQuantity = (int)nudStock.Value, CategoryId = (int)(cboCategory.SelectedValue ?? 1) };
            var res = await ApiClientService.Client.PostAsJsonAsync("products", prod);
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show("Thêm sản phẩm thành công!", "Thông báo");
                await LoadProductsPagedAsync();
                ClearInputs();
            }
        }

        private async Task UpdateProductAsync()
        {
            if (_selectedProductId == 0) return;
            var prod = new { ProductId = _selectedProductId, Barcode = txtBarcode.Text.Trim(), ProductName = txtProductName.Text.Trim(), Price = nudPrice.Value, StockQuantity = (int)nudStock.Value, CategoryId = (int)(cboCategory.SelectedValue ?? 1) };
            var res = await ApiClientService.Client.PutAsJsonAsync($"products/{_selectedProductId}", prod);
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show("Cập nhật thành công!", "Thông báo");
                await LoadProductsPagedAsync();
            }
        }

        private async Task DeleteProductAsync()
        {
            if (_selectedProductId == 0) return;
            if (MessageBox.Show($"Xóa sản phẩm ID {_selectedProductId}?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var res = await ApiClientService.Client.DeleteAsync($"products/{_selectedProductId}");
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đã xóa!", "Thông báo");
                    await LoadProductsPagedAsync();
                    ClearInputs();
                }
            }
        }

        private void ClearInputs()
        {
            _selectedProductId = 0;
            txtBarcode.Clear();
            txtProductName.Clear();
            nudPrice.Value = 0;
            nudStock.Value = 0;
        }

        // Tiêu chí 49: Xuất báo cáo CSV UTF-8
        private async void ExportToCsv()
        {
            using SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = "BaoCao_TonKho_CoolMart.csv" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Mã ID,Mã Vạch,Tên Sản Phẩm,Đơn Giá,Tồn Kho,Nhóm Hàng");

                // Xuất TOÀN BỘ mặt hàng: lấy lần lượt từng trang (API giới hạn tối đa 50 dòng/trang)
                int page = 1, totalPages = 1, count = 0;
                do
                {
                    var res = await ApiClientService.Client.GetFromJsonAsync<PagedResultDto<ProductViewDto>>(
                        $"products/paged?keyword=&pageNumber={page}&pageSize=50");
                    if (res == null || res.Items == null) break;

                    totalPages = res.TotalPages == 0 ? 1 : res.TotalPages;
                    foreach (var item in res.Items)
                    {
                        string name = (item.ProductName ?? "").Replace("\"", "\"\"");
                        string cat = (item.CategoryName ?? "").Replace("\"", "\"\"");
                        sb.AppendLine($"{item.ProductId},{item.Barcode},\"{name}\",{item.Price},{item.StockQuantity},\"{cat}\"");
                        count++;
                    }
                    page++;
                } while (page <= totalPages);

                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show($"Xuất báo cáo thành công {count} sản phẩm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class ProductViewDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CategoryItemDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    public class PagedResultDto<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<T> Items { get; set; } = new();
    }
}