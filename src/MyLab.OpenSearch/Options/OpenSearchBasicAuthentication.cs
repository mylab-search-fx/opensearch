namespace MyLab.OpenSearch.Options;

public class OpenSearchBasicAuthentication
{
    public string? User { get; set; }
    public string? Password { get; set; }

    public bool IsValid() => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);
}