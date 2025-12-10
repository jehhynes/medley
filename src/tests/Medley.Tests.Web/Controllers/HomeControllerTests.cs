using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Medley.Web.Controllers;
using Medley.Web.Models;
using Medley.Domain.Entities;
using Medley.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query;
using System;

namespace Medley.Tests.Web.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IRepository<Source>> _mockSourceRepository;
        private readonly Mock<IRepository<Fragment>> _mockFragmentRepository;
        private readonly Mock<IRepository<Article>> _mockArticleRepository;
        private readonly Mock<IRepository<ArticleType>> _mockArticleTypeRepository;
        private readonly Mock<IRepository<Tag>> _mockTagRepository;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _mockUserManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
            
            _mockSourceRepository = new Mock<IRepository<Source>>();
            _mockFragmentRepository = new Mock<IRepository<Fragment>>();
            _mockArticleRepository = new Mock<IRepository<Article>>();
            _mockArticleTypeRepository = new Mock<IRepository<ArticleType>>();
            _mockTagRepository = new Mock<IRepository<Tag>>();

            // Setup default empty responses
            SetupRepository(_mockSourceRepository, new List<Source>());
            SetupRepository(_mockFragmentRepository, new List<Fragment>());
            SetupRepository(_mockArticleRepository, new List<Article>());
            SetupRepository(_mockArticleTypeRepository, new List<ArticleType>());
            SetupRepository(_mockTagRepository, new List<Tag>());

            _controller = new HomeController(
                _mockLogger.Object, 
                _mockUserManager.Object,
                _mockSourceRepository.Object,
                _mockFragmentRepository.Object,
                _mockArticleRepository.Object,
                _mockArticleTypeRepository.Object,
                _mockTagRepository.Object);
        }

        private void SetupRepository<T>(Mock<IRepository<T>> mockRepo, List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var asyncQueryable = new TestAsyncEnumerable<T>(queryable);
            mockRepo.Setup(r => r.Query()).Returns(asyncQueryable);
        }

        [Fact]
        public async Task Index_ReturnsViewWithDashboardViewModel()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser@example.com")
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.Equal("testuser@example.com", model.UserName);
            Assert.NotNull(model.Metrics);
        }

        [Fact]
        public async Task Index_WithAnonymousUser_ReturnsGuestName()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.Equal("Guest", model.UserName);
        }

        [Fact]
        public async Task Index_PopulatesBasicMetrics()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "test"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var sources = new List<Source> 
            { 
                new Source 
                { 
                    Id = Guid.NewGuid(), 
                    Type = Domain.Enums.SourceType.Meeting, 
                    CreatedAt = DateTime.UtcNow,
                    Integration = new Integration { Type = Domain.Enums.IntegrationType.Fellow, Status = Domain.Enums.ConnectionStatus.Connected }
                },
                new Source 
                { 
                    Id = Guid.NewGuid(), 
                    Type = Domain.Enums.SourceType.Meeting, 
                    CreatedAt = DateTime.UtcNow,
                    Integration = new Integration { Type = Domain.Enums.IntegrationType.Slack, Status = Domain.Enums.ConnectionStatus.Connected }
                }
            };
            SetupRepository(_mockSourceRepository, sources);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.Equal(2, model.Metrics.TotalSources);
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id";
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("test-trace-id", model.RequestId);
        }
    }

    // Helper classes for async EF Core mocking
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(
                                     name: nameof(IQueryProvider.Execute),
                                     genericParameterCount: 1,
                                     types: new[] { typeof(Expression) })
                                 .MakeGenericMethod(expectedResultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(this); }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current
        {
            get { return _inner.Current; }
        }
    }
}
