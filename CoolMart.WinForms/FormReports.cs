using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormReports : Form
    {
        // Bộ lọc ngày
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private ComboBox cboPreset = null!;

        // Biểu đồ
        private Panel pnlBarChart = null!;
        private Panel pnlPieChart = null!;

        // Thẻ KPI
        private Label lblTotalRevenue = null!;
        private Label lblTotalProfit = null!;
        private Label lblAvgBill = null!;

        // Bảng Top SP
        private DataGridView dgvTopProducts = null!;

        // Dữ liệu hiển thị
        private List<HourlyDto> _hourlyData = new();
        private List<CategoryShareDto> _categoryData = new();
        private List<TopProductDto> _topProductsData = new();

        public FormReports()
        {
            this.KeyPreview = true;
            InitializeComponentCustom();
            this.Load += async (s, e) => await LoadAnalyticsDataAsync();
        }

        private void InitializeComponentCustom()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.Dock = DockStyle.Fill;

            // 1. TOP HEADER
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(15, 8, 15, 0) };
            Label lblBreadcrumb = new Label { Text = "Trang chủ  >  AnalyticsView - phân hệ F5", Location = new Point(15, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F) };
            Label lblTitle = new Label { Text = "BÁO CÁO & THỐNG KÊ DOANH THU", Location = new Point(13, 27), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
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

            // 2. MAIN LAYOUT (CHIA 2 CỘT TRÁI - PHẢI)
            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12, 10, 12, 10)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // ================== CỘT TRÁI (BỘ LỌC + BIỂU ĐỒ CỘT + 3 THẺ KPI) ==================
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };

            // Khung bộ lọc
            Panel pnlFilters = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(12, 8, 12, 8) };
            Label lblFilterTitle = new Label { Text = "Thanh thời gian", Location = new Point(12, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            Label lblFrom = new Label { Text = "From:", Location = new Point(12, 32), AutoSize = true, ForeColor = Color.DimGray };
            dtpFrom = new DateTimePicker { Location = new Point(52, 29), Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            Label lblTo = new Label { Text = "To:", Location = new Point(175, 32), AutoSize = true, ForeColor = Color.DimGray };
            dtpTo = new DateTimePicker { Location = new Point(200, 29), Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            Label lblPreset = new Label { Text = "Tạm:", Location = new Point(325, 32), AutoSize = true, ForeColor = Color.DimGray };
            cboPreset = new ComboBox { Location = new Point(360, 29), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cboPreset.Items.AddRange(new object[] { "Hôm nay", "Hôm qua", "Tuần này", "Tháng này" });
            cboPreset.SelectedIndex = 0;
            cboPreset.SelectedIndexChanged += async (s, e) => await OnPresetChanged();

            dtpFrom.ValueChanged += async (s, e) => await LoadAnalyticsDataAsync();
            dtpTo.ValueChanged += async (s, e) => await LoadAnalyticsDataAsync();

            pnlFilters.Controls.AddRange(new Control[] { lblFilterTitle, lblFrom, dtpFrom, lblTo, dtpTo, lblPreset, cboPreset });

            // 3 THẺ KPI PHÍA DƯỚI BÊN TRÁI
            Panel pnlLeftBottomKpis = new Panel { Dock = DockStyle.Bottom, Height = 105, Margin = new Padding(0, 8, 0, 0) };
            TableLayoutPanel tlpKpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            var card1 = CreateKpiCard("TỔNG DOANH THU", "0 VND", "📈", Color.FromArgb(24, 30, 48), out lblTotalRevenue);
            var card2 = CreateKpiCard("TỔNG LỢI NHUẬN", "0 VND", "🌱", Color.FromArgb(40, 167, 69), out lblTotalProfit);
            var card3 = CreateKpiCard("HÓA ĐƠN TRUNG BÌNH", "0 VND", "📜", Color.FromArgb(210, 130, 20), out lblAvgBill);

            tlpKpis.Controls.Add(card1, 0, 0);
            tlpKpis.Controls.Add(card2, 1, 0);
            tlpKpis.Controls.Add(card3, 2, 0);
            pnlLeftBottomKpis.Controls.Add(tlpKpis);

            // BIỂU ĐỒ CỘT DOANH THU THEO NGÀY
            Panel pnlBarContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 8, 0, 8) };
            Label lblBarTitle = new Label { Text = "DOANH THU THEO NGÀY", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            pnlBarChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlBarChart.Paint += DrawHourlyBarChart;

            pnlBarContainer.Controls.Add(pnlBarChart);
            pnlBarContainer.Controls.Add(lblBarTitle);

            pnlLeft.Controls.Add(pnlBarContainer);
            pnlLeft.Controls.Add(pnlLeftBottomKpis);
            pnlLeft.Controls.Add(pnlFilters);

            // ================== CỘT PHẢI (BIỂU ĐỒ TRÒN + TOP SẢN PHẨM) ==================
            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };

            // Khung biểu đồ tròn Tỉ trọng ngành hàng
            Panel pnlPieContainer = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.White, Padding = new Padding(12) };
            Label lblPieTitle = new Label { Text = "TỈ TRỌNG DOANH THU THEO NGÀNH HÀNG", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            pnlPieChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlPieChart.Paint += DrawCategoryPieChart;

            pnlPieContainer.Controls.Add(pnlPieChart);
            pnlPieContainer.Controls.Add(lblPieTitle);

            // Khung Top sản phẩm bán chạy
            Panel pnlTopProdContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 8, 0, 0) };
            Label lblTopTitle = new Label { Text = "TOP SẢN PHẨM BÁN CHẠY NHẤT", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            dgvTopProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 8.5F)
            };
            SetupTopProductsGrid();

            pnlTopProdContainer.Controls.Add(dgvTopProducts);
            pnlTopProdContainer.Controls.Add(lblTopTitle);

            pnlRight.Controls.Add(pnlTopProdContainer);
            pnlRight.Controls.Add(pnlPieContainer);

            tlpMain.Controls.Add(pnlLeft, 0, 0);
            tlpMain.Controls.Add(pnlRight, 1, 0);

            this.Controls.Add(tlpMain);
            this.Controls.Add(pnlTop);
        }

        private void SetupTopProductsGrid()
        {
            dgvTopProducts.Columns.Clear();
            dgvTopProducts.Columns.Add("Rank", "Hạng");
            dgvTopProducts.Columns.Add("Barcode", "Mã SP");
            dgvTopProducts.Columns.Add("ProductName", "Tên SP");
            dgvTopProducts.Columns.Add("Unit", "ĐVT");
            dgvTopProducts.Columns.Add("Quantity", "Số lượng");
            dgvTopProducts.Columns.Add("TotalRevenue", "Thành tiền");

            dgvTopProducts.Columns["Rank"].FillWeight = 30;
            dgvTopProducts.Columns["Barcode"].FillWeight = 65;
            dgvTopProducts.Columns["ProductName"].FillWeight = 130;
            dgvTopProducts.Columns["Unit"].FillWeight = 35;
            dgvTopProducts.Columns["Quantity"].FillWeight = 45;
            dgvTopProducts.Columns["TotalRevenue"].FillWeight = 75;

            dgvTopProducts.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvTopProducts.Columns["TotalRevenue"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvTopProducts.Columns["TotalRevenue"].DefaultCellStyle.Format = "N0";
        }

        private Panel CreateKpiCard(string title, string defaultVal, string icon, Color valColor, out Label lblVal)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(3), Padding = new Padding(10) };
            Label lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.Gray };
            lblVal = new Label { Text = defaultVal, Location = new Point(10, 32), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = valColor };
            Label lblIco = new Label { Text = icon, Location = new Point(140, 28), AutoSize = true, Font = new Font("Segoe UI", 18F) };

            card.Controls.AddRange(new Control[] { lblTitle, lblVal, lblIco });
            return card;
        }

        private async Task OnPresetChanged()
        {
            DateTime now = DateTime.Today;
            switch (cboPreset.SelectedItem?.ToString())
            {
                case "Hôm nay":
                    dtpFrom.Value = now;
                    dtpTo.Value = now;
                    break;
                case "Hôm qua":
                    dtpFrom.Value = now.AddDays(-1);
                    dtpTo.Value = now.AddDays(-1);
                    break;
                case "Tuần này":
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    dtpFrom.Value = now.AddDays(-1 * diff);
                    dtpTo.Value = now;
                    break;
                case "Tháng này":
                    dtpFrom.Value = new DateTime(now.Year, now.Month, 1);
                    dtpTo.Value = now;
                    break;
            }
            await LoadAnalyticsDataAsync();
        }

        private async Task LoadAnalyticsDataAsync()
        {
            try
            {
                string url = $"reports/dashboard-analytics?fromDate={dtpFrom.Value:yyyy-MM-dd}&toDate={dtpTo.Value:yyyy-MM-dd}";
                var response = await ApiClientService.Client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // 1. Đọc dữ liệu KPI
                    if (root.TryGetProperty("kpi", out var kpiProp))
                    {
                        decimal rev = kpiProp.GetProperty("totalRevenue").GetDecimal();
                        decimal profit = kpiProp.GetProperty("totalProfit").GetDecimal();
                        decimal avg = kpiProp.GetProperty("averageBill").GetDecimal();

                        lblTotalRevenue.Text = rev >= 1000000 ? $"{(rev / 1000000m):N1}M VND" : $"{rev:N0} VND";
                        lblTotalProfit.Text = profit >= 1000000 ? $"{(profit / 1000000m):N1}M VND" : $"{profit:N0} VND";
                        lblAvgBill.Text = $"{avg:N0} VND";
                    }

                    // 2. Đọc dữ liệu doanh thu theo giờ
                    _hourlyData.Clear();
                    if (root.TryGetProperty("hourly", out var hProp) && hProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in hProp.EnumerateArray())
                        {
                            _hourlyData.Add(new HourlyDto
                            {
                                TimeSlot = el.GetProperty("timeSlot").GetString() ?? "",
                                Revenue = el.GetProperty("revenue").GetDecimal()
                            });
                        }
                    }

                    // 3. Đọc dữ liệu Top sản phẩm
                    _topProductsData.Clear();
                    dgvTopProducts.Rows.Clear();
                    if (root.TryGetProperty("topProducts", out var topProp) && topProp.ValueKind == JsonValueKind.Array)
                    {
                        int rank = 1;
                        foreach (var el in topProp.EnumerateArray())
                        {
                            string bc = el.GetProperty("barcode").GetString() ?? "";
                            string name = el.GetProperty("productName").GetString() ?? "";
                            string unit = el.GetProperty("unit").GetString() ?? "";
                            int qty = el.GetProperty("quantity").GetInt32();
                            decimal total = el.GetProperty("totalRevenue").GetDecimal();

                            dgvTopProducts.Rows.Add(rank++, bc, name, unit, qty, total);
                        }
                    }

                    // 4. Đọc dữ liệu Tỉ trọng ngành hàng
                    _categoryData.Clear();
                    if (root.TryGetProperty("categoryShares", out var catProp) && catProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in catProp.EnumerateArray())
                        {
                            _categoryData.Add(new CategoryShareDto
                            {
                                CategoryName = el.GetProperty("categoryName").GetString() ?? "Khác",
                                Revenue = el.GetProperty("revenue").GetDecimal()
                            });
                        }
                    }
                }
            }
            catch
            {
                // Giữ lại dữ liệu cũ nếu gặp sự cố kết nối
            }

            await LoadDailyRevenueAsync();

            pnlBarChart.Invalidate();
            pnlPieChart.Invalidate();
        }

        // Biểu đồ cột doanh thu theo ngày: dữ liệu lấy từ Stored Procedure sp_RevenueByDay (qua API)
        private async Task LoadDailyRevenueAsync()
        {
            try
            {
                DateTime to = dtpTo.Value.Date;
                DateTime from = dtpFrom.Value.Date;
                if ((to - from).Days < 6) from = to.AddDays(-6);    // luôn hiện tối thiểu 7 ngày để so sánh
                if ((to - from).Days > 13) from = to.AddDays(-13);  // tối đa 14 cột cho dễ nhìn

                string url = $"reports/revenue-by-day?fromDate={from:yyyy-MM-dd}&toDate={to:yyyy-MM-dd}";
                var rows = await ApiClientService.Client.GetFromJsonAsync<List<DailyRevenueDto>>(url) ?? new List<DailyRevenueDto>();

                _hourlyData.Clear();
                for (DateTime d = from; d <= to; d = d.AddDays(1))
                {
                    var row = rows.FirstOrDefault(r => r.Date.Date == d);
                    _hourlyData.Add(new HourlyDto { TimeSlot = d.ToString("dd/MM"), Revenue = row?.Revenue ?? 0 });
                }
            }
            catch
            {
                _hourlyData.Clear();
            }
        }

        // Vẽ biểu đồ cột doanh thu theo giờ
        private void DrawHourlyBarChart(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = pnlBarChart.Width;
            int h = pnlBarChart.Height;

            if (_hourlyData.Count == 0) return;

            decimal maxRev = _hourlyData.Max(d => d.Revenue);
            if (maxRev == 0) maxRev = 100000;

            int padLeft = 45;
            int padBottom = 30;
            int chartW = w - padLeft - 20;
            int chartH = h - padBottom - 20;

            // Vẽ trục tọa độ
            g.DrawLine(Pens.LightGray, padLeft, h - padBottom, w - 20, h - padBottom);
            g.DrawLine(Pens.LightGray, padLeft, 20, padLeft, h - padBottom);

            int barWidth = Math.Max(12, chartW / (_hourlyData.Count * 2));
            int gap = chartW / _hourlyData.Count;

            using Brush barBrush = new SolidBrush(Color.FromArgb(23, 120, 208));
            using Font fontLabel = new Font("Segoe UI", 7.5F);

            for (int i = 0; i < _hourlyData.Count; i++)
            {
                var item = _hourlyData[i];
                int barH = (int)((item.Revenue / maxRev) * (chartH - 25));
                int x = padLeft + (i * gap) + (gap - barWidth) / 2;
                int y = (h - padBottom) - barH;

                // Vẽ cột
                g.FillRectangle(barBrush, x, y, barWidth, barH);

                // Nhãn giờ
                g.DrawString(item.TimeSlot, fontLabel, Brushes.DimGray, x - 5, h - padBottom + 6);

                // Giá trị trên cột (nếu có doanh thu)
                if (item.Revenue > 0)
                {
                    string txtVal = item.Revenue >= 1000000 ? $"{(item.Revenue / 1000000m):0.0}M" : $"{item.Revenue / 1000m:0}k";
                    g.DrawString(txtVal, fontLabel, Brushes.Black, x - 2, y - 14);
                }
            }
        }

        // Vẽ biểu đồ tròn Donut tỉ trọng ngành hàng
        private void DrawCategoryPieChart(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            decimal total = _categoryData.Sum(c => c.Revenue);
            Color[] colors = new Color[]
            {
                Color.FromArgb(0, 123, 255),
                Color.FromArgb(220, 53, 69),
                Color.FromArgb(253, 126, 20),
                Color.FromArgb(40, 167, 69),
                Color.FromArgb(108, 117, 125)
            };

            int diameter = Math.Min(pnlPieChart.Height - 30, 140);
            Rectangle rect = new Rectangle(15, (pnlPieChart.Height - diameter) / 2, diameter, diameter);

            if (total == 0)
            {
                g.DrawEllipse(new Pen(Color.FromArgb(230, 230, 230), 16), rect);
                g.DrawString("Chưa có giao dịch", this.Font, Brushes.Gray, 30, pnlPieChart.Height / 2 - 8);
                return;
            }

            float startAngle = 0;
            int colorIdx = 0;

            for (int i = 0; i < _categoryData.Count; i++)
            {
                var item = _categoryData[i];
                float sweepAngle = (float)((item.Revenue / total) * 360m);
                using Brush b = new SolidBrush(colors[colorIdx % colors.Length]);
                g.FillPie(b, rect, startAngle, sweepAngle);

                // Chú thích nhãn bên phải
                int legY = 25 + (i * 22);
                g.FillRectangle(b, 175, legY, 12, 12);
                g.DrawString($"{item.CategoryName} ({((item.Revenue / total) * 100):0}%)", this.Font, Brushes.Black, 192, legY - 2);

                startAngle += sweepAngle;
                colorIdx++;
            }

            // Tạo lỗ rỗng ở giữa để thành Donut Chart
            int holeSize = (int)(diameter * 0.52);
            Rectangle holeRect = new Rectangle(rect.X + (diameter - holeSize) / 2, rect.Y + (diameter - holeSize) / 2, holeSize, holeSize);
            g.FillEllipse(Brushes.White, holeRect);
        }
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal Revenue { get; set; }
    }

    public class HourlyDto
    {
        public string TimeSlot { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class CategoryShareDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TopProductDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}