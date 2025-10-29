using Google.Apis.Auth.OAuth2;
using Medley.Collector.Data;

namespace Medley.Collector.Services;

/// <summary>
/// Service for managing Google OAuth authentication
/// </summary>
public class GoogleAuthService
{
    private readonly ConfigurationService _configurationService;

    public GoogleAuthService()
    {
        _configurationService = new ConfigurationService();
    }

    /// <summary>
    /// Gets the stored Google OAuth credential if available
    /// </summary>
    public async Task<UserCredential?> GetCredentialAsync()
    {
        var clientId = await _configurationService.GetGoogleClientIdAsync();
        var clientSecret = await _configurationService.GetGoogleClientSecretAsync();

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var hasToken = await _configurationService.HasGoogleTokenAsync();
        if (!hasToken)
        {
            return null;
        }

        try
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Define the scopes - should match what was used during authentication
            string[] scopes = {
                "https://www.googleapis.com/auth/drive.readonly",
                "https://www.googleapis.com/auth/drive.meet.readonly",
                "https://www.googleapis.com/auth/drive.metadata.readonly",
                "https://www.googleapis.com/auth/drive.labels.readonly"
            };

            // This will load the existing token from the database
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                scopes,
                "user",
                CancellationToken.None,
                new DatabaseDataStore());

            return credential;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the user has authenticated with Google OAuth
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _configurationService.HasGoogleTokenAsync();
    }
}
