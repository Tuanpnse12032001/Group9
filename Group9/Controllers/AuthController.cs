using Group9.Data;
using Group9.DTOs;
using Group9.Models;
using Group9.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtService _jwtService;

        public AuthController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            JwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var existedUser = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (existedUser)
            {
                return BadRequest(new
                {
                    message = "Email đã tồn tại."
                });
            }

            var userRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "User");

            if (userRole == null)
            {
                return BadRequest(new
                {
                    message = "Role User chưa tồn tại trong database."
                });
            }

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone,
                RoleId = userRole.Id,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đăng ký tài khoản thành công."
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Email hoặc mật khẩu không đúng."
                });
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Email hoặc mật khẩu không đúng."
                });
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiryTime(),
                User = MapToUserResponse(user)
            });
        }

        [HttpPost("logout")]
        [Authorize(Roles = "User,Admin,ChatbotService")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
            {
                return Unauthorized();
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đăng xuất thành công."
            });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Refresh token không hợp lệ."
                });
            }

            if (user.RefreshTokenExpiryTime == null ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized(new
                {
                    message = "Refresh token đã hết hạn."
                });
            }

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiryTime(),
                User = MapToUserResponse(user)
            });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                return Ok(new
                {
                    message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu sẽ được gửi."
                });
            }

            var token = _jwtService.GenerateResetPasswordToken();

            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiryTime = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Demo: trả token ra để test Postman/Swagger.
            // Thực tế: phải gửi token qua email, không nên trả trực tiếp.
            return Ok(new
            {
                message = "Token đặt lại mật khẩu đã được tạo.",
                resetToken = token
            });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                return BadRequest(new
                {
                    message = "Thông tin reset mật khẩu không hợp lệ."
                });
            }

            if (user.ResetPasswordToken != request.Token ||
                user.ResetPasswordTokenExpiryTime == null ||
                user.ResetPasswordTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    message = "Token reset mật khẩu không hợp lệ hoặc đã hết hạn."
                });
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiryTime = null;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đặt lại mật khẩu thành công."
            });
        }

        [HttpGet("me")]
        [Authorize(Roles = "User,Admin,ChatbotService")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(MapToUserResponse(user));
        }

        [HttpPut("profile")]
        [Authorize(Roles = "User,Admin,ChatbotService")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return Unauthorized();
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Phone = request.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật profile thành công.",
                user = MapToUserResponse(user)
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdValue))
            {
                return null;
            }

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return userId;
        }

        private static UserResponse MapToUserResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role?.RoleName ?? string.Empty
            };
        }
    }
}