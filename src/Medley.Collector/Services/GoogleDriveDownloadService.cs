using Medley.Collector.Data;
using Medley.Collector.Models;
using Microsoft.Playwright;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Medley.Collector.Services;

/// <summary>
/// Service for downloading Google Meet transcripts using HttpClient with Playwright fallback
/// </summary>
public class GoogleDriveDownloadService
{
    private readonly ConfigurationService _configurationService;
    private List<BrowserCookie>? _cookies;

    public GoogleDriveDownloadService(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Downloads transcript for a Google Drive video, first trying direct HTTP download, then falling back to Playwright
    /// </summary>
    public async Task<MeetingTranscript?> DownloadTranscriptAsync(
        GoogleDriveVideo video,
        CancellationToken cancellationToken = default)
    {
        // Get browser cookies for authentication
        if (_cookies == null)
        {
            _cookies = await _configurationService.GetGoogleBrowserCookiesAsync();
            if (_cookies.Count == 0)
            {
                throw new InvalidOperationException("Browser authentication required. Please authenticate using the Browser Auth button in Google Auth.");
            }
        }

        // Try to download captions directly first (faster approach)
        var captionUrl = $"https://drive.google.com/uc?ttlang=en&ttkind=asr&id={video.Id}&export=timedtext";
        //Console.WriteLine($"  Attempting direct download: {captionUrl}");

        // Create HTTP client to download the caption file
        using var httpClient = new HttpClient();

        // Copy cookies to HttpClient
        CopyCookiesToHttpClient(_cookies, httpClient);

        bool downloadSuccessful = false;
        MeetingTranscript? transcript = null;

        try
        {
            // Download the caption content
            var response = await httpClient.GetAsync(captionUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var captionContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrEmpty(captionContent))
                {
                    // Check if response is HTML (indicates authentication failure even with 200 status)
                    if (IsHtmlContent(captionContent))
                    {
                        //Console.WriteLine($"  Authentication failed - cookies are invalid or expired");
                        throw new InvalidOperationException(
                            "Authentication failed. Your browser cookies are invalid or expired. " +
                            "Please re-authenticate using the Browser Auth button in Google Auth.");
                    }
                    else
                    {
                        // Parse the caption content into structured transcript
                        video.Transcript = ParseCaptionContent(captionContent);

                        // Serialize the entire DriveVideo object to JSON
                        var jsonContent = JsonSerializer.Serialize(video, new JsonSerializerOptions 
                        { 
                            WriteIndented = false 
                        });

                        transcript = new MeetingTranscript
                        {
                            ExternalId = video.Id,
                            Title = video.Name,
                            Date = video.CreatedTime,
                            Content = jsonContent,
                            Source = TranscriptSource.Google,
                            SourceDetail = video.FolderPath != null && video.FolderPath.Length > 0 
                                ? string.Join(" > ", video.FolderPath) 
                                : null,
                            DownloadedAt = DateTime.UtcNow,
                            LengthInMinutes = video.DurationMillis == null ? null : (int)(video.DurationMillis.Value / 1000 / 60),
                            TranscriptLength = video.Transcript.Sum(x => x.Text.Length),
                            Participants = video.LastModifyingUserDisplayName ?? video.LastModifyingUserEmail
                        };

                        //Console.WriteLine($"  Captions downloaded successfully via HTTP");
                        //Console.WriteLine($"  Serialized DriveVideo to JSON ({jsonContent.Length} characters)");
                        downloadSuccessful = true;
                    }
                }
                else
                {
                    //Console.WriteLine($"  No caption content received for video {video.Id}");
                }
            }
            else
            {
                //Console.WriteLine($"  Direct download failed with status: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            //Console.WriteLine($"  HTTP error during direct download: {ex.Message}");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"  Error during direct download: {ex.Message}");
        }

        // If direct download succeeded, return the transcript
        if (downloadSuccessful && transcript != null)
        {
            return transcript;
        }

        // If direct download failed, check if captions exist and trigger generation if needed
        //Console.WriteLine($"  Direct download failed, checking caption status...");
        await TriggerCaptionGenerationIfNeededAsync(video);
        
        return null;
    }

    /// <summary>
    /// Checks caption status and triggers automatic generation if captions are not yet generated
    /// </summary>
    private async Task TriggerCaptionGenerationIfNeededAsync(GoogleDriveVideo video)
    {
        var captionEditUrl = $"https://drive.google.com/video/captions/edit?id={video.Id}";
        //Console.WriteLine($"  Checking caption status: {captionEditUrl}");

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync();
        
        // Add cookies to browser context
        if (_cookies != null && _cookies.Any())
        {
            var playwrightCookies = _cookies.Select(c => new Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = c.SameSite switch
                {
                    "Lax" => SameSiteAttribute.Lax,
                    "Strict" => SameSiteAttribute.Strict,
                    "None" => SameSiteAttribute.None,
                    _ => SameSiteAttribute.None
                }
            }).ToList();

            await context.AddCookiesAsync(playwrightCookies);
        }

        var page = await context.NewPageAsync();
        
        try
        {
            await page.GotoAsync(captionEditUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Look for the button that contains the "Generate automatic captions" text
            var generateButtons = await page.QuerySelectorAllAsync("button:has(.mUIrbf-vQzf8d)");
            
            if (generateButtons.Count > 0)
            {
                // Check each button to find the one with the correct text
                foreach (var button in generateButtons)
                {
                    var buttonText = await button.TextContentAsync();
                    if (buttonText != null && buttonText.Contains("Generate automatic captions"))
                    {
                        // Check if button is disabled (already clicked)
                        var isDisabled = await button.IsDisabledAsync();
                        if (isDisabled)
                        {
                            //Console.WriteLine($"  Generate button is disabled - caption generation already in progress");
                            break;
                        }

                        //Console.WriteLine($"  Captions not generated yet, triggering automatic generation...");
                        await button.ClickAsync();
                        await page.WaitForTimeoutAsync(1000); // Wait for generation to start
                        //Console.WriteLine($"  Automatic caption generation triggered, skipping download for now");
                        break;
                    }
                }
            }
            else
            {
                //Console.WriteLine($"  No generate button found - captions may already be generated but download failed");
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"  Error checking caption status: {ex.Message}");
        }
        finally
        {
            await page.CloseAsync();
            await browser.CloseAsync();
            playwright.Dispose();
        }

        //Console.WriteLine($"  Unable to download captions for video {video.Id}");
    }

    private static void CopyCookiesToHttpClient(List<BrowserCookie> cookies, HttpClient httpClient)
    {
        try
        {
            if (cookies.Any())
            {
                //Console.WriteLine($"  Copying {cookies.Count} cookies to HttpClient");

                // Create cookie header string
                var cookieHeader = string.Join("; ", cookies.Select(cookie => $"{cookie.Name}={cookie.Value}"));

                // Add cookies to the HttpClient default headers
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                // Also add User-Agent to match browser
                httpClient.DefaultRequestHeaders.Add("User-Agent", 
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                //Console.WriteLine($"  Authentication cookies copied successfully");
            }
            else
            {
                //Console.WriteLine($"  No cookies found in browser context");
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"  Error copying cookies: {ex.Message}");
        }
    }

    private static bool IsHtmlContent(string content)
    {
        // Check if content starts with common HTML indicators
        var trimmedContent = content.TrimStart();
        return trimmedContent.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
               trimmedContent.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
               trimmedContent.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               (trimmedContent.Contains("<html") && trimmedContent.Contains("</html>"));
    }

    private static List<GoogleTranscriptSegment> ParseCaptionContent(string captionContent)
    {
        var lines = captionContent.Split('\n');
        var segments = new List<GoogleTranscriptSegment>();
        
        // Regex to match timestamp lines (format: HH:MM:SS.mmm --> HH:MM:SS.mmm)
        var timestampRegex = new Regex(@"^(\d{2}):(\d{2}):(\d{2})\.(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2})\.(\d{3})$");

        // Skip the first 3 lines (WEBVTT header)
        for (int i = 3; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(line))
                continue;

            // Check if this is a timestamp line
            var match = timestampRegex.Match(line);
            if (match.Success)
            {
                // Parse start time
                var startHours = int.Parse(match.Groups[1].Value);
                var startMinutes = int.Parse(match.Groups[2].Value);
                var startSeconds = int.Parse(match.Groups[3].Value);
                var startMilliseconds = int.Parse(match.Groups[4].Value);
                var startTime = new TimeSpan(0, startHours, startMinutes, startSeconds, startMilliseconds);

                // Parse end time
                var endHours = int.Parse(match.Groups[5].Value);
                var endMinutes = int.Parse(match.Groups[6].Value);
                var endSeconds = int.Parse(match.Groups[7].Value);
                var endMilliseconds = int.Parse(match.Groups[8].Value);
                var endTime = new TimeSpan(0, endHours, endMinutes, endSeconds, endMilliseconds);

                // The next non-empty line should be the text content
                var textLines = new List<string>();
                for (int j = i + 1; j < lines.Length; j++)
                {
                    var textLine = lines[j].Trim();
                    
                    // Stop at empty line or next timestamp
                    if (string.IsNullOrEmpty(textLine) || timestampRegex.IsMatch(textLine))
                    {
                        i = j - 1; // Update outer loop index
                        break;
                    }
                    
                    textLines.Add(textLine);
                }

                // Create segment if we have text
                if (textLines.Count > 0)
                {
                    var text = string.Join(" ", textLines);
                    segments.Add(new GoogleTranscriptSegment
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        Text = text
                    });
                }
            }
        }

        return segments;
    }
}
