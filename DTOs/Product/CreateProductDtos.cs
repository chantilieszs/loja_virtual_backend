using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace ProjetoUsers.DTOs.Product
{
    public class CreateProductDtos
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório")]
        [MaxLength(120)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Preço inválido")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "A descrição do produto é obrigatória")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        [Url(ErrorMessage = "URL inválida")]
        public string? ImageURL { get; set; }
    }
}