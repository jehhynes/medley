# pgvector Setup Guide

This document provides instructions for setting up PostgreSQL with the pgvector extension for the Medley application.

## Prerequisites

- PostgreSQL 16.0 or higher
- Administrative access to PostgreSQL

## Installation Steps

### 1. Install pgvector Extension

#### On Windows

1. Download pgvector from the official repository: https://github.com/pgvector/pgvector
2. Follow the Windows installation instructions in the repository
3. Alternatively, use Docker (recommended for development):

```bash
docker run -d \
  --name medley-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=medley_dev \
  -p 5432:5432 \
  pgvector/pgvector:pg16
```

#### On Linux/Mac

```bash
# Install build dependencies
sudo apt-get install postgresql-server-dev-16 build-essential

# Clone and build pgvector
git clone https://github.com/pgvector/pgvector.git
cd pgvector
make
sudo make install
```

### 2. Enable pgvector Extension

Connect to your PostgreSQL database and run:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

### 3. Verify Installation

Check that the extension is installed:

```sql
SELECT * FROM pg_extension WHERE extname = 'vector';
```

You should see a row with `extname = 'vector'`.

### 4. Run Database Migrations

The application will automatically create the necessary tables and indexes when you run migrations:

```bash
dotnet ef database update --project src/Medley.Infrastructure --startup-project src/Medley.Web
```

## Vector Configuration

### Embedding Dimensions

The application is configured for **1536-dimensional vectors**, which is the embedding size for Claude 4.5 via AWS Bedrock.

### Indexing Strategy

The application uses **HNSW (Hierarchical Navigable Small World)** indexing for fast similarity search:

- **Index Type**: HNSW with cosine distance
- **Parameters**:
  - `m = 16`: Number of connections per layer
  - `ef_construction = 64`: Build-time accuracy parameter
- **Performance**: Optimized for <1M vectors with high recall (~95%)

### Distance Metric

The application uses **cosine distance** for semantic similarity:
- Range: 0-2 (0 = identical, 2 = opposite)
- Operator: `<->` in PostgreSQL

## Performance Benchmarks

Expected performance with HNSW index:

- **Query Time**: <100ms for top-10 similarity search on 10,000 vectors
- **Recall**: ~95% with default parameters
- **Memory Overhead**: ~200 bytes per vector for HNSW index

## Troubleshooting

### Extension Not Found

If you get an error about the vector extension not being found:

1. Verify pgvector is installed: `SELECT * FROM pg_available_extensions WHERE name = 'vector';`
2. Check PostgreSQL version: `SELECT version();` (must be 16.0+)
3. Restart PostgreSQL service after installation

### Migration Errors

If migrations fail:

1. Ensure the database exists: `CREATE DATABASE medley_dev;`
2. Verify connection string in `appsettings.Development.json`
3. Check PostgreSQL logs for detailed error messages

### Performance Issues

If vector queries are slow:

1. Verify HNSW index exists: `\d+ "Fragments"` in psql
2. Check index usage: `EXPLAIN ANALYZE SELECT ...`
3. Consider adjusting HNSW parameters for your dataset size

## Testing

### Unit Tests

Run unit tests (no database required):

```bash
dotnet test src/tests/Medley.Tests.Domain
dotnet test src/tests/Medley.Tests.Infrastructure
```

### Integration Tests

Run integration tests (requires PostgreSQL with pgvector):

```bash
# Set test connection string
export ConnectionStrings__TestConnection="Host=localhost;Database=medley_test;Username=postgres;Password=postgres"

# Run tests
dotnet test src/tests/Medley.Tests.Integration
```

## Additional Resources

- [pgvector Documentation](https://github.com/pgvector/pgvector)
- [Pgvector.EntityFrameworkCore](https://github.com/pgvector/pgvector-dotnet)
- [HNSW Algorithm Paper](https://arxiv.org/abs/1603.09320)
- [PostgreSQL Vector Operations](https://github.com/pgvector/pgvector#querying)

## Connection String Configuration

The application uses the following connection string format:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=medley_dev;Username=postgres;Password=postgres"
  }
}
```

No special parameters are required for pgvector operations. The extension is automatically enabled through the migration.

## Health Checks

The application includes a health check endpoint that verifies:
- Database connectivity
- pgvector extension availability
- Vector operations functionality

Access the health check at: `/health`
