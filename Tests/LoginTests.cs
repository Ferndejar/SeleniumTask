using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SauceDemoTests.Pages;
using SauceDemoTests.Utilities;
using FluentAssertions;
using Serilog;

namespace SauceDemoTests.Tests;

[TestClass]
[TestCategory("LoginTests")]
public class LoginTests
{
    private IWebDriver _driver;
    private LoginPage _loginPage;
    private InventoryPage _inventoryPage;
    private ILogger _logger;

    [TestInitialize]
    public void TestInitialize()
    {
        // Setup logger
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        _logger.Information("Test initialized");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _driver?.Quit();
        _driver?.Dispose();
        _logger.Information("Test cleaned up");
    }

    private void InitializeBrowser(BrowserType browserType)
    {
        _driver = WebDriverFactory.Instance.CreateDriver(browserType);
        _driver.Manage().Window.Maximize();
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        
        _loginPage = new LoginPage(_driver);
        _inventoryPage = new InventoryPage(_driver);
        
        _logger.Information($"Initialized {browserType} browser");
    }

    [TestMethod]
    [TestProperty("TestCase", "UC-1")]
    [DataRow(BrowserType.Firefox)]
    [DataRow(BrowserType.Edge)]
    public void Login_WithEmptyCredentials_ShouldShowUsernameRequiredError(BrowserType browserType)
    {
        // Arrange
        InitializeBrowser(browserType);
        _logger.Information("Starting UC-1: Login with empty credentials");
        
        _loginPage.NavigateToLoginPage();

        // Act - Type and clear both fields
        _loginPage.EnterUsername("test_user");
        _loginPage.EnterPassword("test_password");
        _loginPage.EnterUsername("");
        _loginPage.EnterPassword("");
        _loginPage.ClickLogin();

        // Assert
        _loginPage.VerifyErrorMessage("Username is required");
        _logger.Information("UC-1 completed: Username required error shown correctly");
    }

    [TestMethod]
    [TestProperty("TestCase", "UC-2")]
    [DataRow(BrowserType.Firefox)]
    [DataRow(BrowserType.Edge)]
    public void Login_WithEmptyPassword_ShouldShowPasswordRequiredError(BrowserType browserType)
    {
        // Arrange
        InitializeBrowser(browserType);
        _logger.Information("Starting UC-2: Login with empty password");
        
        _loginPage.NavigateToLoginPage();

        // Act - Type username and password, then clear password
        _loginPage.EnterUsername("test_user");
        _loginPage.EnterPassword("test_password");
        _loginPage.EnterPassword("");
        _loginPage.ClickLogin();

        // Assert
        _loginPage.VerifyErrorMessage("Password is required");
        _logger.Information("UC-2 completed: Password required error shown correctly");
    }

    [TestMethod]
    [TestProperty("TestCase", "UC-3")]
    [DynamicData(nameof(GetValidUsersData), DynamicDataSourceType.Method)]
    [DynamicData(nameof(GetBrowsersData), DynamicDataSourceType.Method)]
    public void Login_WithValidCredentials_ShouldNavigateToInventoryPage(string username, BrowserType browserType)
    {
        // Arrange
        InitializeBrowser(browserType);
        _logger.Information($"Starting UC-3: Login with valid user {username} on {browserType}");
        
        _loginPage.NavigateToLoginPage();

        // Act
        _loginPage.Login(username, "secret_sauce");

        // Assert
        _inventoryPage.VerifyPageLoaded();
        _inventoryPage.VerifyPageTitle("PRODUCTS");
        
        // Verify page contains "Swag Labs" in title
        _driver.Title.Should().Be("Swag Labs");
        _logger.Information($"UC-3 completed: Successfully logged in as {username} on {browserType}");
    }

    public static IEnumerable<object[]> GetValidUsersData()
    {
        foreach (var user in TestDataProvider.GetValidUsers())
        {
            foreach (var browser in TestDataProvider.GetBrowsers())
            {
                yield return new object[] { user[0], browser[0] };
            }
        }
    }

    public static IEnumerable<object[]> GetBrowsersData()
    {
        return TestDataProvider.GetBrowsers();
    }
}