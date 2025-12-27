using Microsoft.AspNetCore.Identity;

namespace ChronicleHub.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ApplicationUser()
    {
        CreatedAtUtc = DateTime.UtcNow;
    }
}
