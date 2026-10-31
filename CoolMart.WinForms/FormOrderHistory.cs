using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    // Màn hình xem lại các hóa đơn đã lập (dữ liệu lấy từ API api/orders)
    public partial class FormOrderHistory : Form
    {
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private TextBox txtKeyword = null!;
        private Label lblSummary = null!;
        private Label lblDetailTitle = null!;
        private DataGridView dgvOrders = null!;
        private DataGridView dgvItems = null!;
        private bool _loading = false;

        public FormOrderHistory()
        {
            InitializeUi();
            this.Load += async (s, e) => await LoadOrdersAsync();
        }

        private void InitializeUi()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // 1. Tiêu đề
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(15, 8, 15, 0) };
            Label lblBreadcrumbs = new Label { Text = "Trang chủ  >  Lịch sử hóa đơn", Location = new Point(15, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F) };
            Label lblMainTitle = new Label { Text = "LỊCH SỬ HÓA ĐƠN ĐÃ LẬP", Location = new Point(13, 27), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            pnlHeader.Controls.Add(lblBreadcrumbs);
            pnlHeader.Controls.Add(lblMainTitle);

            // 2. Thanh lọc: từ ngày - đến ngày - từ khóa
            Panel pnlFilter = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
            Label lblFrom = new Label { Text = "Từ ngày:", Location = new Point(15, 17), AutoSize = true };
            dtpFrom = new DateTimePicker { Location = new Point(80, 13), Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            Label lblTo = new Label { Text = "Đến ngày:", Location = new Point(205, 17), AutoSize = true };
            dtpTo = new DateTimePicker { Location = new Point(275, 13), Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            Label lblKeyword = new Label { Text = "Tìm:", Location = new Point(400, 17), AutoSize = true };
            txtKeyword = new TextBox { Location = new Point(435, 13), Width = 190, PlaceholderText = "Mã hóa đơn / thu ngân..." };
            txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LoadOrdersAsync(); };
            Button btnView = new Button
            {
                Text = "🔍 Xem",
                Location = new Point(640, 10),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(24, 30, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnView.FlatAppearance.BorderSize = 0;
            btnView.Click += async (s, e) => await LoadOrdersAsync();
            lblSummary = new Label { Text = "Tổng: 0 hóa đơn", Location = new Point(755, 17), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(40, 167, 69) };
            pnlFilter.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblKeyword, txtKeyword, btnView, lblSummary });

            // 3. Bảng danh sách hóa đơn (vùng giữa)
            Panel pnlOrders = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            Label lblOrdersTitle = new Label { Text = "DANH SÁCH HÓA ĐƠN", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            dgvOrders = CreateGrid();
            AddColumn(dgvOrders, "OrderCode", "Mã hóa đơn", 120);
            AddColumn(dgvOrders, "CreatedDate", "Thời gian", 110);
            AddColumn(dgvOrders, "Cashier", "Thu ngân", 80);
            AddColumn(dgvOrders, "Customer", "Khách hàng", 120);
            AddColumn(dgvOrders, "TotalItems", "SL", 40, right: true);
            AddColumn(dgvOrders, "FinalAmount", "Thành tiền (VND)", 100, right: true, format: "N0");
            AddColumn(dgvOrders, "Reward", "Điểm +", 50, right: true);
            dgvOrders.SelectionChanged += async (s, e) => await LoadSelectedDetailAsync();
            pnlOrders.Controls.Add(dgvOrders);
            pnlOrders.Controls.Add(lblOrdersTitle);

            // 4. Bảng chi tiết các dòng hàng (vùng dưới)
            Panel pnlDetail = new Panel { Dock = DockStyle.Bottom, Height = 240, BackColor = Color.White, Padding = new Padding(12) };
            lblDetailTitle = new Label { Text = "CHI TIẾT HÓA ĐƠN: (chọn một hóa đơn ở trên)", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            dgvItems = CreateGrid();
            AddColumn(dgvItems, "Barcode", "Mã vạch", 100);
            AddColumn(dgvItems, "ProductName", "Tên hàng hóa", 220);
            AddColumn(dgvItems, "Quantity", "Số lượng", 60, right: true);
            AddColumn(dgvItems, "UnitPrice", "Đơn giá", 80, right: true, format: "N0");
            AddColumn(dgvItems, "LineTotal", "Thành tiền", 90, right: true, format: "N0");
            pnlDetail.Controls.Add(dgvItems);
            pnlDetail.Controls.Add(lblDetailTitle);

            // Thứ tự Add: control Fill trước, các control Dock còn lại sau
            this.Controls.Add(pnlOrders);
            this.Controls.Add(pnlDetail);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(24, 30, 48);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            return grid;
        }

        private void AddColumn(DataGridView grid, string name, string header, float weight, bool right = false, string? format = null)
        {
            int idx = grid.Columns.Add(name, header);
            var col = grid.Columns[idx];
            col.FillWeight = weight;
            if (right) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (format != null) col.DefaultCellStyle.Format = format;
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                _loading = true;
                string url = $"orders?fromDate={dtpFrom.Value:yyyy-MM-dd}&toDate={dtpTo.Value:yyyy-MM-dd}&keyword={Uri.EscapeDataString(txtKeyword.Text.Trim())}";
                var list = await ApiClientService.Client.GetFromJsonAsync<List<OrderHistoryDto>>(url) ?? new List<OrderHistoryDto>();

                dgvOrders.Rows.Clear();
                dgvItems.Rows.Clear();
                foreach (var o in list)
                {
                    int idx = dgvOrders.Rows.Add(o.OrderCode, o.CreatedDate.ToString("dd/MM/yyyy HH:mm"), o.CashierUsername,
                        o.CustomerName, o.TotalItems, o.FinalAmount, o.RewardPoints);
                    dgvOrders.Rows[idx].Tag = o.OrderId;
                }
                lblSummary.Text = $"Tổng: {list.Count} hóa đơn  |  Doanh thu: {list.Sum(x => x.FinalAmount):N0} VND";
                _loading = false;

                if (dgvOrders.Rows.Count > 0)
                {
                    dgvOrders.ClearSelection();
                    dgvOrders.Rows[0].Selected = true;
                    await LoadSelectedDetailAsync();
                }
                else
                {
                    lblDetailTitle.Text = "CHI TIẾT HÓA ĐƠN: (không có hóa đơn trong khoảng thời gian này)";
                }
            }
            catch (Exception ex)
            {
                _loading = false;
                MessageBox.Show("Lỗi tải danh sách hóa đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadSelectedDetailAsync()
        {
            if (_loading || dgvOrders.SelectedRows.Count == 0) return;
            if (dgvOrders.SelectedRows[0].Tag is not int orderId) return;

            try
            {
                var res = await ApiClientService.Client.GetFromJsonAsync<OrderDetailResultDto>($"orders/{orderId}/details");
                dgvItems.Rows.Clear();
                if (res == null) return;

                foreach (var it in res.Items)
                {
                    dgvItems.Rows.Add(it.Barcode, it.ProductName, it.Quantity, it.UnitPrice, it.LineTotal);
                }
                lblDetailTitle.Text = $"CHI TIẾT HÓA ĐƠN: {res.OrderCode}   |   Thành tiền: {res.FinalAmount:N0} VND";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết hóa đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class OrderHistoryDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CashierUsername { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal FinalAmount { get; set; }
        public int RewardPoints { get; set; }
    }

    public class OrderLineDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class OrderDetailResultDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public List<OrderLineDto> Items { get; set; } = new();
    }
}
