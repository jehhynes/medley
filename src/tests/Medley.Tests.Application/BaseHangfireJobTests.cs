using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using Xunit;

namespace Medley.Tests.Application.Jobs;

/// <summary>
/// Unit tests for BaseHangfireJob
/// </summary>
public class BaseHangfireJobTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<TestJob>> _mockLogger;
    private readonly TestJob _testJob;

    public BaseHangfireJobTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<TestJob>>();
        _testJob = new TestJob(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWithTransactionAsync_SuccessfulExecution_CommitsTransaction()
    {
        // Arrange
        var jobActionExecuted = false;
        var jobAction = new Func<Task>(() =>
        {
            jobActionExecuted = true;
            return Task.CompletedTask;
        });

        // Act
        await _testJob.ExecuteWithTransactionAsync(jobAction);

        // Assert
        Assert.True(jobActionExecuted);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(IsolationLevel.ReadCommitted), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteWithTransactionAsync_JobActionThrowsException_RollsBackTransaction()
    {
        // Arrange
        var jobAction = new Func<Task>(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _testJob.ExecuteWithTransactionAsync(jobAction));

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(IsolationLevel.ReadCommitted), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithTransactionAsync_WithResult_SuccessfulExecution_CommitsTransaction()
    {
        // Arrange
        var expectedResult = "Test Result";
        var jobActionExecuted = false;
        var jobAction = new Func<Task<string>>(() =>
        {
            jobActionExecuted = true;
            return Task.FromResult(expectedResult);
        });

        // Act
        var result = await _testJob.ExecuteWithTransactionAsync(jobAction);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.True(jobActionExecuted);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(IsolationLevel.ReadCommitted), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Never);
    }

    /// <summary>
    /// Test implementation of BaseHangfireJob for testing purposes
    /// </summary>
    public class TestJob : BaseHangfireJob<TestJob>
    {
        public TestJob(IUnitOfWork unitOfWork, ILogger<TestJob> logger) : base(unitOfWork, logger)
        {
        }

        public new Task ExecuteWithTransactionAsync(Func<Task> jobAction, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return base.ExecuteWithTransactionAsync(jobAction, isolationLevel);
        }

        public new Task<TResult> ExecuteWithTransactionAsync<TResult>(Func<Task<TResult>> jobAction, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return base.ExecuteWithTransactionAsync(jobAction, isolationLevel);
        }
    }
}
