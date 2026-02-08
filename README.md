# Medley

AI-powered product intelligence platform that transforms meeting transcripts and organizational conversations into structured documentation through human-AI collaboration.

🌐 [medleyapp.io](https://medleyapp.io/)

⚠️ **Status:** Active development - APIs and features are subject to change.

## Overview

Medley automatically extracts insights from meeting transcripts, clusters related content using semantic similarity, and generates documentation with AI assistance. Built on Clean Architecture principles with ASP.NET Core, PostgreSQL with pgvector, and pluggable AI providers.

## Features

- **AI Fragment Extraction** - Extract insights from transcripts using configurable AI providers
- **Semantic Clustering** - Group related fragments using pgvector similarity search
- **Knowledge Units** - Synthesize fragments into validated knowledge aggregates
- **Article Generation** - AI-assisted documentation creation with version control
- **Article Chat** - Interactive AI conversation with dual modes:
  - **Agent Mode** - Direct article editing, Q&A, and plan implementation
  - **Planning Mode** - Generate improvement plans with knowledge unit recommendations
- **Speaker Extraction** - Automatic speaker identification and tracking
- **Smart Tagging** - Configurable taxonomy with automatic categorization
- **Fellow.ai Integration** - Meeting transcript sync and processing
- **GitHub Integration** - Repository connection management
- **Background Processing** - Hangfire-powered async jobs with monitoring dashboard
- **Real-time Updates** - SignalR notifications
- **RBAC** - Role-based access control (Admin, Editor, Viewer)
- **Token Tracking** - AI API usage and cost monitoring

## Technology Stack

**Backend:** ASP.NET Core (MVC + Razor), C#, PostgreSQL (pgvector), Entity Framework Core, ASP.NET Core Identity, Hangfire, SignalR, Serilog

**AI/ML:** Pluggable AI providers (AWS Bedrock, OpenAI, etc.), Pluggable embedding providers (Ollama, OpenAI), pgvector similarity search

**Frontend:** Vue.js, Vite, TipTap, Chart.js, Bootstrap, Marked

**Storage:** AWS S3 or Local filesystem

**Tools:** Medley.Collector (Windows Forms app for Fellow.ai/Google Drive transcript collection)

## Architecture

Clean Architecture with dependency inversion:

- **Medley.Domain** - Entities, enums, core business logic (no external dependencies)
- **Medley.Application** - Business services, background jobs, interface abstractions
- **Medley.Infrastructure** - EF Core repositories, AWS Bedrock/S3, Fellow.ai/GitHub integrations
- **Medley.Web** - MVC controllers, Razor views, Vue.js components, SignalR hubs, API endpoints

Dependencies point inward to Domain. Interface abstractions enable easy mocking and implementation swapping for Database, AI Processing, and File Storage.

## Prerequisites

- .NET SDK
- PostgreSQL with pgvector extension
- Node.js (frontend build)
- AI provider credentials (AWS Bedrock, OpenAI, or compatible)
- Embedding provider (Ollama for local, or OpenAI)

## License

See [LICENSE.md](LICENSE.md)
