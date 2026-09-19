namespace TaskManagement.Api.Helpers
{
    // Bound from configuration (appsettings + environment variables / user-secrets).
    // No secret values live in source control - see appsettings.json comments and README.
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;
    }
}
