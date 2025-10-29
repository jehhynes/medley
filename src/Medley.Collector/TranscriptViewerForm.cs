using Medley.Collector.Data;
using Medley.Collector.Models;
using System.Text.Json;
using Markdig;

namespace Medley.Collector;

public partial class TranscriptViewerForm : Form
{
    public TranscriptViewerForm(MeetingTranscript transcript)
    {
        InitializeComponent();
        
        // Deserialize content once
        object? deserializedContent = null;
        if (transcript.Source == TranscriptSource.Google)
        {
            deserializedContent = JsonSerializer.Deserialize<GoogleDriveVideo>(transcript.Content);
        }
        else
        {
            deserializedContent = JsonSerializer.Deserialize<FellowRecording>(transcript.Content);
        }
        
        LoadTranscript(transcript, deserializedContent);
        LoadNote(transcript, deserializedContent);
        
        // Prevent text from being selected on load
        textBoxTranscript.Select(0, 0);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Close form when Escape is pressed
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void LoadTranscript(MeetingTranscript transcript, object? deserializedContent)
    {
        Text = $"Transcript - {transcript.Title}";
        
        try
        {
            if (transcript.Source == TranscriptSource.Google)
            {
                var driveVideo = deserializedContent as GoogleDriveVideo;
                
                if (driveVideo?.Transcript != null && driveVideo.Transcript.Count > 0)
                {
                    var consolidatedTranscript = ConsolidateGoogleTranscript(driveVideo.Transcript);
                    textBoxTranscript.Text = consolidatedTranscript;
                }
                else
                {
                    textBoxTranscript.Text = "No transcript available.";
                }
            }
            else
            {
                var recording = deserializedContent as FellowRecording;
                
                if (recording?.Transcript?.SpeechSegments != null && recording.Transcript.SpeechSegments.Count > 0)
                {
                    var consolidatedTranscript = ConsolidateTranscriptBySpeaker(recording.Transcript.SpeechSegments);
                    textBoxTranscript.Text = consolidatedTranscript;
                }
                else
                {
                    textBoxTranscript.Text = "No transcript available.";
                }
            }
        }
        catch (Exception ex)
        {
            textBoxTranscript.Text = $"Error loading transcript: {ex.Message}";
        }
    }

    /// <summary>
    /// Consolidates transcript segments by speaker, combining consecutive segments from the same speaker
    /// </summary>
    private static string ConsolidateTranscriptBySpeaker(List<FellowSpeechSegment> segments)
    {
        if (segments == null || segments.Count == 0)
            return string.Empty;

        var result = new System.Text.StringBuilder();
        string? currentSpeaker = null;
        var currentTexts = new List<string>();

        foreach (var segment in segments)
        {
            if (segment.Speaker != currentSpeaker)
            {
                // Write out the previous speaker's consolidated text
                if (currentSpeaker != null && currentTexts.Count > 0)
                {
                    result.AppendLine($"{currentSpeaker}: {string.Join(" ", currentTexts)}");
                    result.AppendLine(); // Add blank line between speakers
                }

                // Start new speaker
                currentSpeaker = segment.Speaker;
                currentTexts.Clear();
            }

            // Add text to current speaker's segments
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                currentTexts.Add(segment.Text.Trim());
            }
        }

        // Write out the last speaker's text
        if (currentSpeaker != null && currentTexts.Count > 0)
        {
            result.AppendLine($"{currentSpeaker}: {string.Join(" ", currentTexts)}");
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// Consolidates Google Drive transcript segments into a readable format
    /// </summary>
    private static string ConsolidateGoogleTranscript(List<GoogleTranscriptSegment> segments)
    {
        if (segments == null || segments.Count == 0)
            return string.Empty;

        var result = new System.Text.StringBuilder();

        foreach (var segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                // Format: [HH:MM:SS] Text
                result.AppendLine($"[{segment.StartTime:hh\\:mm\\:ss}] {segment.Text.Trim()}");
            }
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// Loads the note content if this is a Fellow meeting with a note
    /// </summary>
    private void LoadNote(MeetingTranscript transcript, object? deserializedContent)
    {
        try
        {
            // Only show Note tab for Fellow meetings
            if (transcript.Source != TranscriptSource.Fellow)
            {
                tabControl.TabPages.Remove(tabPageNote);
                return;
            }

            var recording = deserializedContent as FellowRecording;
            
            if (recording?.Note == null || string.IsNullOrWhiteSpace(recording.Note.ContentMarkdown))
            {
                tabControl.TabPages.Remove(tabPageNote);
                return;
            }

            // Display attendee emails
            if (recording.Note.EventAttendees != null && recording.Note.EventAttendees.Count > 0)
            {
                var attendeeEmails = string.Join(", ", recording.Note.EventAttendees
                    .Where(a => !string.IsNullOrWhiteSpace(a.Email))
                    .Select(a => a.Email));
                
                textBoxAttendees.Text = $"Attendees: {attendeeEmails}";
            }
            else
            {
                textBoxAttendees.Text = "Attendees: None";
            }

            // Convert markdown to HTML and display in WebBrowser
            var html = ConvertMarkdownToHtml(recording.Note.ContentMarkdown);
            webBrowserNote.DocumentText = html;
        }
        catch (Exception ex)
        {
            // If there's an error loading the note, just hide the Note tab
            tabControl.TabPages.Remove(tabPageNote);
        }
    }

    /// <summary>
    /// Converts markdown to HTML with basic styling
    /// </summary>
    private static string ConvertMarkdownToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        
        var htmlBody = Markdown.ToHtml(markdown, pipeline);
        
        // Wrap in a complete HTML document with styling matching Fellow's interface
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.7;
            color: #1f2937;
            background-color: #ffffff;
            font-size: 15px;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 32px;
            margin-bottom: 8px;
            font-weight: 700;
            line-height: 1.3;
            color: #111827;
            letter-spacing: -0.01em;
        }}
        h1 {{ 
            font-size: 1.5em; 
            margin-top: 0;
            margin-bottom: 4px;
        }}
        h2 {{ 
            font-size: 1.3em; 
            margin-top: 32px;
            margin-bottom: 6px;
        }}
        h3 {{ 
            font-size: 1.15em;
            font-weight: 600;
            margin-top: 24px;
        }}
        h4, h5, h6 {{
            font-size: 1em;
            font-weight: 600;
            margin-top: 20px;
        }}
        p {{ 
            margin-bottom: 16px;
            margin-top: 0;
        }}
        ul, ol {{ 
            padding-left: 28px;
            margin: 8px 0 16px 0;
        }}
        li {{ 
            margin-bottom: 6px;
            line-height: 1.7;
            padding-left: 4px;
        }}
        li p {{
            margin-bottom: 8px;
        }}
        ul ul, ol ul, ul ol, ol ol {{
            margin-top: 4px;
            margin-bottom: 4px;
        }}
        li::marker {{
            color: #6b7280;
        }}
        code {{
            background-color: #f3f4f6;
            padding: 2px 6px;
            border-radius: 4px;
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 0.9em;
            color: #dc2626;
            border: 1px solid #e5e7eb;
        }}
        pre {{
            background-color: #f9fafb;
            padding: 16px;
            border-radius: 6px;
            overflow-x: auto;
            margin: 16px 0;
            border: 1px solid #e5e7eb;
            line-height: 1.6;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
            color: #1f2937;
            border: none;
        }}
        blockquote {{
            border-left: 3px solid #d1d5db;
            padding-left: 20px;
            margin: 16px 0;
            color: #6b7280;
            font-style: italic;
        }}
        blockquote p {{
            margin-bottom: 8px;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 20px 0;
            font-size: 14px;
        }}
        th, td {{
            border: 1px solid #d1d5db;
            padding: 10px 14px;
            text-align: left;
        }}
        th {{
            background-color: #f9fafb;
            font-weight: 600;
            color: #111827;
        }}
        tr:hover {{
            background-color: #f9fafb;
        }}
        a {{
            color: #2563eb;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        hr {{
            border: none;
            border-top: 1px solid #e5e7eb;
            margin: 32px 0;
        }}
        strong {{
            font-weight: 600;
            color: #111827;
        }}
        em {{
            font-style: italic;
        }}
        /* Task lists / checkboxes */
        input[type=""checkbox""] {{
            margin-right: 8px;
        }}
    </style>
</head>
<body>
{htmlBody}
</body>
</html>";
    }
}
