using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormMainShell : Form
    {
        private Panel panelSidebar = null!;
        private Panel panelTopHeader = null!;
        private Panel panelMainContent = null!;
        private Label lblTitle = null!;
        private Label lblUserInfo = null!;
        private FormPOS? _posInstance = null;

        // Các nút bấm phân quyền
        private Button btnPOS = null!;
        private Button btnWarehouse = null!;
        private Button btnCategory = null!;
        private Button btnProduct = null!;
        private Button btnCustomer = null!;
        private Button btnReports = null!;
        private Button btnOrders = null!;
        private Button btnUserManage = null!;
        private Button btnLogout = null!;
        private Form? _activeForm = null;

        public FormMainShell()
        {
            InitializeComponentProgrammatically();
            ApplyRolePermissions(SessionManager.CurrentRole);
        }

        private void InitializeComponentProgrammatically()
        {
            // Thiết lập cửa sổ chính chuẩn HD
            this.Text = "Hệ thống Quản trị & Bán lẻ Cool Mart";
            this.Size = new Size(1300, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1024, 600);

            // 1. Sidebar Panel (Trái, rộng 230px)
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = Color.FromArgb(24, 30, 48)
            };

            // Tiêu đề Logo Sidebar
            Label lblLogo = new Label
            {
                Text = "🍦 COOL MART 🥤",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 65,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelSidebar.Controls.Add(lblLogo);

            // Nút Đăng xuất ở dưới cùng (Dock = Bottom)
            btnLogout = CreateMenuButton("🚪  Đăng xuất", 55);
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.BackColor = Color.FromArgb(180, 40, 40);
            btnLogout.Click += (s, e) =>
            {
                if (MessageBox.Show("Bạn có muốn đăng xuất phiên làm việc hiện tại?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    SessionManager.JwtToken = string.Empty;
                    SessionManager.CurrentUsername = string.Empty;
                    SessionManager.CurrentFullName = string.Empty;
                    SessionManager.CurrentRole = string.Empty;
                    ApiClientService.AttachToken(string.Empty);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            panelSidebar.Controls.Add(btnLogout);

            // Tạo các nút menu nghiệp vụ (Chiều cao chuẩn 48px)
            btnPOS = CreateMenuButton("🛒  Thu ngân (POS)", 48);
            btnWarehouse = CreateMenuButton("📦  Quản lý Kho & Nhập hàng", 48);
            btnCategory = CreateMenuButton("📁  Danh mục Hàng hóa", 48);
            btnProduct = CreateMenuButton("🏷️  Quản lý Sản phẩm", 48);
            btnCustomer = CreateMenuButton("👥  Quản lý Khách hàng", 48);
            btnReports = CreateMenuButton("📊  Báo cáo Thống kê", 48);
            btnOrders = CreateMenuButton("🧾  Lịch sử Hóa đơn", 48);
            btnUserManage = CreateMenuButton("⚙️  Phân quyền & Cài đặt", 48);

            // Vì Dock = Top sẽ xếp chồng ngược, ta Add từ dưới lên để giao diện hiển thị đúng thứ tự từ trên xuống dưới
            panelSidebar.Controls.Add(btnUserManage);
            panelSidebar.Controls.Add(btnReports);
            panelSidebar.Controls.Add(btnOrders);
            panelSidebar.Controls.Add(btnCustomer);
            panelSidebar.Controls.Add(btnProduct);
            panelSidebar.Controls.Add(btnCategory);
            panelSidebar.Controls.Add(btnWarehouse);
            panelSidebar.Controls.Add(btnPOS);

            // Gắn sự kiện Click mở Form con tương ứng
            btnPOS.Click += (s, e) =>
            {
                if (_posInstance == null || _posInstance.IsDisposed)
                {
                    _posInstance = new FormPOS();
                }
                ShowChildForm(_posInstance, "HỆ THỐNG QUẦY THU NGÂN BÁN LẺ (POS)", btnPOS);
            };
            btnWarehouse.Click += (s, e) => ShowChildForm(new FormInventoryManagement(), " KHO & NHẬP HÀNG (Inventory & Warehouse)", btnWarehouse);
            btnCategory.Click += (s, e) => ShowChildForm(new FormCategoryManagement(), "DANH MỤC SẢN PHẨM & GIÁ", btnCategory);
            btnProduct.Click += (s, e) => ShowChildForm(new FormProductManagement(), " SẢN PHẨM & TỒN KHO", btnProduct);
            btnCustomer.Click += (s, e) => ShowChildForm(new FormCustomerManagement(), "KHÁCH HÀNG & TÍCH ĐIỂM", btnCustomer);
            btnReports.Click += (s, e) => ShowChildForm(new FormReports(), "DASHBOARD CHỈ SỐ DOANH THU & TOP BÁN CHẠY", btnReports);
            btnOrders.Click += (s, e) => ShowChildForm(new FormOrderHistory(), "LỊCH SỬ HÓA ĐƠN ĐÃ LẬP", btnOrders);
            btnUserManage.Click += (s, e) => ShowChildForm(new FormUserManagement(), "QUẢN TRỊ TÀI KHOẢN VÀ PHÂN QUYỀN (RBAC)", btnUserManage);
            // 2. Header Panel (Trên đỉnh)
            panelTopHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White
            };

            lblTitle = new Label
            {
                Text = "BÀN LÀM VIỆC TRUNG TÂM",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 30, 48),
                Location = new Point(20, 20),
                AutoSize = true
            };

            lblUserInfo = new Label
            {
                Text = $"👤 {SessionManager.CurrentFullName} | Vai trò: [{SessionManager.CurrentRole}]",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Dock = DockStyle.Right,
                Width = 400,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };

            panelTopHeader.Controls.Add(lblTitle);
            panelTopHeader.Controls.Add(lblUserInfo);

            // 3. Main Workspace Panel (Vùng làm việc trung tâm)
            panelMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 246, 249)
            };

            // Thêm các panel lớn vào Form
            this.Controls.Add(panelMainContent);
            this.Controls.Add(panelTopHeader);
            this.Controls.Add(panelSidebar);
        }

        private Button CreateMenuButton(string text, int height)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 30, 48),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseMnemonic = false // <-- Thêm dòng này để hiện nguyên vẹn chữ "Kho & Nhập hàng"
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // Nhúng động Form con vào panel trung tâm mà không bị giật lag
        public void ShowChildForm(Form childForm, string title, Button senderBtn)
        {
            HighlightButton(senderBtn);

            // Nếu form cũ đang hiển thị và khác form mới, chỉ ẩn đi (Hide) thay vì đóng (Close)
            if (_activeForm != null && _activeForm != childForm)
            {
                _activeForm.Hide();
            }

            _activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            if (!panelMainContent.Controls.Contains(childForm))
            {
                panelMainContent.Controls.Add(childForm);
            }

            lblTitle.Text = title;
            childForm.BringToFront();
            childForm.Show();
        }

        private void HighlightButton(Button activeButton)
        {
            foreach (Control ctrl in panelSidebar.Controls)
            {
                if (ctrl is Button btn && btn != btnLogout)
                {
                    btn.BackColor = Color.FromArgb(24, 30, 48);
                }
            }
            activeButton.BackColor = Color.FromArgb(41, 100, 180);
        }

        // Phân quyền hiển thị theo Role (RBAC)
        private void ApplyRolePermissions(string role)
        {
            switch (role.ToUpper())
            {
                case "ADMIN":
                    btnPOS.Visible = true;
                    btnWarehouse.Visible = true;
                    btnCategory.Visible = true;
                    btnProduct.Visible = true;
                    btnCustomer.Visible = true;
                    btnReports.Visible = true;
                    btnOrders.Visible = true;
                    btnUserManage.Visible = true;
                    ShowChildForm(new FormReports(), "DASHBOARD CHỈ SỐ DOANH THU & TOP BÁN CHẠY", btnReports);
                    break;

                case "CASHIER":
                    btnPOS.Visible = true;
                    btnCustomer.Visible = false; // Thu ngân chỉ thấy nút Bán hàng POS
                    btnWarehouse.Visible = false;
                    btnCategory.Visible = false;
                    btnProduct.Visible = false;
                    btnReports.Visible = false;
                    btnOrders.Visible = false;
                    btnUserManage.Visible = false;
                    ShowChildForm(new FormPOS(), "HỆ THỐNG QUẦY THU NGÂN BÁN LẺ (POS)", btnPOS);
                    break;

                case "WAREHOUSE":
                    btnPOS.Visible = false;
                    btnCustomer.Visible = false;
                    btnReports.Visible = false;
                    btnOrders.Visible = false;
                    btnUserManage.Visible = false;
                    btnCategory.Visible = true;
                    btnProduct.Visible = true;
                    btnWarehouse.Visible = true;
                    ShowChildForm(new FormInventoryManagement(), "QUẢN LÝ KHO & NHẬP HÀNG (Inventory & Warehouse)", btnWarehouse);
                    break;
            }
        }
    }
}