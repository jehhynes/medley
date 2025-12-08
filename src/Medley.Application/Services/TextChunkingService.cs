using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Services;

/// <summary>
/// Service for chunking large text content into smaller pieces for AI processing
/// Uses simple character-based chunking with paragraph/sentence boundary detection
/// Prioritizes paragraph boundaries, then falls back to sentence boundaries
/// </summary>
public class TextChunkingService
{
    private readonly ILogger<TextChunkingService> _logger;

    // Maximum characters per chunk
    private const int MaxCharsPerChunk = 50000;

    // Overlap between chunks to preserve context at boundaries
    private const int ChunkOverlapChars = 6000;

    public TextChunkingService(ILogger<TextChunkingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Chunks text content into smaller pieces suitable for AI processing
    /// </summary>
    /// <param name="content">The content to chunk</param>
    /// <param name="maxChunkSizeChars">Optional maximum characters per chunk (defaults to MaxCharsPerChunk)</param>
    /// <returns>List of text chunks</returns>
    public Task<List<string>> ChunkTextAsync(string content, int? maxChunkSizeChars = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new List<string>());
        }

        var maxChunkSize = maxChunkSizeChars ?? MaxCharsPerChunk;
        var overlapChars = ChunkOverlapChars;

        // If content fits in one chunk, return as-is
        if (content.Length <= maxChunkSize)
        {
            _logger.LogDebug("Content fits in single chunk ({Length} characters)", content.Length);
            return Task.FromResult(new List<string> { content });
        }

        // Step 1: Determine the number of chunks to return
        var numChunks = CalculateNumberOfChunks(content.Length, maxChunkSize, overlapChars);
        _logger.LogDebug("Calculated {NumChunks} chunks for content of {Length} characters", numChunks, content.Length);

        // Step 2: Calculate rough start/end positions for each chunk
        var chunkPositions = CalculateRoughChunkPositions(content.Length, numChunks, maxChunkSize, overlapChars);
        
        // Step 3: Adjust start/end positions by looking for paragraph/sentence boundaries
        var adjustedPositions = AdjustChunkBoundaries(content, chunkPositions, maxChunkSize);

        // Extract chunks based on adjusted positions
        var chunks = new List<string>();
        for (int i = 0; i < adjustedPositions.Count; i++)
        {
            var (startIndex, endIndex) = adjustedPositions[i];
            var chunk = content.Substring(startIndex, endIndex - startIndex).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
                _logger.LogDebug("Created chunk {ChunkNumber}: {Length} characters (indices {StartIndex}-{EndIndex})",
                    i + 1, chunk.Length, startIndex, endIndex);
            }
        }

        _logger.LogInformation("Chunked content into {ChunkCount} chunks (original length: {OriginalLength} characters)",
            chunks.Count, content.Length);

        return Task.FromResult(chunks);
    }

    /// <summary>
    /// Step 1: Calculate the number of chunks needed based on content length and chunk size
    /// </summary>
    private int CalculateNumberOfChunks(int contentLength, int maxChunkSize, int overlapChars)
    {
        if (contentLength <= maxChunkSize)
        {
            return 1;
        }

        // Calculate how many chunks we need, accounting for overlap
        // Each chunk after the first overlaps with the previous chunk
        var effectiveChunkSize = maxChunkSize - overlapChars;
        var numChunks = (int)Math.Ceiling((double)(contentLength - maxChunkSize) / effectiveChunkSize) + 1;
        
        return numChunks;
    }

    /// <summary>
    /// Step 2: Calculate rough start/end positions for each chunk
    /// </summary>
    private List<(int Start, int End)> CalculateRoughChunkPositions(int contentLength, int numChunks, int maxChunkSize, int overlapChars)
    {
        var positions = new List<(int Start, int End)>();

        if (numChunks == 1)
        {
            positions.Add((0, contentLength));
            return positions;
        }

        var effectiveChunkSize = maxChunkSize - overlapChars;
        
        for (int i = 0; i < numChunks; i++)
        {
            int startIndex;
            int endIndex;

            if (i == 0)
            {
                // First chunk starts at 0
                startIndex = 0;
                endIndex = Math.Min(maxChunkSize, contentLength);
            }
            else if (i == numChunks - 1)
            {
                // Last chunk ends at contentLength
                startIndex = Math.Max(0, contentLength - maxChunkSize);
                endIndex = contentLength;
            }
            else
            {
                // Middle chunks have overlap
                startIndex = i * effectiveChunkSize;
                endIndex = Math.Min(startIndex + maxChunkSize, contentLength);
            }

            positions.Add((startIndex, endIndex));
        }

        return positions;
    }

    /// <summary>
    /// Step 3: Adjust chunk boundaries to align with paragraph/sentence boundaries
    /// </summary>
    private List<(int Start, int End)> AdjustChunkBoundaries(string content, List<(int Start, int End)> roughPositions, int maxChunkSize)
    {
        var adjustedPositions = new List<(int Start, int End)>();

        for (int i = 0; i < roughPositions.Count; i++)
        {
            var (roughStart, roughEnd) = roughPositions[i];
            var adjustedStart = roughStart;
            var adjustedEnd = roughEnd;

            // Adjust start position (except for first chunk)
            if (i > 0)
            {
                // Look for paragraph/sentence boundary near the start
                var searchStart = Math.Max(0, roughStart - (maxChunkSize / 5));
                var paragraphStart = FindParagraphBoundary(content, searchStart, roughStart);
                if (paragraphStart >= 0 && paragraphStart >= searchStart)
                {
                    adjustedStart = paragraphStart;
                }
                else
                {
                    var sentenceStart = FindSentenceBoundary(content, searchStart, roughStart);
                    if (sentenceStart >= 0 && sentenceStart >= searchStart)
                    {
                        adjustedStart = sentenceStart;
                    }
                }
            }

            // Adjust end position (except for last chunk)
            if (i < roughPositions.Count - 1)
            {
                // Look for paragraph/sentence boundary near the end
                var searchStart = Math.Max(adjustedStart, roughEnd - (maxChunkSize / 5));
                var paragraphEnd = FindParagraphBoundary(content, searchStart, roughEnd);
                if (paragraphEnd >= 0 && paragraphEnd > adjustedStart && paragraphEnd <= roughEnd)
                {
                    adjustedEnd = paragraphEnd;
                }
                else
                {
                    var sentenceEnd = FindSentenceBoundary(content, searchStart, roughEnd);
                    if (sentenceEnd >= 0 && sentenceEnd > adjustedStart && sentenceEnd <= roughEnd)
                    {
                        adjustedEnd = sentenceEnd;
                    }
                }
            }

            // Ensure we don't create invalid ranges
            if (adjustedEnd > adjustedStart)
            {
                adjustedPositions.Add((adjustedStart, adjustedEnd));
            }
        }

        return adjustedPositions;
    }

    /// <summary>
    /// Finds a sentence boundary (period, exclamation, question mark followed by space/newline)
    /// </summary>
    private int FindSentenceBoundary(string text, int startIndex, int maxIndex)
    {
        for (int i = maxIndex - 1; i >= startIndex; i--)
        {
            if (i < text.Length - 1 && IsSentenceEnding(text[i]))
            {
                // Check if followed by whitespace or end of string
                if (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1]))
                {
                    return i + 1;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Finds a paragraph boundary (double newline or significant whitespace)
    /// </summary>
    private int FindParagraphBoundary(string text, int startIndex, int maxIndex)
    {
        for (int i = maxIndex - 1; i >= startIndex; i--)
        {
            if (i < text.Length - 1 && text[i] == '\n')
            {
                // Check for double newline (paragraph break)
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    return i + 2;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if a character is a sentence ending punctuation
    /// </summary>
    private bool IsSentenceEnding(char c)
    {
        return c == '.' || c == '!' || c == '?';
    }
}
