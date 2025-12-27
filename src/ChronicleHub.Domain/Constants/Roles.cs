namespace ChronicleHub.Domain.Constants;

public static class Roles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";

    public static readonly string[] AllRoles = { Owner, Admin, Member };

    public static bool IsValid(string role)
    {
        return AllRoles.Contains(role);
    }
}
