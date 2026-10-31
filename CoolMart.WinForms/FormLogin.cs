using System;
using System.Drawing;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Forms;

namespace CoolMart.WinForms
{
    public partial class FormLogin : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;

        public FormLogin()
        {
            InitializeComponentProgrammatically();
        }

        private void InitializeComponentProgrammatically()
        {
            // Thiết lập cửa sổ
            this.Text = "Hệ thống Bán lẻ Cool Mart - Đăng nhập";
            this.Size = new Size(420, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(24, 30, 54)
            };

            Label lblLogo = new Label
            {
                Text = "🍦 COOL MART 🥤",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 198, 255),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblSubtitle = new Label
            {
                Text = "HỆ THỐNG QUẢN LÝ TIỆN LỢI THÔNG MINH",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.WhiteSmoke,
                Dock = DockStyle.Bottom,
                Height = 35,
                TextAlign = ContentAlignment.TopCenter
            };

            pnlHeader.Controls.Add(lblLogo);
            pnlHeader.Controls.Add(lblSubtitle);

            // Vùng nhập liệu
            Label lblUser = new Label
            {
                Text = "Tên đăng nhập:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(45, 140),
                AutoSize = true
            };

            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Location = new Point(45, 168),
                Size = new Size(315, 30),
                Text = "admin01" // Tài khoản mẫu
            };

            Label lblPass = new Label
            {
                Text = "Mật khẩu:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(45, 215),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Location = new Point(45, 243),
                Size = new Size(315, 30),
                UseSystemPasswordChar = true,
                Text = "123" // Mật khẩu mẫu
            };

            // Nút Đăng nhập
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP HỆ THỐNG",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 122, 255),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(45, 310),
                Size = new Size(315, 45),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Gắn phím Enter để kích hoạt nút đăng nhập
            this.AcceptButton = btnLogin;

            // Thêm các điều khiển vào Form
            this.Controls.Add(pnlHeader);
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Đang xác thực...";

            try
            {
                var loginData = new { Username = username, Password = password };
                var response = await ApiClientService.Client.PostAsJsonAsync("auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    var root = doc.RootElement;

                    SessionManager.JwtToken = root.GetProperty("token").GetString() ?? string.Empty;
                    SessionManager.CurrentUsername = root.GetProperty("username").GetString() ?? string.Empty;
                    SessionManager.CurrentFullName = root.GetProperty("fullName").GetString() ?? string.Empty;
                    SessionManager.CurrentRole = root.GetProperty("role").GetString() ?? string.Empty;

                    // Đính kèm Token bảo mật JWT Bearer
                    ApiClientService.AttachToken(SessionManager.JwtToken);

                    FormMainShell mainShell = new FormMainShell();
                    this.Hide();
                    mainShell.ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Tài khoản hoặc mật khẩu không chính xác!", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối máy chủ API: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "ĐĂNG NHẬP HỆ THỐNG";
            }
        }
    }
}