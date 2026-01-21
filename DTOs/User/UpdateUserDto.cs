using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoUsers.DTOs.User
{
    public class UpdateUserDto
    {
        public string? Name { get; set; } = string.Empty;

        public string? Email { get; set; } = string.Empty;

        public string? Password { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? AddressNumber { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }
    }
}