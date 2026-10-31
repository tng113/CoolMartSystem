using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoolMart.API.Data;

namespace CoolMart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ Admin được xem lịch sử hóa đơn
    public class OrdersController : ControllerBase
    {
        private readonly CoolMartDbContext _context;

        public OrdersController(CoolMartDbContext context)
        {
            _context = context;
        }

        // GET: api/orders?fromDate=2026-10-01&toDate=2026-10-30&keyword=HD-2026
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? keyword)
        {
            var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
            var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedDate >= start && o.CreatedDate <= end);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                query = query.Where(o => o.OrderCode.Contains(kw) || o.CashierUsername.Contains(kw));
            }

            var list = await (
                from o in query
                join c in _context.Customers.AsNoTracking() on o.CustomerId equals c.CustomerId into cj
                from c in cj.DefaultIfEmpty()
                orderby o.CreatedDate descending
                select new
                {
                    orderId = o.OrderId,
                    orderCode = o.OrderCode,
                    createdDate = o.CreatedDate,
                    cashierUsername = o.CashierUsername,
                    customerName = c != null ? c.CustomerName : "Khách vãng lai",
                    totalItems = o.TotalItems,
                    finalAmount = o.FinalAmount,
                    rewardPoints = o.RewardPoints
                })
                .Take(200)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/orders/7/details  (các dòng hàng của 1 hóa đơn)
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound(new { message = "Không tìm thấy hóa đơn!" });

            var items = await _context.OrderDetails
                .AsNoTracking()
                .Where(d => d.OrderId == id)
                .Select(d => new
                {
                    barcode = d.Product != null ? d.Product.Barcode : "",
                    productName = d.Product != null ? d.Product.ProductName : "",
                    quantity = d.Quantity,
                    unitPrice = d.UnitPrice,
                    lineTotal = d.LineTotal
                })
                .ToListAsync();

            return Ok(new { orderCode = order.OrderCode, finalAmount = order.FinalAmount, items });
        }
    }
}
