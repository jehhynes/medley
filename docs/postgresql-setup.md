# PostgreSQL Setup for Medley Development

This document provides instructions for setting up PostgreSQL with pgvector extension for local development.

## Prerequisites

- PostgreSQL 16.0 or higher (required for pgvector extension support)
- Administrative access to install PostgreSQL and extensions

## Installation Steps

### Windows

1. **Download PostgreSQL 16+**
   - Visit https://www.postgresql.org/download/windows/
   - Download the installer for PostgreSQL 16 or later
   - Run the installer and follow the setup wizard

2. **Install pgvector Extension**
   ```bash
   # Option 1: Using pre-compiled binaries (recommended)
   # Download from: https://github.com/pgvector/pgvector/releases
   # Extract and copy files to PostgreSQL installation directory
   
   # Option 2: Using package manager (if available)
   # This may require additional setup depending on your environment
   ```

3. **Create Development Database**
   ```sql
   -- Connect to PostgreSQL as superuser
   CREATE DATABASE medley_dev;
   CREATE USER postgres WITH PASSWORD 'postgres';
   GRANT ALL PRIVILEGES ON DATABASE medley_dev TO postgres;
   
   -- Connect to medley_dev database
   \c medley_dev
   CREATE EXTENSION IF NOT EXISTS vector;
   ```

### macOS

1. **Install PostgreSQL using Homebrew**
   ```bash
   brew install postgresql@16
   brew services start postgresql@16
   ```

2. **Install pgvector**
   ```bash
   brew install pgvector
   ```

3. **Create Development Database**
   ```sql
   createdb medley_dev
   psql medley_dev -c "CREATE EXTENSION IF NOT EXISTS vector;"
   ```

### Linux (Ubuntu/Debian)

1. **Install PostgreSQL 16**
   ```bash
   sudo apt update
   sudo apt install postgresql-16 postgresql-contrib-16
   ```

2. **Install pgvector**
   ```bash
   sudo apt install postgresql-16-pgvector
   ```

3. **Create Development Database**
   ```bash
   sudo -u postgres createdb medley_dev
   sudo -u postgres psql medley_dev -c "CREATE EXTENSION IF NOT EXISTS vector;"
   ```

## Configuration

### Connection String

The application uses the following connection string format:

**Development:**
```
Host=localhost;Database=medley_dev;Username=postgres;Password=postgres
```

**Production:**
```
Host=your-production-host;Database=medley_prod;Username=your-username;Password=your-password;SSL Mode=Require
```

### Environment Variables (Optional)

You can override connection settings using environment variables:

```bash
export POSTGRES_HOST=localhost
export POSTGRES_DB=medley_dev
export POSTGRES_USER=postgres
export POSTGRES_PASSWORD=postgres
```

## Verification

To verify your setup is working correctly:

1. **Test Connection**
   ```bash
   psql -h localhost -U postgres -d medley_dev -c "SELECT version();"
   ```

2. **Test pgvector Extension**
   ```sql
   SELECT * FROM pg_extension WHERE extname = 'vector';
   ```

3. **Run Application Health Check**
   ```bash
   cd src
   dotnet run --project Medley.Web
   # Navigate to: http://localhost:5000/health
   ```

## Troubleshooting

### Common Issues

1. **Connection Refused**
   - Ensure PostgreSQL service is running
   - Check if port 5432 is available
   - Verify firewall settings

2. **Authentication Failed**
   - Check username/password in connection string
   - Verify user permissions on database
   - Check pg_hba.conf for authentication method

3. **pgvector Extension Not Found**
   - Ensure pgvector is properly installed
   - Check PostgreSQL version compatibility
   - Verify extension is created in the correct database

### Useful Commands

```bash
# Check PostgreSQL status
sudo systemctl status postgresql  # Linux
brew services list | grep postgresql  # macOS

# Connect to database
psql -h localhost -U postgres -d medley_dev

# List databases
\l

# List extensions
\dx

# Exit psql
\q
```

## Security Notes

- The default credentials (postgres/postgres) are for development only
- Use strong passwords and proper authentication in production
- Consider using connection pooling for production deployments
- Enable SSL/TLS for production connections