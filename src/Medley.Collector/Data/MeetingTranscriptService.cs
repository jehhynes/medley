using Microsoft.EntityFrameworkCore;

namespace Medley.Collector.Data;

public class MeetingTranscriptService
{
    public async Task<List<MeetingTranscript>> GetAllTranscriptsAsync(bool isArchived = false)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .Include(t => t.ApiKeys)
            .Where(t => t.IsArchived == isArchived)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
    
    public async Task<bool> TranscriptExistsForApiKeyAsync(string meetingId, int apiKeyId)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .Include(t => t.ApiKeys)
            .AnyAsync(t => t.ExternalId == meetingId && t.ApiKeys.Any(a => a.Id == apiKeyId));
    }
    
    public async Task SaveTranscriptAsync(MeetingTranscript transcript, ApiKey apiKey)
    {
        using var context = new AppDbContext();
        
        // Check if meeting already exists (by MeetingId only)
        var existing = await context.MeetingTranscripts
            .Include(t => t.ApiKeys)
            .FirstOrDefaultAsync(t => t.ExternalId == transcript.ExternalId);
        
        if (existing != null)
        {
            // Meeting exists - check if this API key is already associated
            if (!existing.ApiKeys.Any(a => a.Id == apiKey.Id))
            {
                // Add the API key to the existing meeting
                var trackedApiKey = await context.ApiKeys.FindAsync(apiKey.Id);
                if (trackedApiKey != null)
                {
                    existing.ApiKeys.Add(trackedApiKey);
                }
            }
        }
        else
        {
            // New meeting - add it with the API key
            var trackedApiKey = await context.ApiKeys.FindAsync(apiKey.Id);
            if (trackedApiKey != null)
            {
                transcript.ApiKeys.Add(trackedApiKey);
            }
            context.MeetingTranscripts.Add(transcript);
        }
        
        await context.SaveChangesAsync();
    }
    
    public async Task<int> GetTranscriptCountAsync(bool isArchived = false)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .Where(t => t.IsArchived == isArchived)
            .CountAsync();
    }
    
    public async Task<int> DeleteAllTranscriptsAsync()
    {
        using var context = new AppDbContext();
        var count = await context.MeetingTranscripts.CountAsync();
        context.MeetingTranscripts.RemoveRange(context.MeetingTranscripts);
        await context.SaveChangesAsync();
        return count;
    }
    
    public async Task UpdateTranscriptSelectionAsync(int transcriptId, bool? isSelected)
    {
        using var context = new AppDbContext();
        var transcript = await context.MeetingTranscripts.FindAsync(transcriptId);
        if (transcript != null)
        {
            transcript.IsSelected = isSelected;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<MeetingTranscript>> GetSelectedTranscriptsAsync(bool isArchived = false)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .Where(t => t.IsSelected == true && t.IsArchived == isArchived)
            .OrderBy(t => t.Title)
            .ToListAsync();
    }
    
    public async Task<bool> HasUndecidedTranscriptsAsync(bool isArchived = false)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .Where(t => t.IsArchived == isArchived)
            .AnyAsync(t => t.IsSelected == null);
    }
    
    public async Task<MeetingTranscript?> GetTranscriptByIdAsync(int transcriptId)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .Include(t => t.ApiKeys)
            .FirstOrDefaultAsync(t => t.Id == transcriptId);
    }

    public async Task<MeetingTranscript?> GetTranscriptByExternalIdAsync(string externalId, TranscriptSource source)
    {
        using var context = new AppDbContext();
        return await context.MeetingTranscripts
            .FirstOrDefaultAsync(t => t.ExternalId == externalId && t.Source == source);
    }

    public async Task CreateTranscriptAsync(MeetingTranscript transcript)
    {
        using var context = new AppDbContext();
        context.MeetingTranscripts.Add(transcript);
        await context.SaveChangesAsync();
    }

    public async Task ArchiveTranscriptsByIdsAsync(List<int> ids)
    {
        using var context = new AppDbContext();
        var transcripts = await context.MeetingTranscripts
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
        
        foreach (var transcript in transcripts)
        {
            transcript.IsArchived = true;
        }
        
        await context.SaveChangesAsync();
    }

    public async Task<int> ArchiveExcludedAsync()
    {
        using var context = new AppDbContext();
        var transcripts = await context.MeetingTranscripts
            .Where(t => t.IsSelected == false && t.IsArchived == false)
            .ToListAsync();
        
        foreach (var transcript in transcripts)
        {
            transcript.IsArchived = true;
        }
        
        await context.SaveChangesAsync();
        return transcripts.Count;
    }

    public async Task<int> ArchiveExportedAsync()
    {
        using var context = new AppDbContext();
        var transcripts = await context.MeetingTranscripts
            .Where(t => t.ExportedAt != null && t.IsArchived == false)
            .ToListAsync();
        
        foreach (var transcript in transcripts)
        {
            transcript.IsArchived = true;
        }
        
        await context.SaveChangesAsync();
        return transcripts.Count;
    }

    public async Task RestoreTranscriptsByIdsAsync(List<int> ids)
    {
        using var context = new AppDbContext();
        var transcripts = await context.MeetingTranscripts
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
        
        foreach (var transcript in transcripts)
        {
            transcript.IsArchived = false;
        }
        
        await context.SaveChangesAsync();
    }

    public async Task<int> RestoreAllArchivedAsync()
    {
        using var context = new AppDbContext();
        var transcripts = await context.MeetingTranscripts
            .Where(t => t.IsArchived == true)
            .ToListAsync();
        
        foreach (var transcript in transcripts)
        {
            transcript.IsArchived = false;
        }
        
        await context.SaveChangesAsync();
        return transcripts.Count;
    }

    public async Task MarkTranscriptsAsExportedAsync(List<int> ids, DateTime exportedAt)
    {
        using var context = new AppDbContext();
        var transcripts = await context.MeetingTranscripts
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
        
        foreach (var transcript in transcripts)
        {
            transcript.ExportedAt = exportedAt;
        }
        
        await context.SaveChangesAsync();
    }
}
