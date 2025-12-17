using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Web.Controllers;
using Medley.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Medley.Tests.Web.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IUserAuditLogService> _auditLogServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null, null, null, null);

        _auditLogServiceMock = new Mock<IUserAuditLogService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _auditLogServiceMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext with TempData
        var httpContext = new DefaultHttpContext();
        var tempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        
        _controller.ControllerContext.HttpContext = httpContext;
        _controller.TempData = tempData;
    }

    //[Fact]
    //public void Register_Get_ReturnsView()
    //{
    //    // Act
    //    var result = _controller.Register();

    //    // Assert
    //    Assert.IsType<ViewResult>(result);
    //}

    //[Fact]
    //public async Task Register_Post_WithInvalidModel_ReturnsViewWithModel()
    //{
    //    // Arrange
    //    var model = new RegisterViewModel();
    //    _controller.ModelState.AddModelError("Email", "Required");

    //    // Act
    //    var result = await _controller.Register(model);

    //    // Assert
    //    var viewResult = Assert.IsType<ViewResult>(result);
    //    Assert.Equal(model, viewResult.Model);
    //}

    [Fact]
    public void Login_Get_ReturnsView()
    {
        // Act
        var result = _controller.Login();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Login_Post_WithValidCredentials_LogsAuditAndRedirects()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Test@123!",
            RememberMe = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        _auditLogServiceMock.Verify(x => x.LogLoginAsync(
            user.Id, user.UserName!, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Login_Post_WithInvalidCredentials_LogsFailedAttempt()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        _auditLogServiceMock.Verify(x => x.LogFailedLoginAsync(
            model.Email, It.IsAny<string>(), "Invalid password"), Times.Once);
    }

    [Fact]
    public async Task Login_Post_WithLockedOutAccount_ReturnsLockoutView()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Test@123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Lockout", viewResult.ViewName);
        _auditLogServiceMock.Verify(x => x.LogFailedLoginAsync(
            model.Email, It.IsAny<string>(), "Account locked out"), Times.Once);
    }

    [Fact]
    public async Task Login_Post_WithNonExistentUser_LogsFailedAttempt()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "nonexistent@example.com",
            Password = "Test@123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        _auditLogServiceMock.Verify(x => x.LogFailedLoginAsync(
            model.Email, It.IsAny<string>(), "User not found"), Times.Once);
    }

    [Fact]
    public async Task Logout_LogsAuditAndRedirects()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com"
        };

        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
        _auditLogServiceMock.Verify(x => x.LogLogoutAsync(
            user.Id, user.UserName!, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsEmailAndRedirects()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var code = "confirmation-code";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, code))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ConfirmEmail(userId, code);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Login), redirectResult.ActionName);
        Assert.Equal("Email confirmed successfully! You can now log in.", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var code = "invalid-code";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, code))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act
        var result = await _controller.ConfirmEmail(userId, code);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Login), redirectResult.ActionName);
        Assert.Equal("Error confirming email. The link may have expired.", _controller.TempData["Error"]);
    }
}
