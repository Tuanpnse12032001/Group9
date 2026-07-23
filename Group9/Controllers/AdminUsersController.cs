using Group9.Data;
using Group9.DTOs;
using Group9.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group9.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AdminUsersController(AppDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Id)
                .Select(u => new AdminUserResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = u.Phone,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : ""
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.Id)
                .Select(r => new RoleResponse
                {
                    Id = r.Id,
                    RoleName = r.RoleName
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateAdminUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                return BadRequest(new { message = "FirstName không được để trống." });
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { message = "LastName không được để trống." });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email không được để trống." });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Password không được để trống." });
            }

            var email = request.Email.Trim().ToLower();

            var emailExists = await _context.Users.AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                return BadRequest(new { message = "Email đã tồn tại." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);

            if (role == null)
            {
                return BadRequest(new { message = "Role không hợp lệ." });
            }

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone,
                RoleId = request.RoleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo user thành công.",
                user = new AdminUserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    RoleId = user.RoleId,
                    RoleName = role.RoleName
                }
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateAdminUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);

            if (role == null)
            {
                return BadRequest(new { message = "Role không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                return BadRequest(new { message = "FirstName không được để trống." });
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { message = "LastName không được để trống." });
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Phone = request.Phone;
            user.RoleId = request.RoleId;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật user thành công." });
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, UpdateUserRoleRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);

            if (role == null)
            {
                return BadRequest(new { message = "Role không hợp lệ." });
            }

            user.RoleId = request.RoleId;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật role thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa user thành công." });
        }
    }
}