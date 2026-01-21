using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjetoUsers.Data;
using ProjetoUsers.Models;
using ProjetoUsers.DTOs.User;
using ProjetoUsers.DTOs.Login;
using ProjetoUsers.DTOs.Product;
using ProjetoUsers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ProjetoUsers.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DatabaseContext _DatabaseContext; 
        private readonly TokenService _tokenService;
        public UsersController(DatabaseContext databaseContext, TokenService tokenService)
        {
            _DatabaseContext = databaseContext;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _DatabaseContext.User.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized("Usuário não encontrado");

            bool passwordValid = PasswordService.VerifyPassword(
                dto.Password,
                user.PasswordHash
            );

            if (!passwordValid)
                return Unauthorized("Email ou senha inválidos");

            var token = _tokenService.GenerateToken(user);
            var refreshTokenGenerated = _tokenService.GenerateRefreshToken();
            var refreshEntity = new RefreshToken
            {
                Token = refreshTokenGenerated,
                UserId = user.UserID,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _DatabaseContext.RefreshTokens.Add(refreshEntity);
            _DatabaseContext.SaveChanges();

            return Ok(new { accessToken = token, refreshToken = refreshTokenGenerated });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _DatabaseContext.User
                .Include(u => u.Products)
                .Select(u => new UserResponseWithProductsDto
                {
                    UserID = u.UserID,
                    Name = u.Name,
                    Products = u.Products.Select(p => new ProductResponseDtos
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        CreatedAt = p.CreatedAt
                    }).ToList()
                }).ToListAsync();

            return Ok(users);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _DatabaseContext.User
                .Include(u => u.Products)
                .Where(u => u.UserID == id)
                .Select(u => new UserResponseWithProductsDto
                {
                    UserID = u.UserID,
                    Name = u.Name,
                    Products = u.Products.Select(p => new ProductResponseDtos
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        CreatedAt = p.CreatedAt
                    }).ToList()
                }).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(CreateUserDto dto)
        {
            var emailExists = await _DatabaseContext.User.AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
                return BadRequest("E-mail já cadastrado");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = PasswordService.HashPassword(dto.Password),
                Address = dto.Address,
                AddressNumber = dto.AddressNumber,
                City = dto.City,
                State = dto.State,
                CreatedAt = DateTime.UtcNow,
                Role = "User"
            };
            
            _DatabaseContext.User.Add(user);
            await _DatabaseContext.SaveChangesAsync();

            var response = new UserResponseDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.UserID }, response);
        }
        
        [Authorize(Policy = "AdminOrSelf")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updatedUser)
        {
            var user = await _DatabaseContext.User.FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            if (updatedUser.Name != null)
                user.Name = updatedUser.Name;

            if (updatedUser.Email != null)
                user.Email = updatedUser.Email;

            if (!string.IsNullOrWhiteSpace(updatedUser.Password))
                user.PasswordHash = PasswordService.HashPassword(updatedUser.Password);

            if (updatedUser.Address != null)
                user.Address = updatedUser.Address;

            if (updatedUser.AddressNumber != null)
                user.AddressNumber = updatedUser.AddressNumber;

            if (updatedUser.City != null)
                user.City = updatedUser.City;

            if (updatedUser.State != null)
                user.State = updatedUser.State;

            await _DatabaseContext.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _DatabaseContext.User.FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            _DatabaseContext.User.Remove(user);
            await _DatabaseContext.SaveChangesAsync();

            return NoContent();
        }   

    
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(string refreshToken)
        {
            var token = await _DatabaseContext.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (token == null || token.ExpiresAt < DateTime.UtcNow)
                return Unauthorized("Refresh token inválido");

            if (token.IsRevoked)
            {
                var allTokens = _DatabaseContext.RefreshTokens
                    .Where(t => t.UserId == token.UserId);

                foreach (var t in allTokens)
                    t.IsRevoked = true;

                await _DatabaseContext.SaveChangesAsync();

                return Unauthorized("Tentativa de reutilização de refresh token detectada");
            }

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            _DatabaseContext.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                UserId = token.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            var newAccessToken = _tokenService.GenerateToken(token.User);

            await _DatabaseContext.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }
    }
}