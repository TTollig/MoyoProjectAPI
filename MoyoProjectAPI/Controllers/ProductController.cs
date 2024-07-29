using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoyoProjectAPI.Data;
using MoyoProjectAPI.Data.ProductAPI.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoyoProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("AddProduct")]
        [Authorize(Roles = "Capturer,Manager")]
        public async Task<IActionResult> AddProduct(Product product)
        {
            product.Status = "Created"; // Automatically set the status to "Created"
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpPut("UpdateProductStatus/{id}/status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateProductStatus(int id, [FromBody] UpdateStatus status)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (status.status != "Approved" && status.status != "Deleted")
            {
                return BadRequest("Invalid status value.");
            }

            product.Status = status.status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("UpdateProduct/{id}")]
        [Authorize(Roles = "Capturer,Manager")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Update the product properties except ID and status
            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("GetCreatedProducts")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<IEnumerable<Product>>> GetCreatedProducts()
        {
            return await _context.Products.Where(p => p.Status == "Created").ToListAsync();
        }

        [HttpGet("GetDeletedProducts")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<IEnumerable<Product>>> GetDeletedProducts()
        {
            return await _context.Products.Where(p => p.Status == "Deleted").ToListAsync();
        }

        [HttpGet("GetApprovedProducts")]
        [Authorize(Roles = "Capturer,Manager")]
        public async Task<ActionResult<IEnumerable<Product>>> GetApprovedProducts()
        {
            return await _context.Products.Where(p => p.Status == "Approved").ToListAsync();
        }

        [HttpGet("GetProductById/{id}")]
        [Authorize]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }
    }
}

public class UpdateStatus
{
    public string status { get; set; }
}
