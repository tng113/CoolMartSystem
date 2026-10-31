using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoolMart.API.Data;
using CoolMart.API.Models;

namespace CoolMart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Tiêu chí 30: Chỉ Admin mới có quyền quản lý tài khoản và cấp Role
    public class UsersController : ControllerBase
    {
        private readonly CoolMartDbContext _context;

        public UsersController(CoolMartDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Role,
                    u.IsActive
                })
                .ToListAsync();
            return Ok(users);
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
                return BadRequest(new { message = "Tài khoản và mật khẩu không được để trống!" });

            // Kiểm tra trùng username
            if (await _context.Users.AnyAsync(u => u.Username == user.Username.Trim()))
                return BadRequest(new { message = "Tên đăng nhập này đã tồn tại!" });

            user.Username = user.Username.Trim();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo tài khoản thành công!", id = user.Id });
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] User updateUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            user.FullName = updateUser.FullName;
            user.Role = updateUser.Role;
            user.IsActive = updateUser.IsActive;

            // Nếu người dùng có nhập mật khẩu mới thì mới đổi
            if (!string.IsNullOrWhiteSpace(updateUser.Password))
            {
                user.Password = updateUser.Password;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            // Không cho phép tự xóa tài khoản admin gốc
            if (user.Username.ToLower() == "admin01")
                return BadRequest(new { message = "Không được phép xóa tài khoản quản trị viên mặc định!" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}