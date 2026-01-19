using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoUsers.Models
{
    public class User
    {
        public int UserID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? AddressNumber { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; }
    }
}