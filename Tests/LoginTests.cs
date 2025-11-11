using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SauceDemoTests.Pages;
using SauceDemoTests.Utilities;
using FluentAssertions;
using Serilog;
using static SauceDemoTests.Utilities.WebDriverFactory;

namespace SauceDemoTests.Tests;

[TestClass]
[TestCategory("LoginTests")]
public class LoginTests
{
    private  IWebDriver? _driver;
    private LoginPage? _loginPage;
    private InventoryPage? _inventoryPage;
    private ILogger? _logger;

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
        _logger?.Information("Test cleaned up");
    }

    private void InitializeBrowser(BrowserType browserType)
    {
        _driver = WebDriverFactory.CreateDriver(browserType);
        _driver.Manage().Window.Maximize();
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        _loginPage = new LoginPage(_driver);
        _inventoryPage = new InventoryPage(_driver);

        // Dodano sprawdzenie nullowalnoœci loggera
        _logger?.Information($"Initialized {browserType} browser");
    }

    [TestMethod]
    [TestProperty("TestCase", "UC-1")]
    public void Login_WithEmptyCredentials_ShouldShowUsernameRequiredError()
    {
        // Use Firefox for all tests to simplify
        InitializeBrowser(BrowserType.Firefox);
        _logger!.Information("Starting UC-1: Login with empty credentials");

        _loginPage!.NavigateToLoginPage();

        // Act - Type and clear both fields
        _loginPage.EnterUsername("test_user");
        _loginPage.EnterPassword("test_password");
        _loginPage.EnterUsername("");
        _loginPage.EnterPassword("");
        _loginPage.ClickLogin();

        // Wait for error message
        bool isErrorDisplayed = _loginPage.IsErrorMessageDisplayed();
        isErrorDisplayed.Should().BeTrue("Error message should be displayed");

        // Assert
        _loginPage.VerifyErrorMessage("Username is required");
        _logger.Information("UC-1 completed: Username required error shown correctly");
    }

    [TestMethod]
    [TestProperty("TestCase", "UC-2")]
    public void Login_WithEmptyPassword_ShouldShowPasswordRequiredError()
    {
        // Use Firefox for all tests to simplify
        InitializeBrowser(BrowserType.Firefox);
        _logger.Information("Starting UC-2: Login with empty password");

        _loginPage.NavigateToLoginPage();

        // Act - Type username and password, then clear password
        _loginPage.EnterUsername("test_user");
        _loginPage.EnterPassword("test_password");
        _loginPage.EnterPassword("");
        _loginPage.ClickLogin();

        // Wait for error message
        bool isErrorDisplayed = _loginPage.IsErrorMessageDisplayed();
        isErrorDisplayed.Should().BeTrue("Error message should be displayed");

        // Assert
        _loginPage.VerifyErrorMessage("Password is required");
        _logger.Information("UC-2 completed: Password required error shown correctly");
    }

    [TestMethod]
    [TestProperty("TestCase", "UC-3")]
    [DataRow("standard_user")]
    [DataRow("problem_user")]
    [DataRow("performance_glitch_user")]
    public void Login_WithValidCredentials_ShouldNavigateToInventoryPage(string username)
    {
        // Use Firefox for all tests to simplify
        InitializeBrowser(BrowserType.Firefox);
        _logger.Information($"Starting UC-3: Login with valid user {username}");

        _loginPage.NavigateToLoginPage();

        // Act
        _loginPage.Login(username, "secret_sauce");

        // Wait for inventory page to load
        bool isInventoryLoaded = _inventoryPage.IsInventoryPageLoaded();
        isInventoryLoaded.Should().BeTrue("Inventory page should be loaded after successful login");

        // Assert
        _inventoryPage.VerifyPageTitle("Products");

        // Verify page contains "Swag Labs" in title
        _driver.Title.Should().Be("Swag Labs");
        _logger.Information($"UC-3 completed: Successfully logged in as {username}");
    }

  
    [TestCategory("LoginTests")]

        [TestMethod]
        [TestProperty("TestCase", "UC-1-Edge")]
        public void Login_WithEmptyCredentials_OnEdge_ShouldShowUsernameRequiredError()
        {
            InitializeBrowser(BrowserType.Edge);
            _logger.Information("Starting UC-1 on Edge: Login with empty credentials");

            // For Edge, use page refresh to ensure clean state
            _loginPage.NavigateToLoginPage();
            _loginPage.RefreshPageForCleanState(); // Extra refresh for Edge

            // Type and immediately clear using tab to blur fields
            _loginPage.EnterUsername("test_user");
            _loginPage.EnterPassword("test_password");

            // Extra clearing step for Edge
            _loginPage.ClearWithJavaScript(By.CssSelector("#user-name"));
            _loginPage.ClearWithJavaScript(By.CssSelector("#password"));

            // Add tab key to blur fields and trigger change events
            _loginPage.SendTabKey(); // This will move focus away from fields

            // Wait a moment for any pending events
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromMilliseconds(500));
            wait.Until(_ => true); // Krótkie oczekiwanie zamiast Thread.Sleep

            _loginPage.ClickLogin();

            // Wait for error message with longer timeout for Edge
            bool isErrorDisplayed = WaitForErrorMessageDisplayed(_loginPage, TimeSpan.FromSeconds(5));
            isErrorDisplayed.Should().BeTrue("Error message should be displayed on Edge");

            // Assert
            _loginPage.VerifyErrorMessage("Username is required");
            _logger.Information("UC-1 on Edge completed: Username required error shown correctly");
        }

        // Dodaj metodê pomocnicz¹ na koñcu klasy
        private bool WaitForErrorMessageDisplayed(LoginPage loginPage, TimeSpan timeout)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, timeout);
            try
            {
                return wait.Until(_ => loginPage.IsErrorMessageDisplayed());
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
    }

