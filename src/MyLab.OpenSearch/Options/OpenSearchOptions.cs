namespace MyLab.OpenSearch.Options;

public class OpenSearchOptions
{
    public const string SectionName = "OpenSearch";

    public string? Host { get; set; }

    public string[]? HostPool { get; set; }

    public OpenSearchBasicAuthentication? BasicAuth { get; set; }

    public bool IgnoreServerSslCertificate { get; set; }
}