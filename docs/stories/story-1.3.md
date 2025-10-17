# Story 1.3: Vector Database Setup with pgvector

Status: Draft

## Story

As a developer,
I want to configure PostgreSQL with pgvector extension for semantic similarity operations,
So that I can perform efficient similarity matching and clustering of fragments.

## Acceptance Criteria

1. pgvector extension installed and configured on local PostgreSQL instance
2. Vector column setup for fragment embeddings in database schema
3. Indexing strategy implemented for vector similarity searches (HNSW or IVFFlat)
4. Database migration scripts created for vector schema
5. Performance testing completed for vector operations with sample data
6. Connection string configuration verified for vector operations

## Tasks / Subtasks

- [ ] Task 1: Install and Configure pgvector Extension (AC: #1)
  - [ ] Verify PostgreSQL version 16.0+ installed and running
  - [ ] Install pgvector extension using CREATE EXTENSION command
  - [ ] Verify extension installation with SELECT * FROM pg_extension
  - [ ] Document installation steps in README or setup guide
  - [ ] Test basic vector operations (vector creation, distance calculations)

- [ ] Task 2: Add Vector Support to Fragment Entity (AC: #2)
  - [ ] Install Pgvector.EntityFrameworkCore NuGet package to Infrastructure project
  - [ ] Update Fragment entity in Domain layer to include Vector property (float[] or Vector type)
  - [ ] Create FragmentConfiguration class in Infrastructure/Data/Configurations/
  - [ ] Configure vector column mapping using HasColumnType("vector(1536)") for embeddings
  - [ ] Update ApplicationDbContext to include entity configuration

- [ ] Task 3: Implement Vector Indexing Strategy (AC: #3)
  - [ ] Research HNSW vs IVFFlat indexing for 1536-dimensional vectors (Claude embeddings)
  - [ ] Select HNSW for high recall and performance (recommended for <1M vectors)
  - [ ] Configure index creation in migration using raw SQL or HasIndex with custom SQL
  - [ ] Document indexing strategy rationale and performance characteristics
  - [ ] Add index parameters (m=16, ef_construction=64 for HNSW)

- [ ] Task 4: Create and Test Database Migration (AC: #4)
  - [ ] Generate EF Core migration: dotnet ef migrations add AddVectorSupport
  - [ ] Review generated migration SQL for vector column and index creation
  - [ ] Add custom SQL for pgvector extension and HNSW index if needed
  - [ ] Test migration on clean local database instance
  - [ ] Test rollback: dotnet ef database update <previous-migration>
  - [ ] Verify migration idempotency (can run multiple times safely)

- [ ] Task 5: Implement Vector Repository Methods (AC: #2, #5)
  - [ ] Add IFragmentRepository interface with vector similarity methods
  - [ ] Implement FindSimilarAsync(Vector embedding, int limit, double threshold)
  - [ ] Use EF Core vector distance functions (<-> for cosine, <#> for inner product)
  - [ ] Add unit tests for repository methods with mocked DbContext
  - [ ] Add integration tests with real database and sample vector data

- [ ] Task 6: Performance Testing and Validation (AC: #5)
  - [ ] Create seed data with 1000+ sample vectors for testing
  - [ ] Implement similarity search test queries with different parameters
  - [ ] Measure query performance: <100ms for top-10 similarity search
  - [ ] Test with and without HNSW index to validate performance improvement
  - [ ] Document performance benchmarks and optimization recommendations

- [ ] Task 7: Connection Configuration and Verification (AC: #6)
  - [ ] Verify connection string includes necessary PostgreSQL parameters
  - [ ] Test vector operations through EF Core DbContext
  - [ ] Add health check endpoint for pgvector extension availability
  - [ ] Validate configuration in development environment
  - [ ] Document any special connection string requirements

- [ ] Task 8: Testing and Documentation (All ACs)
  - [ ] Unit tests for Fragment entity with vector property
  - [ ] Integration tests for vector similarity queries
  - [ ] Test migration rollback and reapplication
  - [ ] Document pgvector setup in development environment guide
  - [ ] Add troubleshooting section for common pgvector issues
  - [ ] Verify all acceptance criteria met with passing tests

## Dev Notes

### Architecture Patterns and Constraints

**Clean Architecture Alignment:**
- Vector operations abstracted through `IFragmentRepository` interface in Application layer
- Fragment entity with vector property in Domain layer (no external dependencies)
- EF Core vector configuration in Infrastructure layer
- Repository implementation in Infrastructure layer

**Key Technical Decisions:**
- **pgvector Extension:** Native PostgreSQL vector operations with SQL compatibility, no external vector database needed
- **Vector Dimensions:** 1536 dimensions (Claude 4.5 embedding size via AWS Bedrock)
- **Indexing Strategy:** HNSW (Hierarchical Navigable Small World) for high recall and performance with <1M vectors
  - HNSW parameters: m=16 (connections per layer), ef_construction=64 (build-time accuracy)
  - Alternative: IVFFlat for >1M vectors or lower memory requirements
- **Distance Metric:** Cosine distance (<-> operator) for semantic similarity
- **Performance Target:** <100ms for top-10 similarity search (NFR001: sub-2-second response)

**Integration Points:**
- Vector embeddings will be generated by AWS Bedrock (Claude 4.5) in Epic 2
- Fragment clustering will use vector similarity in Epic 3
- Document generation will leverage clustered fragments in Epic 4

**NuGet Packages Required:**
- `Pgvector.EntityFrameworkCore` - EF Core support for pgvector types
- Already installed: `Npgsql.EntityFrameworkCore.PostgreSQL` (from Story 1.1)

### Project Structure Notes

**Files to Create/Modify:**

```
src/Medley.Domain/
├── Entities/
│   └── Fragment.cs (add Vector property: public float[]? Embedding { get; set; })

src/Medley.Application/
├── Interfaces/
│   └── IFragmentRepository.cs (add FindSimilarAsync method)

src/Medley.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs (register FragmentConfiguration)
│   ├── Configurations/
│   │   └── FragmentConfiguration.cs (NEW - vector column mapping)
│   ├── Repositories/
│   │   └── FragmentRepository.cs (implement similarity search)
│   └── Migrations/
│       └── [timestamp]_AddVectorSupport.cs (generated migration)

src/tests/Medley.Tests.Infrastructure/
├── Repositories/
│   └── FragmentRepositoryTests.cs (vector similarity tests)
```

**Configuration Changes:**
- `appsettings.Development.json` - Verify PostgreSQL connection string
- No special connection string parameters needed for pgvector

**Alignment with Existing Structure:**
- Builds on Story 1.1 database foundation (PostgreSQL + EF Core)
- Compatible with Story 1.2 authentication (no conflicts)
- Prepares Fragment entity for Epic 2 AI processing
- No deviations from established Clean Architecture pattern

### References

**Source Documents:**
- [Tech Spec Epic 1](../tech-spec-epic-1.md#Data Models (Epic 1)) - Fragment entity structure and vector requirements
- [Epics](../epics.md#Story 1.3: Vector Database Setup with pgvector) - Story acceptance criteria and prerequisites
- [Solution Architecture](../solution-architecture.md#2.1 Architecture Pattern) - Clean Architecture layer separation and interface abstractions
- [PRD](../PRD.md#Non-Functional Requirements) - NFR002: Performance requirements (10,000+ fragments/hour processing)

**Technical References:**
- pgvector documentation: https://github.com/pgvector/pgvector
- Pgvector.EntityFrameworkCore: https://github.com/pgvector/pgvector-dotnet
- HNSW algorithm: https://arxiv.org/abs/1603.09320
- PostgreSQL vector operations: https://github.com/pgvector/pgvector#querying

**Performance Benchmarks:**
- HNSW index: ~95% recall with 10x faster queries vs sequential scan
- Target: <100ms for top-10 similarity search on 10,000 vectors
- Memory overhead: ~200 bytes per vector for HNSW index

## Dev Agent Record

### Context Reference

<!-- Path(s) to story context XML will be added here by context workflow -->

### Agent Model Used

Claude 3.5 Sonnet (via Kiro IDE)

### Debug Log References

### Completion Notes List

### File List

## Change Log

- **2025-01-17**: Story 1.3 updated by SM agent. Enhanced task breakdown with 8 tasks and 35+ subtasks. Added detailed technical decisions (HNSW indexing, 1536-dimensional vectors, cosine distance). Expanded dev notes with architecture patterns, file structure, and performance benchmarks. Status: Draft (needs review via story-ready). Prerequisites: Story 1.1 completed.