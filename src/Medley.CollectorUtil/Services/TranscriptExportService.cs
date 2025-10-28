using Medley.CollectorUtil.Data;
using System.IO.Compression;
using System.Text.Json;

namespace Medley.CollectorUtil.Services;

public class TranscriptExportService
{
    public async Task ExportTranscriptsToZipAsync(List<MeetingTranscript> transcripts, string zipFilePath)
    {
        await Task.Run(() =>
        {
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var transcript in transcripts)
                {
                    if (string.IsNullOrWhiteSpace(transcript.FullJson))
                        continue;

                    var safeTitle = string.IsNullOrWhiteSpace(transcript.Title)
                        ? "untitled"
                        : string.Join("_", transcript.Title.Split(Path.GetInvalidFileNameChars()));

                    var fileName = $"{transcript.MeetingId}_{safeTitle}.json";

                    if (fileName.Length > 200)
                    {
                        fileName = fileName.Substring(0, 200) + ".json";
                    }

                    var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);

                    using (var entryStream = entry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        writer.Write(transcript.FullJson);
                    }
                }
            }
        });
    }
}
