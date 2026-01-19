using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjetoUsers.Data;
using ProjetoUsers.DTOs.Product;
using ProjetoUsers.Models;
using Microsoft.AspNetCore.Authorization;


namespace ProjetoUsers.Controllers
{   
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly DatabaseContext _databaseContext;
    
        public ProductsController(DatabaseContext databaseContext)
          {
                _databaseContext = databaseContext;
          }
        [AllowAnonymous]
        [HttpGet]     
        public async Task<IActionResult> GetAllProducts()
        {
            var products = _databaseContext.Products.ToList();

            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = _databaseContext.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }
            var response = new ProductResponseDtos
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                Description = product.Description,
                StockQuantity = product.StockQuantity,
                Category = product.Category,
                ImageURL = product.ImageURL,
                CreatedAt = product.CreatedAt
            };
            return Ok(product);
        }
        
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductDtos dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = new Product
            {
                ProductName = dto.ProductName,
                Price = dto.Price,
                Description = dto.Description,
                StockQuantity = dto.StockQuantity,
                Category = dto.Category,
                ImageURL = dto.ImageURL,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _databaseContext.Products.Add(product);
            await _databaseContext.SaveChangesAsync();

            var response = new ProductResponseDtos
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                Description = product.Description,
                StockQuantity = product.StockQuantity,
                Category = product.Category,
                ImageURL = product.ImageURL,
                CreatedAt = product.CreatedAt
            };

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product updatedProduct)
        {
            var product = _databaseContext.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            product.ProductName = updatedProduct.ProductName;
            product.Price = updatedProduct.Price;
            product.Description = updatedProduct.Description;
            product.StockQuantity = updatedProduct.StockQuantity;
            product.Category = updatedProduct.Category;
            product.ImageURL = updatedProduct.ImageURL;
            product.UpdatedAt = DateTime.UtcNow;

            await _databaseContext.SaveChangesAsync();

            return NoContent();
        }   

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = _databaseContext.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            _databaseContext.Products.Remove(product);
            await _databaseContext.SaveChangesAsync();

            return NoContent();
        }    
    }
}