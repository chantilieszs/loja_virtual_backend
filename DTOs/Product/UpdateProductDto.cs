using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoUsers.DTOs.Product
{
    public class UpdateProductDto
    {
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public int? StockQuantity { get; set; }
        public string? Category { get; set; }
        public string? ImageURL { get; set; }
    }
}