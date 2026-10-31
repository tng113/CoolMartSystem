using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoolMart.API.Data;
using CoolMart.API.Models;

namespace CoolMart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Cashier")]
    public class CustomersController : ControllerBase
    {
        private readonly CoolMartDbContext _context;

        public CustomersController(CoolMartDbContext context)
        {
            _context = context;
        }

        // GET: api/customers
        [HttpGet]
        public async Task<IActionResult> GetAllCustomers()
        {
            var list = await _context.Customers
                .AsNoTracking()
                .Select(c => new
                {
                    customerId = c.CustomerId,
                    customerName = c.CustomerName,
                    phoneNumber = c.PhoneNumber,
                    address = c.Address ?? "",
                    rewardPoints = c.RewardPoints,
                    membershipRank = c.MembershipRank,
                    totalSpent = _context.Orders
                        .Where(o => o.CustomerId == c.CustomerId)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0,
                    createdDate = "19/07/2026",
                    status = "Hoạt động"
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/customers/by-phone/0901234567
        [HttpGet("by-phone/{phone}")]
        public async Task<IActionResult> GetByPhone(string phone)
        {
            var cleanPhone = phone.Replace(" ", "").Trim();
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone);

            if (customer == null)
                return NotFound(new { message = "Không tìm thấy khách hàng!" });

            return Ok(customer);
        }

        // POST: api/customers
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest(new { message = "Họ tên và số điện thoại không được để trống!" });

            string cleanPhone = dto.PhoneNumber.Replace(" ", "").Trim();

            bool exists = await _context.Customers.AnyAsync(c => c.PhoneNumber == cleanPhone);
            if (exists)
                return Conflict(new { message = "Số điện thoại này đã được đăng ký!" });

            var customer = new Customer
            {
                CustomerName = dto.CustomerName.Trim(),
                PhoneNumber = cleanPhone,
                Address = string.IsNullOrWhiteSpace(dto.Address) ? "TP.HCM" : dto.Address.Trim(),
                RewardPoints = 0,
                MembershipRank = "Chuẩn"
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, customerId = customer.CustomerId, message = "Thêm thành công!" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = "Không tìm thấy khách hàng!" });

            if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest(new { message = "Họ tên và số điện thoại không được để trống!" });

            string cleanPhone = dto.PhoneNumber.Replace(" ", "").Trim();

            // Kiểm tra nếu đổi SĐT thì không được trùng với người khác
            bool exists = await _context.Customers.AnyAsync(c => c.PhoneNumber == cleanPhone && c.CustomerId != id);
            if (exists)
                return Conflict(new { message = "Số điện thoại này đã được đăng ký bởi khách hàng khác!" });

            customer.CustomerName = dto.CustomerName.Trim();
            customer.PhoneNumber = cleanPhone;
            customer.Address = dto.Address ?? "";
            customer.MembershipRank = dto.MembershipRank ?? customer.MembershipRank;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật thông tin khách hàng thành công!" });
        }

        public class UpdateCustomerDto
        {
            public string CustomerName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string? Address { get; set; }
            public string? MembershipRank { get; set; }
        }
    }

    public class CreateCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}