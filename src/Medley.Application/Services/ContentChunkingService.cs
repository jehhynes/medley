using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Integrations.Models.Fellow;
using Medley.Application.Interfaces;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using System.Text.Json;

namespace Medley.Application.Services;

/// <summary>
/// Service for intelligently chunking content using AI and speech segments.
/// Groups speech segments based on topical cohesion using LLM analysis.
/// </summary>
public class ContentChunkingService : IContentChunkingService
{
    private readonly IAiProcessingService _aiService;
    private readonly ILogger<ContentChunkingService> _logger;

    // Chunking configuration
    private const int ChunkingThreshold = 90000; // Maximum characters per chunk

    public ContentChunkingService(
        IAiProcessingService aiService,
        ILogger<ContentChunkingService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Chunks content from a source using speech segments from metadata
    /// </summary>
    public async Task<List<ContentChunk>> ChunkContentAsync(Source source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source.Content))
        {
            _logger.LogWarning("Source {SourceId} has no content to chunk", source.Id);
            return new List<ContentChunk>();
        }

        // Extract speech segments from source metadata
        var segments = ExtractSpeechSegments(source);

        if (segments == null || segments.Count == 0)
        {
            _logger.LogError("Source {SourceId} has no speech segments in metadata. Cannot chunk content.", source.Id);
            throw new InvalidOperationException($"Source {source.Id} does not contain speech segments required for chunking.");
        }

        _logger.LogInformation("Found {Count} speech segments in source metadata. Using segment-based chunking.", segments.Count);
        return await GetSegmentBasedChunksAsync(segments, cancellationToken);
    }

    private List<SpeechSegment>? ExtractSpeechSegments(Source source)
    {
        if (string.IsNullOrWhiteSpace(source.MetadataJson))
        {
            return null;
        }

        try
        {
            // Try parsing as Fellow recording
            var fellowRecording = JsonSerializer.Deserialize<FellowRecordingImportModel>(source.MetadataJson);
            if (fellowRecording?.Transcript?.SpeechSegments != null && fellowRecording.Transcript.SpeechSegments.Count > 0)
            {
                return CombineFellowSegmentsBySpeaker(fellowRecording.Transcript.SpeechSegments);
            }

            // Try parsing as Google Drive video
            var googleVideo = JsonSerializer.Deserialize<GoogleDriveVideoImportModel>(source.MetadataJson);
            if (googleVideo?.Transcript != null && googleVideo.Transcript.Count > 0)
            {
                // Google Drive transcripts don't break at sentence endings, so we need to re-segment
                return SplitGoogleSegmentsBySentence(googleVideo.Transcript);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse speech segments from source metadata");
        }

        return null;
    }

    private List<SpeechSegment> CombineFellowSegmentsBySpeaker(List<FellowSpeechSegmentImportModel> fellowSegments)
    {
        var combinedSegments = new List<SpeechSegment>();
        const int maxCharactersPerSegment = 500;

        string? currentSpeaker = null;
        var accumulatedText = new System.Text.StringBuilder();

        foreach(var segment in fellowSegments)
        {
            var text = segment.Text ?? string.Empty;
            var speaker = segment.Speaker;

            // Check if we should start a new segment
            var speakerChanged = currentSpeaker != null && currentSpeaker != speaker;
            var wouldExceedLimit = accumulatedText.Length + text.Length > maxCharactersPerSegment;

            if (speakerChanged || (wouldExceedLimit && accumulatedText.Length > 0))
            {
                // Save the accumulated segment
                var accumulatedString = accumulatedText.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(accumulatedString))
                {
                    combinedSegments.Add(new SpeechSegment
                    {
                        Index = combinedSegments.Count,
                        Speaker = currentSpeaker,
                        Text = accumulatedString,
                        CharacterCount = accumulatedString.Length
                    });
                }
                
                // Reset accumulator
                accumulatedText.Clear();
                currentSpeaker = speaker;
            }
            else if (currentSpeaker == null)
            {
                // First segment
                currentSpeaker = speaker;
            }

            // Add current segment text to accumulator
            if (accumulatedText.Length > 0 && !string.IsNullOrWhiteSpace(text))
            {
                accumulatedText.Append(' '); // Add space between segments
            }
            accumulatedText.Append(text);
        }

        // Handle any remaining accumulated text
        if (accumulatedText.Length > 0)
        {
            var remaining = accumulatedText.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                combinedSegments.Add(new SpeechSegment
                {
                    Index = combinedSegments.Count,
                    Speaker = currentSpeaker,
                    Text = remaining,
                    CharacterCount = remaining.Length
                });
            }
        }

        _logger.LogInformation("Combined {OriginalCount} Fellow segments into {CombinedCount} speaker-grouped segments",
            fellowSegments.Count, combinedSegments.Count);

        return combinedSegments;
    }

    private List<SpeechSegment> SplitGoogleSegmentsBySentence(List<GoogleTranscriptSegmentImportModel> googleSegments)
    {
        // Concatenate all segments into a single text
        var fullText = string.Join(" ", googleSegments.Select(s => s.Text ?? string.Empty));

        // Use TextChunker to split into lines based on sentence boundaries
        // 1 token ≈ 4 characters, so ~50 tokens = ~200 chars ≈ 1-2 sentences
        const int maxTokensPerLine = 50; // Target ~1-2 sentences per segment
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates
        var lines = TextChunker.SplitPlainTextLines(fullText, maxTokensPerLine);
#pragma warning restore SKEXP0050
        
        // Convert lines into SpeechSegment objects
        var segments = lines.Select((line, idx) => new SpeechSegment
        {
            Index = idx,
            Speaker = null,
            Text = line,
            CharacterCount = line.Length
        }).ToList();

        _logger.LogInformation("Split {OriginalCount} Google Drive segments into {LineCount} text-chunked segments",
            googleSegments.Count, segments.Count);

        return segments;
    }

    private async Task<List<ContentChunk>> GetSegmentBasedChunksAsync(
        List<SpeechSegment> segments,
        CancellationToken cancellationToken = default)
    {
        // Calculate desired number of chunks
        var totalCharacters = segments.Sum(s => s.Text.Length);
        var desiredChunkCount = Math.Max(2, (int)Math.Ceiling(totalCharacters / (double)ChunkingThreshold));

        _logger.LogInformation("Total segments: {SegmentCount}, Total characters: {TotalChars}, Target chunks: {ChunkCount}",
            segments.Count, totalCharacters, desiredChunkCount);

        // Create a compact pipe-delimited summary of segments for the LLM
        // Format (with speakers): Index|Speaker|Text|CharacterCount
        // Format (no speakers): Index|Text|CharacterCount
        var includeSpeaker = segments.Any(s => !string.IsNullOrWhiteSpace(s.Speaker));
        var segmentLines = new System.Text.StringBuilder();
        segmentLines.AppendLine(includeSpeaker
            ? "Index|Speaker|Text|CharacterCount"
            : "Index|Text|CharacterCount");

        foreach (var s in segments)
        {
            // Replace newlines and pipes to keep the format intact
            var speaker = (s.Speaker ?? string.Empty).Replace("|", " ").Replace("\n", " ").Replace("\r", " ");
            var text = s.Text.Replace("|", " ").Replace("\n", " ").Replace("\r", " ");

            if (includeSpeaker)
            {
                segmentLines.AppendLine($"{s.Index}|{speaker}|{text}|{s.CharacterCount}");
            }
            else
            {
                segmentLines.AppendLine($"{s.Index}|{text}|{s.CharacterCount}");
            }
        }

        var segmentSummaryData = segmentLines.ToString().TrimEnd();

        var dataFormatNote = includeSpeaker
            ? "- Data format (pipe-delimited): Index|Speaker|Text|CharacterCount"
            : "- Data format (pipe-delimited): Index|Text|CharacterCount";

        var chunkingSystemPrompt = $@"You are a content chunking specialist. Analyze the provided speech segments and group them into approximately {desiredChunkCount} chunks.

Your goal is to maximize topical cohesion within each chunk - keep related content together and split at natural topic boundaries.

Requirements:
- Target approximately {desiredChunkCount} chunks
- Each chunk should contain segments that total no more than 80000 characters
- Group segments by topic/theme - keep related discussions together
- Provide only the starting segment index (from the Index field) for each chunk
- Chunks are implicitly defined: each chunk starts at its StartIndex and ends at the next chunk's StartIndex - 1
- The first chunk should start at index 0
- Include a brief summary of each chunk's topic(s)
- {dataFormatNote}

Example: If you return StartIndex values of 0, 15, 28, this means:
- Chunk 1: segments 0-14
- Chunk 2: segments 15-27
- Chunk 3: segments 28 to the end";

        SegmentChunkingResponse? chunkingResponse;
        try
        {
            chunkingResponse = await _aiService.ProcessStructuredPromptAsync<SegmentChunkingResponse>(
                userPrompt: segmentSummaryData,
                systemPrompt: chunkingSystemPrompt,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get segment-based chunking from LLM. Falling back to simple chunking.");
            chunkingResponse = CreateFallbackSegmentChunks(segments, desiredChunkCount);
        }

        if (chunkingResponse == null || chunkingResponse.Chunks == null || chunkingResponse.Chunks.Count == 0)
        {
            _logger.LogWarning("LLM returned no segment chunks. Using fallback chunking.");
            chunkingResponse = CreateFallbackSegmentChunks(segments, desiredChunkCount);
        }

        _logger.LogInformation("Grouped segments into {ChunkCount} chunks", chunkingResponse.Chunks.Count);
        if (!string.IsNullOrWhiteSpace(chunkingResponse.Message))
        {
            _logger.LogInformation("Chunking strategy: {Message}", chunkingResponse.Message);
        }

        // Sort chunks by start index to ensure proper ordering
        var sortedChunks = chunkingResponse.Chunks.OrderBy(c => c.StartIndex).ToList();

        // Build content chunks from segment ranges
        var contentChunks = new List<ContentChunk>();
        for (int i = 0; i < sortedChunks.Count; i++)
        {
            var chunk = sortedChunks[i];
            var startIdx = chunk.StartIndex;
            var endIdx = (i < sortedChunks.Count - 1) ? sortedChunks[i + 1].StartIndex : segments.Count;

            // Validate indices
            if (startIdx < 0 || startIdx >= segments.Count)
            {
                _logger.LogWarning("Chunk {ChunkIndex} has invalid start index {StartIdx}, skipping", i, startIdx);
                continue;
            }

            if (endIdx <= startIdx)
            {
                _logger.LogWarning("Chunk {ChunkIndex} has invalid range ({StartIdx}, {EndIdx}), skipping", i, startIdx, endIdx);
                continue;
            }

            // Get segments in this range
            var chunkSegments = segments
                .Where(s => s.Index >= startIdx && s.Index < endIdx)
                .OrderBy(s => s.Index)
                .ToList();

            if (chunkSegments.Count == 0)
            {
                _logger.LogWarning("Chunk {ChunkIndex} has no segments in range ({StartIdx}, {EndIdx}), skipping", i, startIdx, endIdx);
                continue;
            }

            // Combine segment text with speaker labels
            var contentBuilder = new System.Text.StringBuilder();
            foreach (var segment in chunkSegments)
            {
                if (!string.IsNullOrWhiteSpace(segment.Speaker))
                {
                    contentBuilder.AppendLine($"[{segment.Speaker}] {segment.Text}");
                }
                else
                {
                    contentBuilder.AppendLine(segment.Text);
                }
                contentBuilder.AppendLine(); // Add spacing between segments
            }

            contentChunks.Add(new ContentChunk
            {
                Index = i,
                Content = contentBuilder.ToString().Trim(),
                Summary = chunk.Summary
            });

            _logger.LogDebug("Chunk {ChunkIndex}: segments [{StartIdx}-{EndIdx}), {SegmentCount} segments, {CharCount} characters",
                i, startIdx, endIdx - 1, chunkSegments.Count, contentBuilder.Length);
        }

        return contentChunks;
    }

    private SegmentChunkingResponse CreateFallbackSegmentChunks(List<SpeechSegment> segments, int desiredChunkCount)
    {
        var chunks = new List<SegmentChunk>();
        var segmentsPerChunk = Math.Max(1, segments.Count / desiredChunkCount);

        for (int i = 0; i < desiredChunkCount; i++)
        {
            var startIdx = i * segmentsPerChunk;
            
            chunks.Add(new SegmentChunk
            {
                StartIndex = startIdx,
                Summary = $"Chunk starting at segment {startIdx}"
            });
        }

        return new SegmentChunkingResponse
        {
            Message = "Using fallback equal-sized segment chunking",
            Chunks = chunks
        };
    }
}

