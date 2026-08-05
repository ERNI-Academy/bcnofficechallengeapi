using System.ComponentModel.DataAnnotations;

namespace bcnofficechallengeapi.Dtos;

public class CreateUserRequest
{
    [Required(ErrorMessage = "'fullName' is required.")]
    [MinLength(1, ErrorMessage = "'fullName' cannot be empty.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "'email' is required.")]
    [EmailAddress(ErrorMessage = "'email' is not a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "'password' is required.")]
    [MinLength(1, ErrorMessage = "'password' cannot be empty.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "'companyName' is required.")]
    [MinLength(1, ErrorMessage = "'companyName' cannot be empty.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "'jobTitle' is required.")]
    [MinLength(1, ErrorMessage = "'jobTitle' cannot be empty.")]
    public string JobTitle { get; set; } = string.Empty;

    public string? LinkedIn { get; set; }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? LinkedIn { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public int Points { get; set; }
    public DateTime? PointsTimestamp { get; set; }
    public string? LinkedIn { get; set; }
}
