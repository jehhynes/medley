---
inclusion: fileMatch
fileMatchPattern: ['**/tests/**/*', '**/*.Tests/**/*']
---

## Testing Standards

**Always run tests after making any code modifications** to verify changes haven't broken existing functionality and all new functionality works.

### Framework & Tools
- xUnit for test framework
- Moq for mocking dependencies
- AAA pattern (Arrange, Act, Assert) for test structure
- Organize by layer: `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `Web.Tests`

### Database Test Isolation
**Critical: Never use table truncation.** Always use transaction rollback for test cleanup.

- Use `DatabaseFixture` (implements `IClassFixture<DatabaseFixture>`) for all database tests
- Fixtures automatically manage transaction lifecycle and rollback
- Ensures fast, isolated tests without data deletion or race conditions

Example:
```csharp
public class MyRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public MyRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TestMethod()
    {
        var dbContext = _fixture.CreateDbContext();
        // Test logic here - transaction auto-rolls back
    }
}
```

### Integration Tests
- Use real PostgreSQL connections for infrastructure layer tests
- Test repository implementations against actual database
- Verify EF Core mappings and complex queries with real data