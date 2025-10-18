# Tech Spec: Epic 2 - Fellow.ai Integration & Fragment Processing

**Epic:** Fellow.ai Integration & Fragment Processing  
**Date:** 2025-10-17  
**Author:** Medley Developer  
**Architecture Reference:** [Solution Architecture](./solution-architecture.md)

## Epic Overview

Implement comprehensive Fellow.ai integration and AI-powered fragment extraction capabilities to transform meeting data into structured, actionable intelligence.

**Value Delivery:** Core data ingestion pipeline that captures organizational conversations and identifies valuable insights through Claude 4.5 processing.

## Stories Included

- Story 2.1: Integration Management Interface
- Story 2.2: API Key Authentication Framework
- Story 2.3: Fellow.ai API Connection
- Story 2.4: Fellow.ai Meeting Data Ingestion
- Story 2.5: GitHub API Connection
- Story 2.6: GitHub Data Ingestion
- Story 2.7: Claude 4.5 Integration via AWS Bedrock
- Story 2.8: Fragment Extraction Prompts and Processing
- Story 2.9: Fragment Processing Engine
- Story 2.10: Fragment Storage and Indexing
- Story 2.11: Fragment Search Interface

## Architecture Extract

### Technology Stack (Epic 2 Specific)

| Component | Technology | Interface | Justification |
|-----------|------------|-----------|---------------|
| AI Processing | AWS Bedrock (Claude 4.5) | `IAiProcessingService` | Advanced reasoning, large context window |
| Meeting Data | Fellow.ai API v1 | `IMeetingDataService` | Primary meeting transcript source |
| Code Data | GitHub API v4 | `ICodeDataService` | Development activity tracking |
| Background Jobs | ASP.NET Hosted Services | `IBackgroundJobService` | Async AI processing |
| Search | PostgreSQL Full-Text | `ISearchService` | Built-in search capabilities |

### Data Models (Epic 2)

**Extended Entities:**
```csharp
// Meeting and Code Data
public class MeetingTranscript : BaseEntity
{
    public string ExternalId { get; set; } // Fellow.ai meeting ID
    public string Title { get; set; }
    public DateTime MeetingDate { get; set; }
    public TimeSpan Duration { get; set; }
    public string Participants { get; set; } // JSON array
    public string Transcript { get; set; }
    public string ActionItems { get; set; } // JSON array
    public string Agenda { get; set; }
    public ProcessingStatus Status { get; set; }
    public int SourceId { get; set; }
    public Source Source { get; set; }
    public ICollection<Fragment> Fragments { get; set; }
}

public class CodeActivity : BaseEntity
{
    public string ExternalId { get; set; } // GitHub commit/PR/issue ID
    public CodeActivityType Type { get; set; } // Commit, PR, Issue
    public string Title { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public DateTime ActivityDate { get; set; }
    public string Repository { get; set; }
    public string Branch { get; set; }
    public string Metadata { get; set; } // JSON metadata
    public ProcessingStatus Status { get; set; }
    public int SourceId { get; set; }
    public Source Source { get; set; }
    public ICollection<Fragment> Fragments { get; set; }
}

// AI Processing Results
public class Fragment : BaseEntity
{
    public string Content { get; set; }
    public FragmentType Type { get; set; } // Decision, ActionItem, FeatureRequest, Bug, Insight
    public float ConfidenceScore { get; set; }
    public string SourceContext { get; set; } // Surrounding context
    public Vector Embedding { get; set; } // pgvector for similarity
    public string Metadata { get; set; } // JSON metadata
    public ProcessingStatus Status { get; set; }
    public int SourceId { get; set; }
    public Source Source { get; set; }
    public DateTime ExtractedAt { get; set; }
    public string ExtractedBy { get; set; } // AI model version
}

public class ProcessingJob : BaseEntity
{
    public string JobType { get; set; }
    public string InputData { get; set; } // JSON input
    public string OutputData { get; set; } // JSON output
    public ProcessingStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
```

### Service Interfaces

**Core Abstractions:**
```csharp
public interface IMeetingDataService
{
    Task<IEnumerable<MeetingTranscript>> GetTranscriptsAsync(DateTime since);
    Task<MeetingTranscript> GetTranscriptAsync(string meetingId);
    Task<bool> TestConnectionAsync();
    Task<ConnectionStatus> GetConnectionStatusAsync();
}

public interface ICodeDataService
{
    Task<IEnumerable<CodeActivity>> GetActivitiesAsync(string repository, DateTime since);
    Task<CodeActivity> GetActivityAsync(string activityId);
    Task<bool> TestConnectionAsync();
    Task<IEnumerable<string>> GetRepositoriesAsync();
}

public interface IAiProcessingService
{
    Task<FragmentExtractionResult> ExtractFragmentsAsync(string content, ContentType type);
    Task<IEnumerable<Fragment>> ProcessContentAsync(string content, string sourceId);
    Task<Vector> GenerateEmbeddingAsync(string text);
    Task<bool> TestConnectionAsync();
}

public interface ISearchService
{
    Task<SearchResult<Fragment>> SearchFragmentsAsync(SearchQuery query);
    Task<IEnumerable<Fragment>> FindSimilarFragmentsAsync(string content, int limit = 10);
    Task IndexFragmentAsync(Fragment fragment);
    Task ReindexAllFragmentsAsync();
}

public interface IBackgroundJobService
{
    Task EnqueueFragmentExtractionAsync(string sourceId, string content);
    Task EnqueueDataIngestionAsync(string integrationId, DateTime since);
    Task<JobStatus> GetJobStatusAsync(string jobId);
    Task<IEnumerable<JobStatus>> GetActiveJobsAsync();
}
```

### MVC Controllers and Views (Epic 2)

**IntegrationController (Extended):**
- `GET /Integrations/{id}/Status` - Integration status page
- `POST /Integrations/{id}/Sync` - Trigger manual sync
- `GET /Integrations/{id}/Data` - View ingested data

**FragmentController:**
- `GET /Fragments` - Fragment search and browse page
- `GET /Fragments/Details/{id}` - Fragment details view
- `GET /Fragments/Search` - Advanced search form
- `POST /Fragments/Search` - Process search with filters
- `GET /Fragments/Similar/{id}` - Similar fragments page

**JobController:**
- `GET /Jobs` - Processing jobs dashboard
- `GET /Jobs/Details/{id}` - Job status and details
- `POST /Jobs/Retry/{id}` - Retry failed job
- `POST /Jobs/Cancel/{id}` - Cancel running job

**Views and Real-time Updates:**
- Server-rendered pages with SignalR for real-time status
- Progress indicators for background processing
- Error handling with user-friendly messages
- Export functionality for search results

## Implementation Guidance

### Development Workflow

1. **Integration Management (Story 2.1)**
   - Create integration management UI with CRUD operations
   - Implement real-time status updates via SignalR
   - Add form validation and error handling

2. **API Authentication (Story 2.2)**
   - Implement secure API key storage with encryption
   - Create API key validation and testing framework
   - Add rate limiting and error handling

3. **Fellow.ai Integration (Stories 2.3-2.4)**
   - Implement Fellow.ai API client with OAuth 2.0
   - Create meeting data ingestion pipeline
   - Add incremental sync and deduplication

4. **GitHub Integration (Stories 2.5-2.6)**
   - Implement GitHub API client with token auth
   - Create code activity ingestion pipeline
   - Add webhook support for real-time updates

5. **AI Processing (Stories 2.7-2.8)**
   - Configure AWS Bedrock client for Claude 4.5
   - Create prompt templates for fragment extraction
   - Implement confidence scoring and validation

6. **Processing Engine (Stories 2.9-2.10)**
   - Build automated fragment extraction pipeline
   - Implement vector embedding generation and storage
   - Add processing queue management and monitoring

7. **Search Interface (Story 2.11)**
   - Create fragment search UI with advanced filtering
   - Implement full-text and semantic similarity search
   - Add export and saved search capabilities

### Prompt Templates

**Meeting Fragment Extraction:**
```
Analyze this meeting transcript and extract structured insights:

TRANSCRIPT:
{transcript}

Extract the following types of fragments:
1. DECISIONS: Clear decisions made during the meeting
2. ACTION_ITEMS: Specific tasks assigned to individuals
3. FEATURE_REQUESTS: New features or enhancements discussed
4. INSIGHTS: Important observations or learnings
5. CONCERNS: Issues, risks, or problems identified

For each fragment, provide:
- Type: One of the above categories
- Content: The actual insight (2-3 sentences max)
- Context: Surrounding discussion context
- Confidence: Score from 0.0 to 1.0
- Participants: Who was involved in this discussion

Return as JSON array of fragments.
```

**Code Activity Fragment Extraction:**
```
Analyze this code activity and extract relevant insights:

ACTIVITY TYPE: {type}
TITLE: {title}
DESCRIPTION: {description}
AUTHOR: {author}

Extract fragments for:
1. FEATURE_IMPLEMENTATION: New features being developed
2. BUG_FIX: Issues being resolved
3. TECHNICAL_DECISION: Architecture or implementation choices
4. IMPROVEMENT: Code quality or performance enhancements

Provide structured JSON output with confidence scores.
```

### Background Processing Architecture

**Job Processing Flow:**
```
Data Ingestion → Queue → AI Processing → Fragment Storage → Indexing
     ↓              ↓           ↓              ↓            ↓
  Scheduling    Priority    Retry Logic    Validation   Search Index
```

**Processing Pipeline:**
```csharp
public class FragmentProcessingPipeline
{
    private readonly IAiProcessingService _aiService;
    private readonly IRepository<Fragment> _fragmentRepo;
    private readonly ISearchService _searchService;

    public async Task ProcessContentAsync(string sourceId, string content)
    {
        // 1. Extract fragments using AI
        var result = await _aiService.ExtractFragmentsAsync(content, ContentType.Meeting);
        
        // 2. Validate and filter fragments
        var validFragments = result.Fragments.Where(f => f.ConfidenceScore >= 0.7);
        
        // 3. Generate embeddings for similarity
        foreach (var fragment in validFragments)
        {
            fragment.Embedding = await _aiService.GenerateEmbeddingAsync(fragment.Content);
        }
        
        // 4. Store fragments
        await _fragmentRepo.SaveAsync(validFragments);
        
        // 5. Update search index
        foreach (var fragment in validFragments)
        {
            await _searchService.IndexFragmentAsync(fragment);
        }
    }
}
```

### Error Handling Strategy

**API Integration Errors:**
- Connection timeouts: Exponential backoff retry
- Rate limiting: Queue requests with delay
- Authentication failures: Alert admin, pause sync
- Data format errors: Log and skip invalid records

**AI Processing Errors:**
- Model unavailable: Queue for retry with different model
- Content too large: Split into smaller chunks
- Low confidence results: Flag for manual review
- Quota exceeded: Pause processing, alert admin

### Performance Optimization

**Database Indexing:**
```sql
-- Fragment search optimization
CREATE INDEX idx_fragments_type_confidence ON fragments(type, confidence_score);
CREATE INDEX idx_fragments_source ON fragments(source_id);
CREATE INDEX idx_fragments_extracted_date ON fragments(extracted_at);

-- Vector similarity optimization
CREATE INDEX idx_fragments_embedding ON fragments USING hnsw (embedding vector_cosine_ops);

-- Full-text search
CREATE INDEX idx_fragments_content_fts ON fragments USING gin(to_tsvector('english', content));
```

**Caching Strategy:**
- API responses: 5-minute cache for integration status
- Fragment search: 1-hour cache for common queries
- AI embeddings: Permanent cache (expensive to regenerate)

## Testing Approach

**Unit Tests:**
- Mock all external APIs (Fellow.ai, GitHub, AWS Bedrock)
- Test fragment extraction logic with sample data
- Validate prompt template generation

**Integration Tests:**
- Test database operations with real PostgreSQL
- Test vector similarity operations
- Test background job processing

**End-to-End Tests:**
- Complete data ingestion workflow
- Fragment extraction and storage pipeline
- Search functionality with real data

## Success Criteria

**Epic 2 Complete When:**
- ✅ Fellow.ai integration working with real meeting data
- ✅ GitHub integration capturing code activities
- ✅ Claude 4.5 extracting high-quality fragments
- ✅ Background processing handling large data volumes
- ✅ Fragment search returning relevant results
- ✅ Vector similarity working for related content
- ✅ Integration management UI fully functional
- ✅ Error handling and monitoring operational
- ✅ Performance targets met (10,000+ fragments/hour)

**Ready for Epic 3:** Fragment Clustering & Intelligence