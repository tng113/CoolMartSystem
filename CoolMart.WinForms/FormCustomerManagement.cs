using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Azure;

namespace CoolMart.WinForms
{
    public partial class FormCustomerManagement : Form
    {
        // Bộ lọc & Tìm kiếm
        private TextBox txtSearch = null!;
        private ComboBox cboCustomerType = null!;
        private ComboBox cboStatus = null!;

        // Bảng dữ liệu
        private DataGridView dgvCustomers = null!;
        private ContextMenuStrip contextMenuGrid = null!;

        // Bảng nhỏ Lịch sử & Đổi voucher bên phải
        private Label lblCustInfoId = null!;
        private Label lblCustInfoName = null!;
        private Label lblCustInfoLevel = null!;
        private Label lblCustInfoSpent = null!;
        private DataGridView dgvHistory = null!;
        private ListBox lstVouchers = null!;

        // Thẻ KPI Cards
        private Label lblKpiTotalCustomers = null!;
        private Label lblKpiNewMembers = null!;
        private Label lblKpiTotalPoints = null!;

        // Cache danh sách khách hàng
        private List<CustomerViewDto> _originalList = new();

        public FormCustomerManagement()
        {
            this.KeyPreview = true;
            InitializeComponentCustom();
            this.KeyDown += FormCustomerManagement_KeyDown;
            this.Load += async (s, e) => await LoadCustomerDataAsync();
        }

        private void InitializeComponentCustom()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.Dock = DockStyle.Fill;

            // 1. TOP HEADER
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(15, 8, 15, 0) };
            Label lblBreadcrumb = new Label { Text = "Trang chủ  >  Khách hàng & Tích điểm - phân hệ F5", Location = new Point(15, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F) };
            Label lblTitle = new Label { Text = "QUẢN LÝ KHÁCH HÀNG & TÍCH ĐIỂM", Location = new Point(13, 27), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
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

            // 2. BOTTOM KPI CARDS
            Panel pnlBottomKpi = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.Transparent, Padding = new Padding(12, 5, 12, 10) };
            TableLayoutPanel tlpKpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            var card1 = CreateKpiCard("TỔNG KHÁCH HÀNG", "0", "👥", Color.FromArgb(24, 30, 48), out lblKpiTotalCustomers);
            var card2 = CreateKpiCard("THÀNH VIÊN MỚI (Tháng)", "0", "🌱", Color.FromArgb(40, 167, 69), out lblKpiNewMembers);
            var card3 = CreateKpiCard("TỔNG ĐIỂM TÍCH LŨY", "0", "🪙", Color.FromArgb(230, 140, 0), out lblKpiTotalPoints);

            tlpKpis.Controls.Add(card1, 0, 0);
            tlpKpis.Controls.Add(card2, 1, 0);
            tlpKpis.Controls.Add(card3, 2, 0);
            pnlBottomKpi.Controls.Add(tlpKpis);

            // 3. RIGHT PANEL
            Panel pnlRight = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.White, Padding = new Padding(12, 10, 12, 10), Margin = new Padding(5) };

            Label lblDetailHead = new Label { Text = "Chi Tiết Khách Hàng / Đổi Thưởng", Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            Panel pnlCustSummary = new Panel { Dock = DockStyle.Top, Height = 95, Padding = new Padding(0, 5, 0, 5) };
            PictureBox picAvatar = new PictureBox
            {
                Location = new Point(5, 10),
                Size = new Size(55, 55),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(235, 240, 250),
                Image = SystemIcons.Information.ToBitmap(),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            lblCustInfoId = new Label { Text = "ID: ---", Location = new Point(70, 8), AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.DimGray };
            lblCustInfoName = new Label { Text = "Name: ---", Location = new Point(70, 26), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            lblCustInfoLevel = new Label { Text = "Level: ---", Location = new Point(70, 44), AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.Purple };
            lblCustInfoSpent = new Label { Text = "Total spent: 0 VND", Location = new Point(70, 62), AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(40, 167, 69) };
            pnlCustSummary.Controls.AddRange(new Control[] { picAvatar, lblCustInfoId, lblCustInfoName, lblCustInfoLevel, lblCustInfoSpent });

            Label lblHistoryHead = new Label { Text = "Lịch Sử Tích Điểm", Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                Font = new Font("Segoe UI", 8F)
            };
            dgvHistory.Columns.Add("STT", "STT");
            dgvHistory.Columns.Add("Date", "Lịch Sử Tích Điểm");
            dgvHistory.Columns.Add("Spent", "Tổng Chi Tiêu");
            dgvHistory.Columns["STT"].FillWeight = 30;

            Label lblVoucherHead = new Label { Text = "ĐỔI QUÀ / VOUCHER", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            lstVouchers = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 8.5F) };
            lstVouchers.Items.AddRange(new object[]
            {
                "🎁 Tiêu phẩm: Voucher 3.000 VND (300 điểm)",
                "🎁 Tiêu phẩm: Voucher 5.000 VND (500 điểm)",
                "🎁 Tiêu phẩm: Voucher 10.000 VND (1.000 điểm)",
                "🎁 Tiêu phẩm: Voucher 20.000 VND (2.000 điểm)",
                "🎁 Nón bảo hiểm CoolMart VIP (1.500 điểm)"
            });
            lstVouchers.DoubleClick += (s, e) => RedeemVoucher();

            pnlRight.Controls.Add(lstVouchers);
            pnlRight.Controls.Add(lblVoucherHead);
            pnlRight.Controls.Add(dgvHistory);
            pnlRight.Controls.Add(lblHistoryHead);
            pnlRight.Controls.Add(pnlCustSummary);
            pnlRight.Controls.Add(lblDetailHead);

            // 4. MAIN CENTER PANEL
            Panel pnlCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 10, 5) };

            Panel pnlFilters = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.White, Padding = new Padding(10) };

            Label lblSearchTitle = new Label { Text = "Thanh tìm kiếm & Thao tác", Location = new Point(10, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            txtSearch = new TextBox { Location = new Point(10, 28), Width = 230, Font = new Font("Segoe UI", 9F), PlaceholderText = "🔍 Nhập SĐT hoặc Tên khách hàng..." };
            txtSearch.TextChanged += (s, e) => FilterData();

            Label lblType = new Label { Text = "Loại khách hàng:", Location = new Point(255, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            cboCustomerType = new ComboBox { Location = new Point(255, 28), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cboCustomerType.Items.AddRange(new object[] { "Tất cả", "Thành viên", "VIP" });
            cboCustomerType.SelectedIndex = 0;
            cboCustomerType.SelectedIndexChanged += (s, e) => FilterData();

            Label lblStat = new Label { Text = "Trạng thái:", Location = new Point(380, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            cboStatus = new ComboBox { Location = new Point(380, 28), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.Items.AddRange(new object[] { "Tất cả", "Hoạt động", "Khóa" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => FilterData();

            Button btnImportExcel = new Button
            {
                Text = "Import Excel",
                Location = new Point(510, 24),
                Width = 100,
                Height = 32,
                BackColor = Color.FromArgb(33, 37, 41),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnImportExcel.FlatAppearance.BorderSize = 0;
            btnImportExcel.Click += (s, e) => MessageBox.Show("Tính năng nhập danh sách khách hàng từ Excel.", "Import Excel");

            Button btnAddCust = new Button
            {
                Text = "Thêm Khách Hàng (F6)",
                Location = new Point(620, 24),
                Width = 160,
                Height = 32,
                BackColor = Color.FromArgb(13, 20, 36),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddCust.FlatAppearance.BorderSize = 0;
            btnAddCust.Click += (s, e) => AddNewCustomerDialog();

            pnlFilters.Controls.AddRange(new Control[] { lblSearchTitle, txtSearch, lblType, cboCustomerType, lblStat, cboStatus, btnImportExcel, btnAddCust });

            Panel pnlGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), Margin = new Padding(0, 10, 0, 0) };
            Panel pnlGridHeader = new Panel { Dock = DockStyle.Top, Height = 30 };
            Label lblGridTitle = new Label { Text = "DANH SÁCH KHÁCH HÀNG THÀNH VIÊN", Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };

            FlowLayoutPanel flpGridIcons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 100, Height = 30 };
            Button btnIco1 = CreateMiniIcon("📷");
            Button btnIco2 = CreateMiniIcon("📑");
            Button btnIco3 = CreateMiniIcon("🗑️");
            flpGridIcons.Controls.AddRange(new Control[] { btnIco1, btnIco2, btnIco3 });

            pnlGridHeader.Controls.Add(lblGridTitle);
            pnlGridHeader.Controls.Add(flpGridIcons);

            dgvCustomers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9F)
            };
            SetupGridColumns();

            contextMenuGrid = new ContextMenuStrip();
            contextMenuGrid.Items.Add("✏️ Sửa", null, (s, e) => EditCustomer());
            contextMenuGrid.Items.Add("🎁 Tích Điểm Thủ Công", null, (s, e) => AddManualPoints());
            contextMenuGrid.Items.Add("🕒 Xem Lịch Sử Giao Dịch", null, (s, e) => ViewCustomerHistory());
            contextMenuGrid.Items.Add("❌ Xóa", null, (s, e) => DeleteCustomer());
            dgvCustomers.ContextMenuStrip = contextMenuGrid;

            dgvCustomers.SelectionChanged += (s, e) => ShowCustomerDetailFromSelectedRow();

            pnlGridContainer.Controls.Add(dgvCustomers);
            pnlGridContainer.Controls.Add(pnlGridHeader);

            pnlCenter.Controls.Add(pnlGridContainer);
            pnlCenter.Controls.Add(pnlFilters);

            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlBottomKpi);
            this.Controls.Add(pnlTop);
        }

        private void SetupGridColumns()
        {
            dgvCustomers.Columns.Clear();
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 30 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerId", HeaderText = "Mã KH", FillWeight = 70 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Họ Tên", FillWeight = 110 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "PhoneNumber", HeaderText = "SĐT", FillWeight = 85 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", FillWeight = 110 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "MembershipRank", HeaderText = "Hạng", FillWeight = 65 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "RewardPoints", HeaderText = "Điểm Hiện Tại", FillWeight = 75 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSpent", HeaderText = "Tổng Chi Tiêu", FillWeight = 80 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Ngày Đăng Ký", FillWeight = 75 });
            dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng Thái", FillWeight = 65 });

            dgvCustomers.Columns["RewardPoints"].DefaultCellStyle.Format = "N0";
            dgvCustomers.Columns["TotalSpent"].DefaultCellStyle.Format = "N0";
            dgvCustomers.Columns["RewardPoints"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCustomers.Columns["TotalSpent"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCustomers.Columns["MembershipRank"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvCustomers.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private Panel CreateKpiCard(string title, string defaultVal, string icon, Color valColor, out Label lblVal)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(5), Padding = new Padding(12) };
            Label lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.Gray };
            lblVal = new Label { Text = defaultVal, Location = new Point(12, 30), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = valColor };
            Label lblIco = new Label { Text = icon, Location = new Point(190, 25), AutoSize = true, Font = new Font("Segoe UI", 20F) };

            card.Controls.AddRange(new Control[] { lblTitle, lblVal, lblIco });
            return card;
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

        private async Task LoadCustomerDataAsync()
        {
            try
            {
                var response = await ApiClientService.Client.GetAsync("customers");
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    var root = doc.RootElement;

                    JsonElement arr = root.ValueKind == JsonValueKind.Array ? root : (root.TryGetProperty("items", out var it) ? it : root);

                    _originalList.Clear();
                    foreach (var el in arr.EnumerateArray())
                    {
                        int id = 0;
                        if (el.TryGetProperty("customerId", out var idProp) || el.TryGetProperty("CustomerId", out idProp))
                            id = idProp.GetInt32();

                        string name = "";
                        if (el.TryGetProperty("customerName", out var nProp) || el.TryGetProperty("CustomerName", out nProp))
                            name = nProp.GetString() ?? "";

                        string phone = "";
                        if (el.TryGetProperty("phoneNumber", out var pProp) || el.TryGetProperty("PhoneNumber", out pProp))
                            phone = pProp.GetString() ?? "";

                        int points = 0;
                        if (el.TryGetProperty("rewardPoints", out var ptsProp) || el.TryGetProperty("RewardPoints", out ptsProp))
                            points = ptsProp.GetInt32();

                        decimal spent = 0;
                        if (el.TryGetProperty("totalSpent", out var spProp) || el.TryGetProperty("TotalSpent", out spProp))
                            spent = spProp.GetDecimal();

                        string email = "";
                        if (el.TryGetProperty("email", out var emProp) || el.TryGetProperty("Email", out emProp))
                            email = emProp.GetString() ?? "";

                        string createdDate = "19/07/2026";
                        if (el.TryGetProperty("createdDate", out var cdProp) || el.TryGetProperty("CreatedDate", out cdProp))
                            createdDate = cdProp.GetString() ?? "19/07/2026";

                        // Đọc hạng thành viên thật từ database trả về
                        string rank = "Chuẩn";
                        if (el.TryGetProperty("membershipRank", out var rProp) || el.TryGetProperty("MembershipRank", out rProp))
                        {
                            rank = rProp.GetString() ?? "Chuẩn";
                        }
                        else
                        {
                            // Chỉ fallback tính theo điểm nếu DB không có giá trị
                            rank = points >= 500 ? "VIP" : (points >= 200 ? "Thành viên" : "Chuẩn");
                        }
                        _originalList.Add(new CustomerViewDto
                        {
                            CustomerId = id,
                            CustomerName = name,
                            PhoneNumber = phone,
                            Email = string.IsNullOrEmpty(email) ? $"{phone}@gmail.com" : email,
                            MembershipRank = rank,
                            RewardPoints = points,
                            TotalSpent = spent,
                            CreatedDate = createdDate,
                            Status = "Hoạt động"
                        });
                    }

                    UpdateKpiCards();
                    FilterData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chi tiết ngoại lệ khi tải danh sách khách hàng:\n" + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterData()
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            string selectedType = cboCustomerType.SelectedItem?.ToString() ?? "Tất cả";
            string selectedStat = cboStatus.SelectedItem?.ToString() ?? "Tất cả";

            var filtered = _originalList.Where(c =>
            {
                // 1. Khớp từ khóa tìm kiếm
                bool matchKw = string.IsNullOrEmpty(keyword)
                    || c.CustomerName.ToLower().Contains(keyword)
                    || c.PhoneNumber.Contains(keyword)
                    || c.CustomerId.ToString().Contains(keyword);

                // 2. Khớp Loại khách hàng (VIP / Thành viên / Tất cả)
                bool matchType = (selectedType == "Tất cả" || selectedType == "All")
                    || c.MembershipRank.Trim().Equals(selectedType.Trim(), StringComparison.OrdinalIgnoreCase);

                // 3. Khớp Trạng thái
                bool matchStat = (selectedStat == "Tất cả" || selectedStat == "All")
                    || c.Status.Trim().Equals(selectedStat.Trim(), StringComparison.OrdinalIgnoreCase);

                return matchKw && matchType && matchStat;
            }).ToList();

            dgvCustomers.Rows.Clear();
            int stt = 1;
            foreach (var item in filtered)
            {
                int rIdx = dgvCustomers.Rows.Add(
                    stt++,
                    item.CustomerId,
                    item.CustomerName,
                    item.PhoneNumber,
                    item.Email,
                    item.MembershipRank,
                    item.RewardPoints,
                    item.TotalSpent,
                    item.CreatedDate,
                    item.Status
                );

                // Tô nền màu tím nhạt nếu là khách hàng VIP
                if (item.MembershipRank.Trim().Equals("VIP", StringComparison.OrdinalIgnoreCase))
                {
                    dgvCustomers.Rows[rIdx].DefaultCellStyle.BackColor = Color.FromArgb(235, 225, 245);
                }
                else if (item.Status == "Khóa")
                {
                    dgvCustomers.Rows[rIdx].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }

            ShowCustomerDetailFromSelectedRow();
        }
        private void ShowCustomerDetailFromSelectedRow()
        {
            if (dgvCustomers.CurrentRow == null || dgvCustomers.CurrentRow.Index < 0) return;

            string phone = dgvCustomers.CurrentRow.Cells["PhoneNumber"].Value?.ToString() ?? "";
            var cust = _originalList.FirstOrDefault(c => c.PhoneNumber == phone);

            if (cust != null)
            {
                lblCustInfoId.Text = $"ID: {cust.CustomerId}";
                lblCustInfoName.Text = $"Name: {cust.CustomerName}";
                lblCustInfoLevel.Text = $"Level: {cust.MembershipRank}";
                lblCustInfoSpent.Text = $"Total spent: {cust.TotalSpent:N0} VND";

                dgvHistory.Rows.Clear();
                dgvHistory.Rows.Add("1", cust.CreatedDate, $"{cust.TotalSpent:N0} VND");
            }
        }

        private void UpdateKpiCards()
        {
            lblKpiTotalCustomers.Text = _originalList.Count.ToString("N0");
            lblKpiNewMembers.Text = _originalList.Count(c => c.MembershipRank != "VIP").ToString("N0");
            long sumPoints = _originalList.Sum(c => (long)c.RewardPoints);
            lblKpiTotalPoints.Text = sumPoints >= 1000000 ? $"{(sumPoints / 1000000.0):0.1}M" : sumPoints.ToString("N0");
        }

        private void FormCustomerManagement_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F6) { AddNewCustomerDialog(); e.Handled = true; }
        }

        private async void AddNewCustomerDialog()
        {
            using Form dlg = new Form
            {
                Width = 380,
                Height = 310,
                Text = "Thêm khách hàng thành viên mới (F6)",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblN = new Label { Left = 20, Top = 15, Text = "Họ và tên khách hàng:", AutoSize = true };
            TextBox txtN = new TextBox { Left = 20, Top = 35, Width = 320 };

            Label lblP = new Label { Left = 20, Top = 70, Text = "Số điện thoại:", AutoSize = true };
            TextBox txtP = new TextBox { Left = 20, Top = 90, Width = 320 };

            Label lblE = new Label { Left = 20, Top = 125, Text = "Địa chỉ / Ghi chú:", AutoSize = true };
            TextBox txtE = new TextBox { Left = 20, Top = 145, Width = 320 };

            Button btnSave = new Button
            {
                Text = "Lưu Khách Hàng",
                Left = 170,
                Top = 200,
                Width = 170,
                Height = 35,
                BackColor = Color.FromArgb(13, 20, 36),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            dlg.Controls.AddRange(new Control[] { lblN, txtN, lblP, txtP, lblE, txtE, btnSave });

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string name = txtN.Text.Trim();
                string phone = txtP.Text.Trim();
                string address = txtE.Text.Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phone))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ họ tên và số điện thoại!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var payload = new { CustomerName = name, PhoneNumber = phone, Address = address };
                    var response = await ApiClientService.Client.PostAsJsonAsync("customers", payload);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Thêm thành công khách hàng '{name}' vào cơ sở dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadCustomerDataAsync();
                    }
                    else
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Không thể thêm khách hàng:\n{err}", "Lỗi lưu CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối API: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void EditCustomer()
        {
            // 1. Kiểm tra xem người dùng đã chọn khách hàng trên bảng chưa
            if (dgvCustomers.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một khách hàng cần sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int customerId = Convert.ToInt32(dgvCustomers.CurrentRow.Cells["CustomerId"].Value ?? 0);
            var cust = _originalList.FirstOrDefault(c => c.CustomerId == customerId);
            if (cust == null) return;

            // 2. Tạo biểu mẫu Popup chỉnh sửa thông tin khách hàng
            using Form dlg = new Form
            {
                Width = 420,
                Height = 390,
                Text = $"Chỉnh sửa khách hàng: {cust.CustomerName}",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblId = new Label { Left = 20, Top = 15, Text = $"Mã khách hàng: {cust.CustomerId}", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 123, 255), AutoSize = true };

            Label lblN = new Label { Left = 20, Top = 45, Text = "Họ và tên khách hàng:", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            TextBox txtN = new TextBox { Left = 20, Top = 65, Width = 360, Text = cust.CustomerName, Font = new Font("Segoe UI", 9.5F) };

            Label lblP = new Label { Left = 20, Top = 100, Text = "Số điện thoại:", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            TextBox txtP = new TextBox { Left = 20, Top = 120, Width = 360, Text = cust.PhoneNumber, Font = new Font("Segoe UI", 9.5F) };

            Label lblRank = new Label { Left = 20, Top = 155, Text = "Hạng thành viên:", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            ComboBox cboRank = new ComboBox { Left = 20, Top = 175, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cboRank.Items.AddRange(new object[] { "Chuẩn", "Thành viên", "Bạc", "Vàng", "VIP" });

            // Chọn đúng hạng thẻ hiện tại của khách hàng
            int rankIdx = cboRank.FindStringExact(cust.MembershipRank);
            cboRank.SelectedIndex = rankIdx >= 0 ? rankIdx : 0;

            Label lblAddr = new Label { Left = 20, Top = 210, Text = "Địa chỉ / Ghi chú:", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            TextBox txtAddr = new TextBox { Left = 20, Top = 230, Width = 360, Text = "TP.HCM", Font = new Font("Segoe UI", 9.5F) };

            Button btnCancel = new Button { Text = "Hủy", Left = 190, Top = 285, Width = 90, Height = 34, BackColor = Color.FromArgb(240, 242, 245), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Button btnSave = new Button { Text = "💾 Lưu", Left = 290, Top = 285, Width = 90, Height = 34, BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };

            dlg.Controls.AddRange(new Control[] { lblId, lblN, txtN, lblP, txtP, lblRank, cboRank, lblAddr, txtAddr, btnCancel, btnSave });
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;

            // 3. Khi bấm Lưu
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string newName = txtN.Text.Trim();
                string newPhone = txtP.Text.Trim();
                string newRank = cboRank.SelectedItem?.ToString() ?? "Thành viên";
                string newAddr = txtAddr.Text.Trim();

                if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newPhone))
                {
                    MessageBox.Show("Họ tên và số điện thoại không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var payload = new
                    {
                        CustomerName = newName,
                        PhoneNumber = newPhone,
                        Address = newAddr,
                        MembershipRank = newRank
                    };

                    // Gọi API cập nhật vào CSDL SQL Server
                    var response = await ApiClientService.Client.PutAsJsonAsync($"customers/{cust.CustomerId}", payload);

                    if (response.IsSuccessStatusCode)
                    {
                        // Cập nhật trực tiếp vào đối tượng trên RAM
                        cust.CustomerName = newName;
                        cust.PhoneNumber = newPhone;
                        cust.MembershipRank = newRank;

                        MessageBox.Show($"Cập nhật thành công cho khách hàng '{newName}'!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Tải lại và đồng bộ hiển thị lên bảng
                        await LoadCustomerDataAsync();
                    }
                    else
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Lỗi từ máy chủ:\n{err}", "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối khi cập nhật: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddManualPoints()
        {
            if (dgvCustomers.CurrentRow == null) return;
            string name = dgvCustomers.CurrentRow.Cells["CustomerName"].Value?.ToString() ?? "";
            MessageBox.Show($"Cộng điểm tích lũy thủ công cho khách: {name}", "Tích Điểm Thủ Công");
        }

        private void ViewCustomerHistory()
        {
            if (dgvCustomers.CurrentRow == null) return;
            string name = dgvCustomers.CurrentRow.Cells["CustomerName"].Value?.ToString() ?? "";
            MessageBox.Show($"Hiển thị toàn bộ lịch sử các hóa đơn mua hàng của: {name}", "Xem Lịch Sử Giao Dịch");
        }

        private void DeleteCustomer()
        {
            if (dgvCustomers.CurrentRow == null) return;
            string name = dgvCustomers.CurrentRow.Cells["CustomerName"].Value?.ToString() ?? "";
            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa khách hàng: {name}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string phone = dgvCustomers.CurrentRow.Cells["PhoneNumber"].Value?.ToString() ?? "";
                _originalList.RemoveAll(c => c.PhoneNumber == phone);
                UpdateKpiCards();
                FilterData();
            }
        }

        private void RedeemVoucher()
        {
            if (lstVouchers.SelectedItem != null)
            {
                MessageBox.Show($"Đã xác nhận đổi: {lstVouchers.SelectedItem}", "Đổi Thưởng Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class CustomerViewDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MembershipRank { get; set; } = "Thành viên";
        public int RewardPoints { get; set; }
        public decimal TotalSpent { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Hoạt động";
    }
}