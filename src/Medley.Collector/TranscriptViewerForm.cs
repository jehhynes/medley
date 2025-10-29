using Medley.Collector.Data;
using Medley.Collector.Models;
using System.Text.Json;

namespace Medley.Collector;

public partial class TranscriptViewerForm : Form
{
    public TranscriptViewerForm(MeetingTranscript transcript)
    {
        InitializeComponent();
        LoadTranscript(transcript);
        
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

    private void LoadTranscript(MeetingTranscript transcript)
    {
        Text = $"Transcript - {transcript.Title}";
        
        try
        {
            if (transcript.Source == TranscriptSource.Google)
            {
                // Deserialize the DriveVideo object
                var driveVideo = JsonSerializer.Deserialize<GoogleDriveVideo>(transcript.Content);
                
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
                // Deserialize the FullJson to get the recording (Fellow)
                var recording = JsonSerializer.Deserialize<FellowRecording>(transcript.Content);
                
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
}
