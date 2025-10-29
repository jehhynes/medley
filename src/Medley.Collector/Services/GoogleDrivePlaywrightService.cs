using Medley.Collector.Data;
using Medley.Collector.Models;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Medley.Collector.Services;

/// <summary>
/// Service for downloading Google Meet transcripts using HttpClient with Playwright fallback
/// </summary>
public class GoogleDrivePlaywrightService
{
    private readonly ConfigurationService _configurationService;
    private List<BrowserCookie>? _cookies;

    public GoogleDrivePlaywrightService(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Downloads transcript for a Google Drive video, first trying direct HTTP download, then falling back to Playwright
    /// </summary>
    public async Task<MeetingTranscript?> DownloadTranscriptAsync(
        DriveVideo video,
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
        Console.WriteLine($"  Attempting direct download: {captionUrl}");

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
                    // Sanitize the caption content
                    var sanitizedContent = SanitizeCaptionContent(captionContent);
                    
                    transcript = new MeetingTranscript
                    {
                        ExternalId = video.Id,
                        Title = video.Name,
                        Date = video.CreatedTime,
                        Content = sanitizedContent,
                        Source = "GoogleDrive",
                        DownloadedAt = DateTime.UtcNow
                    };

                    Console.WriteLine($"  Captions downloaded successfully via HTTP");
                    Console.WriteLine($"  Caption content size: {sanitizedContent.Length} characters");
                    downloadSuccessful = true;
                }
                else
                {
                    Console.WriteLine($"  No caption content received for video {video.Id}");
                }
            }
            else
            {
                Console.WriteLine($"  Direct download failed with status: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"  HTTP error during direct download: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error during direct download: {ex.Message}");
        }

        // If direct download succeeded, return the transcript
        if (downloadSuccessful && transcript != null)
        {
            return transcript;
        }

        // If direct download failed, fall back to Playwright to trigger caption generation
        Console.WriteLine($"  Direct download failed, falling back to Playwright...");
        return await DownloadTranscriptWithPlaywrightAsync(video, cancellationToken);
    }

    private async Task<MeetingTranscript?> DownloadTranscriptWithPlaywrightAsync(
        DriveVideo video,
        CancellationToken cancellationToken)
    {
        // Launch Playwright browser
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        // Add cookies to context
        var cookies = _cookies!
            .Select(c => new Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = Enum.Parse<SameSiteAttribute>(c.SameSite)
            })
            .ToList();

        if (cookies.Count > 0)
        {
            await context.AddCookiesAsync(cookies);
        }

        var page = await context.NewPageAsync();

        try
        {
            return await ExtractTranscriptFromPageAsync(page, video, cancellationToken);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static void CopyCookiesToHttpClient(List<BrowserCookie> cookies, HttpClient httpClient)
    {
        try
        {
            if (cookies.Any())
            {
                Console.WriteLine($"  Copying {cookies.Count} cookies to HttpClient");

                // Create cookie header string
                var cookieHeader = string.Join("; ", cookies.Select(cookie => $"{cookie.Name}={cookie.Value}"));

                // Add cookies to the HttpClient default headers
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                // Also add User-Agent to match browser
                httpClient.DefaultRequestHeaders.Add("User-Agent", 
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                Console.WriteLine($"  Authentication cookies copied successfully");
            }
            else
            {
                Console.WriteLine($"  No cookies found in browser context");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error copying cookies: {ex.Message}");
        }
    }

    private static string SanitizeCaptionContent(string captionContent)
    {
        var lines = captionContent.Split('\n');
        var contentLines = new List<string>();

        // Skip the first 3 lines (WEBVTT header)
        for (int i = 3; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(line))
                continue;

            // Skip timestamp lines (format: HH:MM:SS.mmm --> HH:MM:SS.mmm)
            if (Regex.IsMatch(line, @"^\d{2}:\d{2}:\d{2}\.\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}\.\d{3}$"))
                continue;

            // This is a content line
            contentLines.Add(line);
        }

        // Join content lines with single spaces, removing extra whitespace
        var sanitizedContent = string.Join(" ", contentLines);

        // Clean up extra whitespace
        sanitizedContent = Regex.Replace(sanitizedContent, @"\s+", " ");
        sanitizedContent = sanitizedContent.Trim();

        return sanitizedContent;
    }

    private async Task<MeetingTranscript?> ExtractTranscriptFromPageAsync(
        IPage page,
        DriveVideo video,
        CancellationToken cancellationToken)
    {
        try
        {
            // Navigate to the video page using the file ID
            var videoUrl = $"https://drive.google.com/file/d/{video.Id}/view";
            await page.GotoAsync(videoUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            // Wait a bit for the page to fully load
            await Task.Delay(2000, cancellationToken);

            // Look for the "Show transcript" button or menu
            // Google Drive video player has a menu button (three dots)
            var menuButton = page.Locator("button[aria-label*='More actions']").Or(
                page.Locator("button[aria-label*='More']"));

            if (await menuButton.CountAsync() > 0)
            {
                await menuButton.First.ClickAsync();
                await Task.Delay(1000, cancellationToken);

                // Look for "Show transcript" or "Open transcript" option
                var transcriptButton = page.Locator("text=/Show transcript|Open transcript/i");

                if (await transcriptButton.CountAsync() > 0)
                {
                    await transcriptButton.First.ClickAsync();
                    await Task.Delay(2000, cancellationToken);

                    // Extract transcript text
                    var transcriptPanel = page.Locator("[role='dialog']").Or(
                        page.Locator(".transcript-panel"));

                    if (await transcriptPanel.CountAsync() > 0)
                    {
                        var transcriptText = await transcriptPanel.First.InnerTextAsync();

                        // Parse the transcript
                        return new MeetingTranscript
                        {
                            ExternalId = video.Id,
                            Title = video.Name,
                            Date = video.CreatedTime,
                            Content = transcriptText,
                            Source = "GoogleDrive",
                            DownloadedAt = DateTime.UtcNow
                        };
                    }
                }
            }

            // Alternative: Check if transcript is embedded in the page
            var embeddedTranscript = page.Locator(".transcript-text").Or(
                page.Locator("[data-transcript]"));

            if (await embeddedTranscript.CountAsync() > 0)
            {
                var transcriptText = await embeddedTranscript.First.InnerTextAsync();

                return new MeetingTranscript
                {
                    ExternalId = video.Id,
                    Title = video.Name,
                    Date = video.CreatedTime,
                    Content = transcriptText,
                    Source = "GoogleDrive"
                };
            }

            return null;
        }
        catch (Exception)
        {
            // If we can't get the transcript, return null
            return null;
        }
    }
}
