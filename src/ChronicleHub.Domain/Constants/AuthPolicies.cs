namespace ChronicleHub.Domain.Constants;

public static class AuthPolicies
{
    public const string RequireAuthentication = "RequireAuthentication";
    public const string RequireTenantMembership = "RequireTenantMembership";
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireOwnerRole = "RequireOwnerRole";
    public const string ApiKeyOnly = "ApiKeyOnly";
}
