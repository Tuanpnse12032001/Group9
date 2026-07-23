namespace Group9.DTOs
{
    public class AdminUserResponse
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;
    }

    public class RoleResponse
    {
        public int Id { get; set; }

        public string RoleName { get; set; } = string.Empty;
    }

    public class CreateAdminUserRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; }
    }

    public class UpdateAdminUserRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int RoleId { get; set; }
    }

    public class UpdateUserRoleRequest
    {
        public int RoleId { get; set; }
    }
}