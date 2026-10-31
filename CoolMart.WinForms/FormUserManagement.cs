using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormUserManagement : Form
    {
        private DataGridView dgvUsers = null!;
        private Label lblSelectedUser = null!;
        private ContextMenuStrip cmsUsers = null!;
        private int _selectedUserId = 0;
        private string _selectedUsername = string.Empty;
        private string _selectedRole = string.Empty;

        public FormUserManagement()
        {
            this.KeyPreview = true;
            InitializeComponentProgrammatically();
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F2) OpenAddUserDialog(); };
            this.Load += async (s, e) => await LoadUsersAsync();
        }

        private void InitializeComponentProgrammatically()
        {
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // 1. Breadcrumbs & Tiêu đề trên cùng
            Panel pnlTopHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(15, 8, 15, 0) };
            Label lblBreadcrumbs = new Label { Text = "Trang chủ  >  Phân quyền & Cài đặt", Location = new Point(15, 8), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5F) };
            Label lblMainTitle = new Label { Text = "PHÂN QUYỀN VÀ CÀI ĐẶT ĐƠN GIẢN", Location = new Point(13, 27), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            pnlTopHeader.Controls.Add(lblBreadcrumbs);
            pnlTopHeader.Controls.Add(lblMainTitle);

            // 2. Vùng làm việc chính: GroupBox quản lý tài khoản
            Panel pnlMainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 15, 15, 5) };
            GroupBox grpUsers = new GroupBox { Dock = DockStyle.Fill, Text = "QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48), Padding = new Padding(12) };

            // Nút Thêm tài khoản F2 ở góc trên GroupBox
            Panel pnlAction = new Panel { Dock = DockStyle.Top, Height = 40 };
            Button btnAddUserF2 = new Button { Text = "THÊM TÀI KHOẢN (F2) 👤+", Size = new Size(190, 32), Location = new Point(grpUsers.Width - 210, 2), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.FromArgb(24, 30, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddUserF2.Click += (s, e) => OpenAddUserDialog();
            pnlAction.Controls.Add(btnAddUserF2);

            // Context Menu chuột phải
            SetupContextMenu();

            // DataGridView với cột Dropdown ComboBox chọn Role trực tiếp trên bảng
            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ContextMenuStrip = cmsUsers,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            SetupColumns();
            dgvUsers.CellClick += DgvUsers_CellClick;

            grpUsers.Controls.Add(dgvUsers);
            grpUsers.Controls.Add(pnlAction);
            pnlMainContainer.Controls.Add(grpUsers);

            // 3. Panel dưới: Thông tin người dùng đang chọn & Các nút cài đặt khác
            Panel pnlBottomSection = new Panel { Dock = DockStyle.Bottom, Height = 175, BackColor = Color.FromArgb(244, 246, 249), Padding = new Padding(15, 5, 15, 10) };

            // Thẻ hiển thị User details: nguyenana | Admin
            Panel pnlSelectedCard = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(15, 8, 15, 8) };
            Label lblCardHeader = new Label { Text = "THÔNG TIN TÀI KHOẢN ĐANG CHỌN", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.DimGray };
            lblSelectedUser = new Label { Text = "User details: Chưa chọn tài khoản", Dock = DockStyle.Bottom, AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 30, 48) };
            pnlSelectedCard.Controls.Add(lblCardHeader);
            pnlSelectedCard.Controls.Add(lblSelectedUser);

            // Khu vực "CÁC CÀI ĐẶT KHÁCH" & Nút "LƯU THAY ĐỔI"
            Panel pnlOtherSettings = new Panel { Dock = DockStyle.Bottom, Height = 95, BackColor = Color.White, Padding = new Padding(15, 8, 15, 10), Margin = new Padding(0, 5, 0, 0) };
            Label lblOtherTitle = new Label { Text = "CÁC CÀI ĐẶT KHÁC", Location = new Point(15, 8), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DimGray };

            Button btnShopProfile = CreateSettingButton("🏪  Hồ sơ cửa hàng", 15, 30);
            Button btnPosConfig = CreateSettingButton("💻  Cấu hình POS", 145, 30);
            Button btnHistory = CreateSettingButton("🕒  Lịch sử", 275, 30);

            Button btnSaveBottom = new Button { Text = "LƯU THAY ĐỔI", Size = new Size(160, 42), Anchor = AnchorStyles.Right | AnchorStyles.Top, BackColor = Color.FromArgb(24, 30, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSaveBottom.Location = new Point(pnlOtherSettings.Width - 180, 28);
            btnSaveBottom.Click += async (s, e) => await SaveChangesFromGridAsync();

            pnlOtherSettings.Controls.Add(lblOtherTitle);
            pnlOtherSettings.Controls.Add(btnShopProfile);
            pnlOtherSettings.Controls.Add(btnPosConfig);
            pnlOtherSettings.Controls.Add(btnHistory);
            pnlOtherSettings.Controls.Add(btnSaveBottom);

            pnlBottomSection.Controls.Add(pnlOtherSettings);
            pnlBottomSection.Controls.Add(pnlSelectedCard);

            this.Controls.Add(pnlMainContainer);
            this.Controls.Add(pnlBottomSection);
            this.Controls.Add(pnlTopHeader);
        }

        private Button CreateSettingButton(string text, int x, int y)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(240, 242, 245),
                ForeColor = Color.FromArgb(33, 37, 41),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            return btn;
        }

        private void SetupColumns()
        {
            dgvUsers.Columns.Clear();

            // 1. UserId
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", HeaderText = "UserId", ReadOnly = true, FillWeight = 15 });
            // 2. Username
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Username", ReadOnly = true, FillWeight = 25 });
            // 3. Password (dấu hoa thị che mật khẩu)
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Password", HeaderText = "Password", ReadOnly = true, FillWeight = 20 });
            // 4. Full Name
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Full Name", FillWeight = 30 });

            // 5. Role (Dropdown trực tiếp: Admin, Thu ngân / Cashier, Kiểm kho / Warehouse)
            DataGridViewComboBoxColumn colRole = new DataGridViewComboBoxColumn
            {
                Name = "Role",
                HeaderText = "Role",
                FlatStyle = FlatStyle.Flat,
                FillWeight = 25
            };
            colRole.Items.AddRange("Admin", "Cashier", "Warehouse");
            dgvUsers.Columns.Add(colRole);

            // 6. IsActive (Checkbox)
            DataGridViewCheckBoxColumn colActive = new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                HeaderText = "IsActive",
                FillWeight = 15
            };
            dgvUsers.Columns.Add(colActive);
        }

        private void SetupContextMenu()
        {
            cmsUsers = new ContextMenuStrip();
            ToolStripMenuItem mnuDetail = new ToolStripMenuItem("Xem chi tiết", null, (s, e) => ShowUserDetailBox());
            ToolStripMenuItem mnuEdit = new ToolStripMenuItem("Sửa người dùng", null, (s, e) => ShowUserDetailBox());
            ToolStripMenuItem mnuReset = new ToolStripMenuItem("Reset mật khẩu", null, async (s, e) => await ResetPasswordAsync());
            ToolStripMenuItem mnuDeactivate = new ToolStripMenuItem("Vô hiệu hóa / Kích hoạt", null, async (s, e) => await ToggleActiveAsync());

            cmsUsers.Items.AddRange(new ToolStripItem[] { mnuDetail, mnuEdit, mnuReset, mnuDeactivate });
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var list = await ApiClientService.Client.GetFromJsonAsync<List<UserItemDto>>("users");
                dgvUsers.Rows.Clear();
                if (list != null)
                {
                    foreach (var u in list)
                    {
                        dgvUsers.Rows.Add(u.Id, u.Username, "••••••", u.FullName, u.Role, u.IsActive);
                    }
                    if (dgvUsers.Rows.Count > 0) SelectRow(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy danh sách tài khoản: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvUsers_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) SelectRow(e.RowIndex);
        }

        private void SelectRow(int index)
        {
            if (index < 0 || index >= dgvUsers.Rows.Count) return;
            var row = dgvUsers.Rows[index];

            _selectedUserId = Convert.ToInt32(row.Cells["UserId"].Value);
            _selectedUsername = row.Cells["Username"].Value?.ToString() ?? "";
            _selectedRole = row.Cells["Role"].Value?.ToString() ?? "";

            lblSelectedUser.Text = $"User details: {_selectedUsername} | Role: [{_selectedRole}]";
        }

        private async Task SaveChangesFromGridAsync()
        {
            if (_selectedUserId == 0 || dgvUsers.CurrentRow == null) return;
            var row = dgvUsers.CurrentRow;

            string fullName = row.Cells["FullName"].Value?.ToString() ?? "";
            string role = row.Cells["Role"].Value?.ToString() ?? "Cashier";
            bool isActive = Convert.ToBoolean(row.Cells["IsActive"].Value);

            var updateDto = new
            {
                FullName = fullName,
                Role = role,
                IsActive = isActive
            };

            var res = await ApiClientService.Client.PutAsJsonAsync($"users/{_selectedUserId}", updateDto);
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show($"Đã lưu thay đổi cho tài khoản '{_selectedUsername}'!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadUsersAsync();
            }
            else
            {
                MessageBox.Show("Lưu thay đổi thất bại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenAddUserDialog()
        {
            using Form dlg = new Form { Text = "Thêm người dùng mới", Size = new Size(360, 320), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label l1 = new Label { Text = "Tên đăng nhập (*):", Location = new Point(20, 15), AutoSize = true };
            TextBox tUser = new TextBox { Location = new Point(20, 35), Width = 300 };

            Label l2 = new Label { Text = "Mật khẩu ban đầu (*):", Location = new Point(20, 70), AutoSize = true };
            TextBox tPass = new TextBox { Location = new Point(20, 90), Width = 300, UseSystemPasswordChar = true };

            Label l3 = new Label { Text = "Họ và tên nhân viên (*):", Location = new Point(20, 125), AutoSize = true };
            TextBox tName = new TextBox { Location = new Point(20, 145), Width = 300 };

            Label l4 = new Label { Text = "Vai trò (Role):", Location = new Point(20, 180), AutoSize = true };
            ComboBox cRole = new ComboBox { Location = new Point(20, 200), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cRole.Items.AddRange(new object[] { "Admin", "Cashier", "Warehouse" });
            cRole.SelectedIndex = 1;

            Button btnSubmit = new Button { Text = "Tạo tài khoản", DialogResult = DialogResult.OK, Location = new Point(190, 235), Size = new Size(130, 35), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            dlg.Controls.AddRange(new Control[] { l1, tUser, l2, tPass, l3, tName, l4, cRole, btnSubmit });

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(tUser.Text) || string.IsNullOrWhiteSpace(tPass.Text))
                {
                    MessageBox.Show("Không được để trống tài khoản và mật khẩu!", "Cảnh báo");
                    return;
                }

                _ = Task.Run(async () =>
                {
                    var req = new { Username = tUser.Text.Trim(), Password = tPass.Text.Trim(), FullName = tName.Text.Trim(), Role = cRole.SelectedItem?.ToString(), IsActive = true };
                    var res = await ApiClientService.Client.PostAsJsonAsync("users", req);
                    this.Invoke(() =>
                    {
                        if (res.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Tạo tài khoản thành công!", "Thông báo");
                            _ = LoadUsersAsync();
                        }
                    });
                });
            }
        }

        private void ShowUserDetailBox()
        {
            if (_selectedUserId == 0) return;
            MessageBox.Show($"ID: {_selectedUserId}\nUsername: {_selectedUsername}\nVai trò hiện tại: {_selectedRole}\n\nBạn có thể chỉnh sửa trực tiếp cột 'Full Name' hoặc chọn lại 'Role' ngay trên bảng rồi nhấn nút 'LƯU THAY ĐỔI' ở góc dưới.", "Chi tiết người dùng");
        }

        private async Task ResetPasswordAsync()
        {
            if (_selectedUserId == 0) return;
            if (MessageBox.Show($"Bạn có muốn đặt lại mật khẩu của '{_selectedUsername}' về '123'?", "Reset mật khẩu", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var req = new { FullName = dgvUsers.CurrentRow?.Cells["FullName"].Value?.ToString(), Password = "123", Role = _selectedRole, IsActive = true };
                var res = await ApiClientService.Client.PutAsJsonAsync($"users/{_selectedUserId}", req);
                if (res.IsSuccessStatusCode) MessageBox.Show("Đã reset mật khẩu thành công về: 123", "Thông báo");
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (_selectedUserId == 0 || dgvUsers.CurrentRow == null) return;
            bool current = Convert.ToBoolean(dgvUsers.CurrentRow.Cells["IsActive"].Value);
            dgvUsers.CurrentRow.Cells["IsActive"].Value = !current;
            await SaveChangesFromGridAsync();
        }

    }
    public class UserItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
} 
