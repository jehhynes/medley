# Tech Spec: Epic 4 - Document Generation & Review Interface

**Epic:** Document Generation & Review Interface  
**Date:** 2025-10-17  
**Author:** Medley Developer  
**Architecture Reference:** [Solution Architecture](./solution-architecture.md)

## Epic Overview

Build AI-powered document generation and side-by-side review interface for human-AI collaboration in creating comprehensive documentation.

**Value Delivery:** Core value proposition - AI document generation with transparent review process enabling efficient documentation creation.

## Key Components

### Technology Stack
- **Document Generation:** Claude 4.5 via AWS Bedrock
- **Template Engine:** Razor templates with custom extensions
- **Real-time Updates:** SignalR for review collaboration
- **File Export:** Multiple formats (Markdown, HTML, PDF)

### Core Interfaces
```csharp
public interface IDocumentGenerationService
{
    Task<GeneratedDocument> GenerateDocumentAsync(DocumentRequest request);
    Task<DocumentTemplate> GetTemplateAsync(DocumentType type);
    Task<IEnumerable<DocumentSuggestion>> GetSuggestionsAsync(IEnumerable<Fragment> fragments);
}

public interface IReviewWorkflowService
{
    Task<ReviewSession> StartReviewAsync(string documentId, string userId);
    Task<ReviewDecision> SubmitReviewAsync(string sessionId, ReviewInput input);
    Task<Document> ApproveDocumentAsync(string documentId, string userId);
    Task NotifyReviewersAsync(string documentId, IEnumerable<string> reviewerIds);
}
```

### Data Models
```csharp
public class Document : BaseEntity
{
    public string Title { get; set; }
    public DocumentType Type { get; set; }
    public string Content { get; set; }
    public DocumentStatus Status { get; set; }
    public string GeneratedBy { get; set; } // AI model
    public DateTime GeneratedAt { get; set; }
    public string ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public ICollection<Fragment> SourceFragments { get; set; }
    public ICollection<ReviewComment> ReviewComments { get; set; }
}

public class ReviewSession : BaseEntity
{
    public string DocumentId { get; set; }
    public string ReviewerId { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<ReviewComment> Comments { get; set; }
}
```

## Implementation Guidance

**Document Generation Pipeline:**
1. Select relevant fragments based on user criteria
2. Apply document template with AI content generation
3. Generate structured content with source attribution
4. Create review session with side-by-side interface

**Review Interface Features:**
- Split-pane view: AI content | Source fragments
- Inline editing with change tracking
- Comment system for collaborative review
- Approval workflow with version control

**Success Criteria:**
- ✅ Document generation completing in under 30 seconds
- ✅ Review interface supporting real-time collaboration
- ✅ Export functionality working for all formats
- ✅ Source attribution maintaining full traceability