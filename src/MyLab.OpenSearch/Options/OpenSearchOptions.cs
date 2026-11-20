namespace MyLab.OpenSearch.Options;

public class OpenSearchOptions
{
    public const string SectionName = "OpenSearch";

    public string? Host { get; set; }

    public string[]? HostPool { get; set; }

    public OpenSearchApiKeyAuthentication? ApiKeyAuth { get; set; }

    public OpenSearchBasicAuthentication? BasicAuth { get; set; }

    public bool IgnoreServerSslCertificate { get; set; }
}

public class OpenSearchBasicAuthentication
{
    public string? User { get; set; }
    public string? Password { get; set; }

    public bool IsValid() => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);
}

public class OpenSearchApiKeyAuthentication
{
    public string? Id { get; set; }
    public string? Token { get; set; }

    public bool IsValid() => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Token);
}