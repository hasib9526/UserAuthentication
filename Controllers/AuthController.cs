using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already exists");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking() // Optional: improves read performance
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null)
                    return Unauthorized("Invalid credentials");

                if (string.IsNullOrEmpty(user.PasswordHash))
                    return StatusCode(500, "PasswordHash is null for this user. Data might be corrupted.");

                if (user.PasswordHash != HashPassword(dto.Password))
                    return Unauthorized("Invalid credentials");

                return Ok("Login successful");
            }
            catch (Exception ex)
            {
                // Log this in real scenario using ILogger or any logger service
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound("User not found");

            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPGeneratedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await EmailService.SendEmail(dto.Email, "OTP for Password Reset", $"Your OTP is: {otp}");

            return Ok("OTP sent to email");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.OTP != dto.OTP || user.OTPGeneratedAt == null || (DateTime.UtcNow - user.OTPGeneratedAt.Value).TotalMinutes > 10)
                return BadRequest("Invalid or expired OTP");

            user.PasswordHash = HashPassword(dto.NewPassword);
            user.OTP = null;
            user.OTPGeneratedAt = null;
            await _context.SaveChangesAsync();

            return Ok("Password has been reset successfully");
        }
    }
}
