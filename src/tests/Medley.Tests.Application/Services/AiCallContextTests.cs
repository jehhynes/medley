using Medley.Application.Services;
using Xunit;

namespace Medley.Tests.Application.Services;

public class AiCallContextTests
{
    [Fact]
    public void SetContext_SetsServiceAndOperation()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("TestService", "TestOperation", "Entity", Guid.NewGuid()))
        {
            // Assert
            Assert.Equal("TestService", context.ServiceName);
            Assert.Equal("TestOperation", context.OperationName);
            Assert.True(context.HasContext);
            Assert.Null(context.CallStack);
        }
    }

    [Fact]
    public void SetContext_WithoutEntityType_SetsBasicContext()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("TestService", "TestOperation"))
        {
            // Assert
            Assert.Equal("TestService", context.ServiceName);
            Assert.Equal("TestOperation", context.OperationName);
            Assert.Null(context.EntityType);
            Assert.Null(context.EntityId);
        }
    }

    [Fact]
    public void SetContext_Dispose_RestoresContext()
    {
        // Arrange
        var context = new AiCallContext();

        // Act & Assert
        using (context.SetContext("Service1", "Operation1"))
        {
            Assert.Equal("Service1", context.ServiceName);
            Assert.Equal("Operation1", context.OperationName);
        }

        // After dispose, context should be cleared
        Assert.Null(context.ServiceName);
        Assert.Null(context.OperationName);
        Assert.False(context.HasContext);
    }

    [Fact]
    public void SetContext_Nested_CreatesCallStack()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("Service1", "Operation1"))
        {
            Assert.Null(context.CallStack); // No stack at top level

            using (context.SetContext("Service2", "Operation2"))
            {
                // Assert
                Assert.Equal("Service2", context.ServiceName);
                Assert.Equal("Operation2", context.OperationName);
                Assert.Equal("Service1:Operation1", context.CallStack);
            }

            // After inner dispose, should restore to Service1
            Assert.Equal("Service1", context.ServiceName);
            Assert.Equal("Operation1", context.OperationName);
            Assert.Null(context.CallStack);
        }
    }

    [Fact]
    public void SetContext_ThreeLevelsDeep_CreatesCorrectCallStack()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("Service1", "Op1"))
        {
            using (context.SetContext("Service2", "Op2"))
            {
                using (context.SetContext("Service3", "Op3"))
                {
                    // Assert
                    Assert.Equal("Service3", context.ServiceName);
                    Assert.Equal("Op3", context.OperationName);
                    Assert.Equal("Service1:Op1\nService2:Op2", context.CallStack);
                }

                // After third level dispose
                Assert.Equal("Service2", context.ServiceName);
                Assert.Equal("Service1:Op1", context.CallStack);
            }

            // After second level dispose
            Assert.Equal("Service1", context.ServiceName);
            Assert.Null(context.CallStack);
        }

        // After all dispose
        Assert.Null(context.ServiceName);
        Assert.False(context.HasContext);
    }

    [Fact]
    public void SetContext_WithEntityInfo_PreservesEntityInfo()
    {
        // Arrange
        var context = new AiCallContext();
        var entityId = Guid.NewGuid();

        // Act
        using (context.SetContext("Service1", "Op1", "Source", entityId))
        {
            // Assert
            Assert.Equal("Source", context.EntityType);
            Assert.Equal(entityId, context.EntityId);

            using (context.SetContext("Service2", "Op2", "Fragment", Guid.NewGuid()))
            {
                Assert.Equal("Fragment", context.EntityType);
                Assert.NotEqual(entityId, context.EntityId);
            }

            // After inner dispose, entity info should be restored
            Assert.Equal("Source", context.EntityType);
            Assert.Equal(entityId, context.EntityId);
        }
    }

    [Fact]
    public void SetContext_NullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var context = new AiCallContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetContext(null!, "Operation"));
    }

    [Fact]
    public void SetContext_EmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var context = new AiCallContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetContext("", "Operation"));
    }

    [Fact]
    public void SetContext_NullOperationName_ThrowsArgumentException()
    {
        // Arrange
        var context = new AiCallContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetContext("Service", null!));
    }

    [Fact]
    public void EnsureContextSet_WhenContextSet_DoesNotThrow()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("Service", "Operation"))
        {
            // Assert - should not throw
            context.EnsureContextSet();
        }
    }

    [Fact]
    public void EnsureContextSet_WhenContextNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new AiCallContext();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => context.EnsureContextSet());
        Assert.Contains("AiCallContext has not been set", exception.Message);
    }

    [Fact]
    public void HasContext_ReturnsTrueWhenSet()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("Service", "Operation"))
        {
            // Assert
            Assert.True(context.HasContext);
        }

        Assert.False(context.HasContext);
    }

    [Fact]
    public void SetContext_MultipleSequentialCalls_WorksCorrectly()
    {
        // Arrange
        var context = new AiCallContext();

        // Act & Assert - First context
        using (context.SetContext("Service1", "Op1"))
        {
            Assert.Equal("Service1", context.ServiceName);
        }

        Assert.False(context.HasContext);

        // Second context
        using (context.SetContext("Service2", "Op2"))
        {
            Assert.Equal("Service2", context.ServiceName);
            Assert.Null(context.CallStack); // No stack because previous was disposed
        }

        Assert.False(context.HasContext);
    }

    [Fact]
    public void SetContext_NestedWithSameServiceDifferentOperation_WorksCorrectly()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("MyService", "Operation1"))
        {
            Assert.Equal("MyService", context.ServiceName);
            Assert.Equal("Operation1", context.OperationName);

            using (context.SetContext("MyService", "Operation2"))
            {
                Assert.Equal("MyService", context.ServiceName);
                Assert.Equal("Operation2", context.OperationName);
                Assert.Equal("MyService:Operation1", context.CallStack);
            }

            Assert.Equal("Operation1", context.OperationName);
        }
    }

    [Fact]
    public void CallStack_FormatIsNewlineSeparated()
    {
        // Arrange
        var context = new AiCallContext();

        // Act
        using (context.SetContext("Service1", "Op1"))
        {
            using (context.SetContext("Service2", "Op2"))
            {
                using (context.SetContext("Service3", "Op3"))
                {
                    // Assert
                    var stack = context.CallStack;
                    Assert.NotNull(stack);
                    Assert.Contains("\n", stack);
                    
                    var lines = stack.Split('\n');
                    Assert.Equal(2, lines.Length);
                    Assert.Equal("Service1:Op1", lines[0]);
                    Assert.Equal("Service2:Op2", lines[1]);
                }
            }
        }
    }
}
