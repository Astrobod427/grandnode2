namespace Integration.Ricardo;

public static class RicardoDefaults
{
    public const string ProviderSystemName = "Integration.Ricardo";
    public const string FriendlyName = "Integration.Ricardo.FriendlyName";
    public const string ConfigurationUrl = "/Admin/Ricardo/Configure";

    // ricardo.ch API endpoints
    public const string ApiBaseUrl = "https://ws.ricardo.ch/ricardoapi/";
    public const string SandboxApiBaseUrl = "https://ws.test.ricardo.ch/ricardoapi/";

    // API Services
    public const string SecurityService = "SecurityService.json";
    public const string ArticlesService = "ArticlesService.json";
    public const string SystemService = "SystemService.json";
    public const string SearchService = "SearchService.json";
}
