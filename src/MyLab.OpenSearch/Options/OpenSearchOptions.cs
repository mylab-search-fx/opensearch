namespace MyLab.OpenSearch.Options
{
    public class OpenSearchOptions
    {
        public const string SectionName = "OpenSearch";

        public string? Host { get; set; }
        public string[]? HostPool { get; set; }
    }
}
