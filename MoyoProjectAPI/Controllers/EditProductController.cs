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
    public class EditProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EditProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint to apply EditProduct changes to Product and delete EditProduct records
        [HttpPost("ApplyUpdateProduct")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ApplyUpdateProduct([FromBody] UpdateModel model)
        {
            var editProduct = await _context.EditProducts.FindAsync(model.editProductId);
            if (editProduct == null || editProduct.ProductId != model.productId)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(model.productId);
            if (product == null)
            {
                return NotFound();
            }

            product.Name = editProduct.Name;
            product.Description = editProduct.Description;

            _context.EditProducts.RemoveRange(_context.EditProducts.Where(ep => ep.ProductId == model.productId));
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("GetProductsWithEdits")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsWithEdits()
        {
            var productsWithEdits = await _context.Products
                .Include(p => p.EditProducts)
                .Where(p => p.EditProducts.Any())
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    EditProducts = p.EditProducts.Select(ep => new EditProductDto
                    {
                        Id = ep.Id,
                        Name = ep.Name,
                        Description = ep.Description
                    }).ToList()
                })
                .ToListAsync();

            return productsWithEdits;
        }



        // Endpoint to delete an EditProduct record by its ID
        [HttpDelete("DeleteEditProduct/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteEditProduct(int id)
        {
            var editProduct = await _context.EditProducts.FindAsync(id);
            if (editProduct == null)
            {
                return NotFound();
            }

            _context.EditProducts.Remove(editProduct);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<EditProductDto> EditProducts { get; set; }
}

public class EditProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class UpdateModel
{
    public int productId { get; set; }
    public int editProductId { get; set; }
}