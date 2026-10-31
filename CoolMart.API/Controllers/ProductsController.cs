using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoolMart.API.Data;
using CoolMart.API.Models;

namespace CoolMart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly CoolMartDbContext _context;

        public ProductsController(CoolMartDbContext context)
        {
            _context = context;
        }

        // GET: api/products/paged?keyword=kem&pageNumber=1&pageSize=10
        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedProducts(
            [FromQuery] string? keyword,
            [FromQuery] int? categoryId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .AsQueryable();

            // Lọc theo nhóm hàng (TC 24)
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(kw) || p.Barcode.Contains(kw));
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = await query
                .OrderBy(p => p.ProductId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductItemDto
                {
                    ProductId = p.ProductId,
                    Barcode = p.Barcode,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : string.Empty
                })
                .ToListAsync();

            return Ok(new
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            });
        }

        [Authorize(Roles = "Admin,Warehouse")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAllProducts), product); // 201 Created
        }

        [Authorize(Roles = "Admin,Warehouse")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();

            p.Barcode = product.Barcode;
            p.ProductName = product.ProductName;
            p.Price = product.Price;
            p.StockQuantity = product.StockQuantity;
            p.CategoryId = product.CategoryId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Admin,Warehouse")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            _context.Products.Remove(p);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/products/import-stock
        [Authorize(Roles = "Admin,Warehouse")]
        [HttpPost("import-stock")]
        public async Task<IActionResult> ImportStock([FromBody] StockImportRequestDto request)
        {
            if (request == null || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Số lượng nhập phải lớn hơn 0!" });
            }

            // Tìm theo Barcode hoặc ProductId
            var product = await _context.Products.FirstOrDefaultAsync(p =>
                p.Barcode == request.Barcode || p.ProductName.Contains(request.Barcode));

            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy sản phẩm có mã/tên '{request.Barcode}'!" });
            }

            product.StockQuantity += request.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Nhập kho thành công!",
                productId = product.ProductId,
                productName = product.ProductName,
                newStockQuantity = product.StockQuantity
            });
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var list = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Select(p => new
                {
                    productId = p.ProductId,
                    barcode = p.Barcode,
                    productName = p.ProductName,
                    price = p.Price,
                    stockQuantity = p.StockQuantity,
                    categoryId = p.CategoryId,
                    categoryName = p.Category != null ? p.Category.CategoryName : "Khác"
                })
                .ToListAsync();

            return Ok(list);
        }

        public class UpdatePriceDto
        {
            [Range(0.01, 1000000000, ErrorMessage = "Giá bán phải lớn hơn 0!")]
            public decimal Price { get; set; }
        }

        [Authorize(Roles = "Admin,Warehouse")]
        [HttpPut("{id}/price")]
        public async Task<IActionResult> UpdateProductPrice(int id, [FromBody] UpdatePriceDto request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            product.Price = request.Price;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật giá thành công!" });
        }
    }

    public class ProductItemDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
    public class StockImportRequestDto
    {
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? ReceiptCode { get; set; }
        public string? Note { get; set; }
    }

}