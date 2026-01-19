using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjetoUsers.Data;
using ProjetoUsers.Models;
using ProjetoUsers.DTOs.User;
using ProjetoUsers.DTOs.Login;
using ProjetoUsers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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

            var user =  _DatabaseContext.User.FirstOrDefault(u => u.Email == dto.Email);

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
            var users = _DatabaseContext.User.Select(u => new UserResponseDto
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Ok(users);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = _DatabaseContext.User.FirstOrDefault(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            var response = new UserResponseDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            var emailExists = _DatabaseContext.User.Any(u => u.Email == dto.Email);

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] Models.User updatedUser)
        {
            var user = _DatabaseContext.User.FirstOrDefault(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.Address = updatedUser.Address;
            user.AddressNumber = updatedUser.AddressNumber;
            user.City = updatedUser.City;
            user.State = updatedUser.State;
            user.PasswordHash = updatedUser.PasswordHash;

            await _DatabaseContext.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = _DatabaseContext.User.FirstOrDefault(u => u.UserID == id);

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
            var token = _DatabaseContext.RefreshTokens.Include(r => r.User).FirstOrDefault(r => r.Token == refreshToken);

            if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
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