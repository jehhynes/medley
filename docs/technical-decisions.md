# Technical Decisions Log

**Project:** Medley - AI-Powered Product Intelligence Platform
**Created:** 2025-10-17
**Last Updated:** 2025-10-17

---

## Integration Priorities

### Primary Integration: Fellow.ai
**Date:** 2025-10-17
**Decision:** Fellow.ai integration is the first/highest priority integration for meeting transcript processing
**Rationale:** Fellow.ai provides structured meeting transcripts and action items that are ideal for AI fragment extraction
**Impact:** This will be the foundational integration for MVP, with other integrations following in subsequent phases
**Technical Notes:** Fellow.ai uses API key authentication (not OAuth), GitHub also uses API key authentication
**Authentication:** Start with API key authentication for Fellow.ai and GitHub, OAuth support can be added later for other integrations

---

## Technology Stack Preferences

### Backend Framework
**Decision:** ASP.NET Core Web API with Entity Framework Core
**Rationale:** Aligns with existing expertise and Microsoft ecosystem preference

### Database
**Decision:** PostgreSQL with pgvector extension (local instance)
**Rationale:** Supports both structured data and vector similarity matching for fragment clustering
**Implementation:** Use local PostgreSQL installation rather than Docker Compose for development

### AI/ML Services
**Decision:** Claude 4.5 via AWS Bedrock
**Rationale:** High-quality LLM processing with AWS integration for seamless infrastructure alignment
**Impact:** Enables consistent AWS ecosystem usage and potentially better cost optimization

### Infrastructure
**Decision:** AWS (Amazon Web Services)
**Rationale:** Comprehensive cloud platform with mature AI/ML services, cost-effective scaling, and strong integration capabilities
**Impact:** Aligns with Bedrock AI service selection

---

## Architecture Decisions

### Service Architecture
**Decision:** Modular design with separate services for data ingestion, fragment extraction, document generation, and user management
**Rationale:** Enables independent scaling and maintenance of different system components

---

_This file captures technical details, preferences, and constraints mentioned during PRD discussions._