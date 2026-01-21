using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoUsers.DTOs.Product
{
    public class FilterProductDto
    {

        public string? Name { get; set; }

        public string? Category { get; set; }

        public string? OrderBy { get; set; }
        // asc or desc
        public string? Direction { get; set; }
    }
}