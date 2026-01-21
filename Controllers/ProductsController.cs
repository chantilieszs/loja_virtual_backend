using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjetoUsers.Data;
using ProjetoUsers.DTOs.Product;
using ProjetoUsers.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


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

        [HttpGet("filter")]
        public async Task<IActionResult> GetProducts(FilterProductDto query)
        {
            var productsQuery = _databaseContext.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(query.Name));
            }

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                productsQuery = productsQuery
                    .Where(p => p.Category == query.Category);
            }
            var validOrderBy = new[] { "price"};
            if (!validOrderBy.Contains(query.OrderBy?.ToLower()))
            {
                return BadRequest("OrderBy inválido");
            }
            if (!string.IsNullOrWhiteSpace(query.OrderBy))
            {
                var direction = query.Direction?.ToLower() ?? "asc";

                productsQuery = query.OrderBy.ToLower() switch
                {
                    "price" => direction == "desc" ? productsQuery.OrderByDescending(p => p.Price) : productsQuery.OrderBy(p => p.Price),
                    _ => productsQuery
                };
            }

            var products = await productsQuery.ToListAsync();

            return Ok(products);
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

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var product = new Product
            {
                ProductName = dto.ProductName,
                Price = dto.Price,
                Description = dto.Description,
                StockQuantity = dto.StockQuantity,
                Category = dto.Category,
                ImageURL = dto.ImageURL,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId
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
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto updatedProduct)
        {
            var product = _databaseContext.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            if (updatedProduct.ProductName != null)
                product.ProductName = updatedProduct.ProductName;

            if (updatedProduct.Price.HasValue)
                product.Price = updatedProduct.Price.Value;

            if (updatedProduct.Description != null)
                product.Description = updatedProduct.Description;

            if (updatedProduct.StockQuantity.HasValue)
                product.StockQuantity = updatedProduct.StockQuantity.Value;

            if (updatedProduct.Category != null)
                product.Category = updatedProduct.Category;

            if (updatedProduct.ImageURL != null)
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