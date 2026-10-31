using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoolMart.API.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CoolMart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly CoolMartDbContext _context;

        public ReportsController(CoolMartDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard-analytics")]
        public async Task<IActionResult> GetDashboardAnalytics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var start = (fromDate ?? DateTime.Today).Date;
            var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            // 1. Lọc đơn hàng theo khoảng ngày
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedDate >= start && o.CreatedDate <= end);

            decimal totalRevenue = await ordersQuery.SumAsync(o => (decimal?)o.FinalAmount) ?? 0;
            int totalOrders = await ordersQuery.CountAsync();
            decimal averageBill = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 0) : 0;
            decimal totalProfit = Math.Round(totalRevenue * 0.15m, 0); // Lợi nhuận ước tính 15%

            // 2. Doanh thu theo khung giờ (08:00 - 22:00)
            var ordersWithTime = await ordersQuery
                .Select(o => new { Hour = o.CreatedDate.Hour, o.FinalAmount })
                .ToListAsync();

            var hourlyRevenue = new List<object>();
            for (int h = 8; h <= 22; h += 2)
            {
                decimal amount = ordersWithTime
                    .Where(o => o.Hour >= h && o.Hour < h + 2)
                    .Sum(o => o.FinalAmount);

                hourlyRevenue.Add(new
                {
                    timeSlot = $"{h:D2}:00",
                    revenue = amount
                });
            }

            // 3. Top 5 sản phẩm bán chạy nhất
            var topProducts = await _context.OrderDetails
                .AsNoTracking()
                .Where(od => od.Order.CreatedDate >= start && od.Order.CreatedDate <= end)
                .GroupBy(od => new { od.ProductId, od.Product.Barcode, od.Product.ProductName })
                .Select(g => new
                {
                    productId = g.Key.ProductId,
                    barcode = g.Key.Barcode,
                    productName = g.Key.ProductName,
                    unit = g.Key.ProductName.Contains("Chai") ? "Chai" : (g.Key.ProductName.Contains("Lon") ? "Lon" : (g.Key.ProductName.Contains("Hộp") ? "Hộp" : "Cây")),
                    quantity = g.Sum(x => x.Quantity),
                    totalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.quantity)
                .Take(5)
                .ToListAsync();

            // 4. Tỉ trọng doanh thu theo ngành hàng (Pie Chart)
            var categoryShares = await _context.OrderDetails
                .AsNoTracking()
                .Where(od => od.Order.CreatedDate >= start && od.Order.CreatedDate <= end)
                .GroupBy(od => od.Product.Category != null ? od.Product.Category.CategoryName : "Khác")
                .Select(g => new
                {
                    categoryName = g.Key,
                    revenue = g.Sum(x => x.LineTotal)
                })
                .ToListAsync();

            return Ok(new
            {
                kpi = new
                {
                    totalRevenue,
                    totalProfit,
                    totalOrders,
                    averageBill
                },
                hourly = hourlyRevenue,
                topProducts,
                categoryShares
            });
        }

        // GET: api/reports/dashboard-stats  (4 thẻ KPI trên màn hình POS - dữ liệu thật)
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todayOrders = _context.Orders.AsNoTracking()
                .Where(o => o.CreatedDate >= today && o.CreatedDate < tomorrow);

            var todayRevenue = await todayOrders.SumAsync(o => (decimal?)o.FinalAmount) ?? 0;
            var todayOrderCount = await todayOrders.CountAsync();
            var todaySoldItems = await _context.OrderDetails.AsNoTracking()
                .Where(od => od.Order.CreatedDate >= today && od.Order.CreatedDate < tomorrow)
                .SumAsync(od => (int?)od.Quantity) ?? 0;
            var lowStockCount = await _context.Products.AsNoTracking()
                .CountAsync(p => p.StockQuantity <= 10);

            return Ok(new { todayRevenue, todayOrderCount, todaySoldItems, lowStockCount });
        }

        // GET: api/reports/revenue-by-day?fromDate=2026-10-01&toDate=2026-10-30
        // Gọi Stored Procedure sp_RevenueByDay (GROUP BY CAST(CreatedDate AS DATE))
        [Authorize(Roles = "Admin")]
        [HttpGet("revenue-by-day")]
        public async Task<IActionResult> GetRevenueByDay([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var from = (fromDate ?? DateTime.Today.AddDays(-6)).Date;
            var to = (toDate ?? DateTime.Today).Date;
            var result = new List<object>();

            using var conn = new SqlConnection(_context.Database.GetConnectionString());
            using var cmd = new SqlCommand("sp_RevenueByDay", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FromDate", from);
            cmd.Parameters.AddWithValue("@ToDate", to);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    date = ((DateTime)reader["OrderDate"]).ToString("yyyy-MM-dd"),
                    totalOrders = (int)reader["TotalOrders"],
                    revenue = (decimal)reader["Revenue"]
                });
            }
            return Ok(result);
        }

        // GET: api/reports/revenue-by-shift?fromDate=2026-10-01&toDate=2026-10-30
        // Gọi Stored Procedure sp_RevenueByShift: doanh thu theo ca (thu ngân) từng ngày
        [Authorize(Roles = "Admin")]
        [HttpGet("revenue-by-shift")]
        public async Task<IActionResult> GetRevenueByShift([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var from = (fromDate ?? DateTime.Today).Date;
            var to = (toDate ?? DateTime.Today).Date;
            var result = new List<object>();

            using var conn = new SqlConnection(_context.Database.GetConnectionString());
            using var cmd = new SqlCommand("sp_RevenueByShift", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FromDate", from);
            cmd.Parameters.AddWithValue("@ToDate", to);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    cashier = reader["CashierUsername"].ToString(),
                    date = ((DateTime)reader["OrderDate"]).ToString("yyyy-MM-dd"),
                    totalOrders = (int)reader["TotalOrders"],
                    revenue = (decimal)reader["Revenue"]
                });
            }
            return Ok(result);
        }
    }
}