using Integration.Ricardo.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integration.Ricardo.Services;

/// <summary>
/// Client for ricardo.ch API communication
/// </summary>
public class RicardoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RicardoSettings _settings;
    private readonly ILogger<RicardoApiClient> _logger;
    private RicardoTokenCredential _tokenCredential;

    public RicardoApiClient(
        HttpClient httpClient,
        RicardoSettings settings,
        ILogger<RicardoApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        // Configure base URL based on sandbox setting
        _httpClient.BaseAddress = new Uri(
            _settings.UseSandbox
                ? RicardoDefaults.SandboxApiBaseUrl
                : RicardoDefaults.ApiBaseUrl
        );
    }

    /// <summary>
    /// Authenticate with ricardo.ch API
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var request = new
            {
                partnerKey = _settings.PartnerKey,
                partnerPartnerId = _settings.PartnerId,
                customerUsername = _settings.AccountUsername,
                customerPassword = _settings.AccountPassword
            };

            var response = await PostAsync<RicardoTokenCredential>(
                RicardoDefaults.SecurityService,
                "TokenCredentialLogin",
                request,
                authenticateFirst: false
            );

            if (response != null && !string.IsNullOrEmpty(response.TokenCredential))
            {
                _tokenCredential = response;
                _logger.LogInformation("Successfully authenticated with ricardo.ch");
                return true;
            }

            _logger.LogError("Failed to authenticate with ricardo.ch: No token received");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with ricardo.ch");
            return false;
        }
    }

    /// <summary>
    /// Insert new article on ricardo.ch
    /// </summary>
    public async Task<InsertArticleResponse> InsertArticleAsync(InsertArticleRequest request)
    {
        try
        {
            return await PostAsync<InsertArticleResponse>(
                RicardoDefaults.ArticlesService,
                "InsertArticle",
                request
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting article on ricardo.ch");
            return new InsertArticleResponse
            {
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Update article quantity
    /// </summary>
    public async Task<UpdateArticleQuantityResponse> UpdateArticleQuantityAsync(long articleId, int newQuantity)
    {
        try
        {
            var request = new UpdateArticleQuantityRequest
            {
                ArticleId = articleId,
                NewQuantity = newQuantity
            };

            return await PostAsync<UpdateArticleQuantityResponse>(
                RicardoDefaults.ArticlesService,
                "UpdateArticleQuantity",
                request
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating article quantity on ricardo.ch");
            return new UpdateArticleQuantityResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Close/delete article on ricardo.ch
    /// </summary>
    public async Task<CloseArticleResponse> CloseArticleAsync(long articleId)
    {
        try
        {
            var request = new CloseArticleRequest
            {
                ArticleId = articleId
            };

            return await PostAsync<CloseArticleResponse>(
                RicardoDefaults.ArticlesService,
                "CloseArticle",
                request
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing article on ricardo.ch");
            return new CloseArticleResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Generic POST request to ricardo.ch API
    /// </summary>
    private async Task<TResponse> PostAsync<TResponse>(
        string service,
        string method,
        object payload,
        bool authenticateFirst = true)
    {
        // Authenticate if needed
        if (authenticateFirst && (_tokenCredential == null || IsTokenExpired()))
        {
            var authenticated = await AuthenticateAsync();
            if (!authenticated)
            {
                throw new Exception("Failed to authenticate with ricardo.ch");
            }
        }

        var requestBody = new
        {
            jsonrpc = "2.0",
            method = method,
            @params = new[] { payload },
            id = Guid.NewGuid().ToString()
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        // Add token if authenticated
        if (_tokenCredential != null)
        {
            content.Headers.Add("Token-Credential", _tokenCredential.TokenCredential);
        }

        var response = await _httpClient.PostAsync(service, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("ricardo.ch API error: {StatusCode} - {Content}",
                response.StatusCode, responseContent);
            throw new Exception($"ricardo.ch API error: {response.StatusCode}");
        }

        var jsonRpcResponse = JsonSerializer.Deserialize<JsonRpcResponse<TResponse>>(responseContent);

        if (jsonRpcResponse?.Error != null)
        {
            _logger.LogError("ricardo.ch API error: {ErrorCode} - {ErrorMessage}",
                jsonRpcResponse.Error.Code, jsonRpcResponse.Error.Message);
            throw new Exception($"ricardo.ch API error: {jsonRpcResponse.Error.Message}");
        }

        return jsonRpcResponse.Result;
    }

    private bool IsTokenExpired()
    {
        if (_tokenCredential == null || string.IsNullOrEmpty(_tokenCredential.TokenExpirationDate))
            return true;

        if (DateTime.TryParse(_tokenCredential.TokenExpirationDate, out var expirationDate))
        {
            return DateTime.UtcNow >= expirationDate;
        }

        return true;
    }

    private class JsonRpcResponse<T>
    {
        [JsonPropertyName("result")]
        public T Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError Error { get; set; }
    }

    private class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
