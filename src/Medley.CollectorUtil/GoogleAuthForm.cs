using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Medley.CollectorUtil.Data;
using System.Text.Json;
using Microsoft.Playwright;
using Medley.CollectorUtil.Models;

namespace Medley.CollectorUtil;

public partial class GoogleAuthForm : Form
{
    private readonly ConfigurationService _configurationService;

    public GoogleAuthForm()
    {
        InitializeComponent();

        _configurationService = new ConfigurationService();
    }

    private async void GoogleAuthForm_Load(object sender, EventArgs e)
    {
        // Load existing credentials from database
        try
        {
            var clientId = await _configurationService.GetGoogleClientIdAsync();
            var clientSecret = await _configurationService.GetGoogleClientSecretAsync();
            var folderId = await _configurationService.GetGoogleDriveFolderIdAsync();

            textBoxClientId.Text = clientId;
            textBoxClientSecret.Text = clientSecret;
            textBoxFolderId.Text = folderId;
        }
        catch
        {
            // Ignore errors loading credentials
        }

        // Check if already authenticated
        UpdateAuthStatus();
    }

    private async void UpdateAuthStatus()
    {
        bool hasOAuthToken = await _configurationService.HasGoogleTokenAsync();
        var cookies = await _configurationService.GetGoogleBrowserCookiesAsync();
        bool hasCookies = cookies.Any();

        if (hasOAuthToken || hasCookies)
        {
            var authMethod = hasOAuthToken && hasCookies ? "OAuth + Browser" :
                           hasOAuthToken ? "OAuth" : "Browser";
            labelStatus.Text = $"Status: Authenticated ({authMethod})";
            labelStatus.ForeColor = Color.Green;
            buttonAuthenticate.Text = "Re-authenticate";
            buttonRevoke.Enabled = true;
        }
        else
        {
            labelStatus.Text = "Status: Not authenticated";
            labelStatus.ForeColor = Color.Red;
            buttonAuthenticate.Text = "OAuth Flow";
            buttonRevoke.Enabled = false;
        }
    }

    private async void buttonSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(textBoxClientId.Text))
        {
            MessageBox.Show("Please enter a Client ID.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(textBoxClientSecret.Text))
        {
            MessageBox.Show("Please enter a Client Secret.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await _configurationService.SaveGoogleClientIdAsync(textBoxClientId.Text.Trim());
            await _configurationService.SaveGoogleClientSecretAsync(textBoxClientSecret.Text.Trim());
            await _configurationService.SaveGoogleDriveFolderIdAsync(textBoxFolderId.Text.Trim());

            MessageBox.Show("Settings saved successfully.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void buttonAuthenticate_Click(object sender, EventArgs e)
    {
        try
        {
            buttonAuthenticate.Enabled = false;
            Cursor = Cursors.WaitCursor;

            var clientId = await _configurationService.GetGoogleClientIdAsync();
            var clientSecret = await _configurationService.GetGoogleClientSecretAsync();

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                MessageBox.Show("Please save your Client ID and Secret first.", "Credentials Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Define the scopes you need - adjust based on your requirements
            string[] scopes = {
                "https://www.googleapis.com/auth/drive.readonly",
                "https://www.googleapis.com/auth/drive.meet.readonly",
                "https://www.googleapis.com/auth/drive.metadata.readonly",
                "https://www.googleapis.com/auth/drive.labels.readonly"
            };

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Use GoogleWebAuthorizationBroker to authenticate with database storage
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                scopes,
                "user",
                CancellationToken.None,
                new DatabaseDataStore());

            MessageBox.Show("Authentication successful! You can now use Google services.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            UpdateAuthStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Authentication failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            buttonAuthenticate.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private async void buttonRevoke_Click(object sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to revoke authentication? You will need to re-authenticate to use Google services.",
            "Confirm Revoke",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            try
            {
                // Delete OAuth tokens from database
                await _configurationService.DeleteAllGoogleTokensAsync();

                // Delete cookies from database
                await _configurationService.DeleteGoogleBrowserCookiesAsync();

                MessageBox.Show("Authentication revoked successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateAuthStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error revoking authentication: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void buttonAuthenticateBrowser_Click(object sender, EventArgs e)
    {
        try
        {
            buttonAuthenticateBrowser.Enabled = false;
            Cursor = Cursors.WaitCursor;

            var confirmResult = MessageBox.Show(
                "This will open a browser window where you can sign in to Google.\n\n" +
                "If this is your first time, the browser will be downloaded automatically (~100MB).\n\n" +
                "Continue?",
                "Browser Authentication",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (confirmResult != DialogResult.Yes)
            {
                return;
            }

            await AuthenticateWithBrowserAsync();

            MessageBox.Show(
                "Authentication successful! Your session has been saved.\n\n" +
                "You can now use Google services without entering credentials again.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            UpdateAuthStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Browser authentication failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            buttonAuthenticateBrowser.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private async Task AuthenticateWithBrowserAsync()
    {
        // Ensure Playwright browsers are installed
        await EnsurePlaywrightBrowsersInstalledAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // Show browser so user can log in
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        var page = await context.NewPageAsync();

        // Navigate to Google Drive to trigger authentication
        await page.GotoAsync("https://drive.google.com/drive");

        // Wait for user to complete authentication by checking for SID cookie
        var authCompleted = false;
        var timeout = TimeSpan.FromMinutes(5);
        var startTime = DateTime.Now;

        while (!authCompleted && DateTime.Now - startTime < timeout)
        {
            try
            {
                // Check for the SID cookie which indicates successful Google authentication
                var cookies = await context.CookiesAsync();
                var sidCookie = cookies.FirstOrDefault(c =>
                    c.Name == "SID" && c.Domain.Contains("google.com"));

                if (sidCookie != null)
                {
                    // Wait a bit to ensure all cookies are set
                    await Task.Delay(2000);
                    authCompleted = true;
                    break;
                }

                await Task.Delay(1000);
            }
            catch
            {
                // Continue waiting
                await Task.Delay(1000);
            }
        }

        if (!authCompleted)
        {
            throw new Exception("Authentication timed out. Please try again and complete the sign-in process.");
        }

        // Save cookies for future use - convert to BrowserCookie objects
        var allCookies = await context.CookiesAsync();
        var browserCookies = allCookies.Select(c => new BrowserCookie
        {
            Name = c.Name,
            Value = c.Value,
            Domain = c.Domain,
            Path = c.Path,
            Expires = c.Expires,
            HttpOnly = c.HttpOnly,
            Secure = c.Secure,
            SameSite = c.SameSite.ToString()
        }).ToList();
        
        await _configurationService.SaveGoogleBrowserCookiesAsync(browserCookies);

        await browser.CloseAsync();
    }

    private async Task EnsurePlaywrightBrowsersInstalledAsync()
    {
        try
        {
            // Try to create Playwright - if browsers aren't installed, this will throw
            using var testPlaywright = await Playwright.CreateAsync();
            var browserType = testPlaywright.Chromium;

            // Try to get executable path - if it doesn't exist, we need to install
            try
            {
                await browserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist"))
            {
                // Browsers need to be installed
                await InstallPlaywrightBrowsersAsync();
            }
        }
        catch (Exception ex)
        {
            // If any error occurs, try to install browsers
            if (ex.Message.Contains("Executable doesn't exist") || ex.Message.Contains("Browser was not found"))
            {
                await InstallPlaywrightBrowsersAsync();
            }
            else
            {
                throw;
            }
        }
    }

    private async Task InstallPlaywrightBrowsersAsync()
    {
        using var progressForm = new Form
        {
            Text = "Installing Browser",
            Width = 400,
            Height = 170,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ControlBox = false
        };

        var label = new Label
        {
            Text = "Downloading and installing Chromium browser...\nThis may take a few minutes.",
            AutoSize = false,
            Width = 360,
            Height = 60,
            Left = 20,
            Top = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            Width = 360,
            Left = 20,
            Top = 80
        };

        progressForm.Controls.Add(label);
        progressForm.Controls.Add(progressBar);

        // Run installation in background task
        var installTask = Task.Run(() =>
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
            return exitCode;
        });

        // Show progress form non-blocking
        progressForm.Show(this);

        try
        {
            // Wait for installation to complete
            var exitCode = await installTask;

            if (exitCode != 0)
            {
                throw new Exception($"Playwright browser installation failed with exit code {exitCode}");
            }

            // Update UI on completion
            if (progressForm.InvokeRequired)
            {
                progressForm.Invoke(() => label.Text = "Installation complete!");
            }
            else
            {
                label.Text = "Installation complete!";
            }

            await Task.Delay(1000);
        }
        finally
        {
            if (progressForm.InvokeRequired)
            {
                progressForm.Invoke(() => progressForm.Close());
            }
            else
            {
                progressForm.Close();
            }
        }
    }

    /// <summary>
    /// Checks if browser-based authentication cookies exist
    /// </summary>
    public static async Task<bool> HasBrowserAuthenticationAsync()
    {
        var configService = new ConfigurationService();
        var cookies = await configService.GetGoogleBrowserCookiesAsync();
        return cookies.Any();
    }
}
