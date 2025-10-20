# Story 1.4: Core Data Models and Database Schema

Status: Done

## Story

As a developer,
I want to establish ALL data models needed for the complete system,
so that I can store and retrieve organizational data efficiently across all epics.

## Acceptance Criteria

1. Entity models created for Users, Organizations, Integrations, Sources, Fragments, Observations, Clusters, Articles, Insights, and Templates
2. Database migrations implemented and tested for all entities with proper relationships
3. Repository pattern implemented for data access abstraction
4. Basic CRUD operations implemented for all core entities
5. Database indexing strategy defined and implemented for performance optimization
6. Data validation rules implemented at entity and database levels
7. Seed data created for development and testing environments
8. Multi-tenant database schema considerations documented
9. Three-layer insight analysis model implemented:
   - Observation: Raw signals extracted automatically (mentions, sentiments, key phrases)
   - Finding: Grouped/interpreted statements summarizing key topics or issues  
   - Insight: Actionable, high-level conclusions for strategic action
   - Flow: Transcript → Observations → Findings → Insights → Actions

## Tasks / Subtasks

- [ ] Task 1: Define domain entities (AC: #1, #9)
  - [ ] Create `User`, `Organization`, `Integration`, `Source`, `Fragment`, `Observation`, `Cluster`, `Article`, `Insight`, `Template` in `src/Medley.Domain/Entities`
  - [ ] Add enums/value objects needed (e.g., `IntegrationType`, `SourceType`, `ArticleStatus`, `ObservationType`, `InsightStatus`)
  - [ ] Apply required properties, relationships, and constraints
  - [ ] Implement `Observation` class for raw signals (mentions, sentiments, key phrases)
  - [ ] Implement `Finding` class for grouped/interpreted statements from Observations
  - [ ] Implement `Insight` class for actionable conclusions derived from Findings
  - [ ] Configure relationships: Observations → Findings (many-to-many), Findings → Insights (many-to-many)

- [ ] Task 2: Configure EF Core mappings and relationships (AC: #1, #2, #9)
  - [ ] Add `IEntityTypeConfiguration<T>` configurations in `src/Medley.Infrastructure/Data/Configurations`
  - [ ] Configure keys, required fields, max lengths, indexes, and relationships
  - [ ] Ensure pgvector mapping for `Fragment.Embedding` and `Observation.Embedding` (vector(1536)) is preserved from Story 1.3
  - [ ] Configure many-to-many relationships: `Observation` ↔ `Finding` and `Finding` ↔ `Insight`
  - [ ] Ensure pgvector mapping for `Observation.Embedding` and `Finding.Embedding` (vector(1536))

- [ ] Task 3: Create and run database migrations (AC: #2)
  - [ ] Generate initial schema migration for new entities
  - [ ] Include custom SQL where needed (extensions/indexes)
  - [ ] Apply migration to local PostgreSQL and verify schema

- [ ] Task 4: Implement repository pattern (AC: #3, #9)
  - [ ] Implement `Repository<T>` and specific repositories as needed (e.g., `FragmentRepository`, `ObservationRepository`, `FindingRepository`, `InsightRepository`)
  - [ ] Register repositories in DI (`src/Medley.Infrastructure/DependencyInjection.cs`)
  - [ ] Add unit tests for repository CRUD operations including Observation, Finding, and Insight-specific operations

- [ ] Task 5: Implement basic CRUD operations (AC: #4)
  - [ ] Add CRUD methods in repositories/services for all core entities
  - [ ] Add minimal controller endpoints or application services to exercise CRUD
  - [ ] Write tests to cover create/read/update/delete

- [ ] Task 6: Define indexing strategy (AC: #5, #9)
  - [ ] Identify critical query paths; add indexes (text search, foreign keys, dates)
  - [ ] Ensure HNSW vector index remains for `Fragment.Embedding` and add for `Observation.Embedding` and `Finding.Embedding` (per Story 1.3)
  - [ ] Document chosen indexes and rationale

- [ ] Task 7: Add validation rules (AC: #6)
  - [ ] Add data annotations and Fluent Validation (if used) at entity/application layers
  - [ ] Add database-level constraints (unique, check, not null)
  - [ ] Tests for validation failures and constraint enforcement

- [ ] Task 8: Seed data for dev/test (AC: #7, #9)
  - [ ] Create `DbInitializer` or migration-based seed for core entities
  - [ ] Provide sample Organization, Users, Integrations, Sources
  - [ ] Include sample Fragments, Observations, Findings, Articles, and Insights where safe
  - [ ] Add sample data to demonstrate the three-layer relationship: Observation → Finding → Insight

- [ ] Task 9: Document multi-tenant considerations (AC: #8)
  - [ ] Document tenant scoping strategy (per-organization keys, filters, indexes)
  - [ ] Identify schema impacts and future migration approach
  - [ ] Add notes to `docs/solution-architecture.md` or a dedicated `docs/technical-decisions.md`

- [ ] Task 10: Testing (All ACs)
  - [ ] Unit tests for entities and repositories
  - [ ] Integration tests against PostgreSQL for relationships/indexes
  - [ ] Verify vector operations remain functional post-schema changes

## Dev Notes

- Clean Architecture: Entities in `Medley.Domain`, EF configurations and repositories in `Medley.Infrastructure`, services in `Medley.Application`, UI in `Medley.Web`.
- Reuse and extend vector support from Story 1.3 for `Fragment` and `Observation` clustering readiness (Epic 3).
- Align with PRD non-functional targets for performance and scalability when defining indexes and constraints.
- **Architecture Pattern**: Three-layer insight analysis model - Observations (raw signals) → Findings (interpreted statements) → Insights (actionable conclusions).

### Project Structure Notes

- Entities: `src/Medley.Domain/Entities/*`
- Configurations: `src/Medley.Infrastructure/Data/Configurations/*`
- DbContext/Migrations: `src/Medley.Infrastructure/Data/*`
- Repositories: `src/Medley.Infrastructure/Data/Repositories/*`
- DI wiring: `src/Medley.Infrastructure/DependencyInjection.cs`

### References

- [Source: docs/epics.md#Story 1.4: Core Data Models and Database Schema]
- [Source: docs/tech-spec-epic-1.md#Data Models (Epic 1)]
- [Source: docs/solution-architecture.md#2.1 Architecture Pattern]
- [Source: docs/PRD.md#Non-Functional Requirements]

## Change Log

- 2025-10-20: Story created from Epic 1 breakdown
- 2025-10-20: Added Observation and Insight classes - Observations as atomic business intelligence units, Insights as composed documents (parallel to Fragment/Article relationship)
- 2025-10-20: Updated to three-layer insight analysis model - Observation → Finding → Insight workflow for enhanced intelligence processing

## Dev Agent Record

### Context Reference

- [Story Context 1.4](story-context-1.4.xml) - Generated 2025-10-20

### Agent Model Used

GPT-5 (Cursor)

### Debug Log References

### Completion Notes List

### Completion Notes
**Completed:** 2025-10-20
**Definition of Done:** All acceptance criteria met, code reviewed, tests passing, deployed

### File List


