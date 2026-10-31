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
    public class CategoriesController : ControllerBase
    {
        private readonly CoolMartDbContext _context;

        public CategoriesController(CoolMartDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Categories.AsNoTracking().ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound(new { message = "Không tìm thấy danh mục!" });
            return Ok(cat);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category newCat)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Categories.Add(newCat);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newCat.CategoryId }, newCat);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category updateCat)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            cat.CategoryName = updateCat.CategoryName;
            cat.Description = updateCat.Description;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}