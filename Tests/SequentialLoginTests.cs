using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SauceDemoTests.Pages;
using SauceDemoTests.Utilities;
using FluentAssertions;
using Serilog;
using static SauceDemoTests.Utilities.WebDriverFactory;

namespace SeleniumTask.Tests;

[TestClass]
[TestCategory("SequentialLoginTests")]
public class SequentialLoginTests
{
    private ILogger _logger = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    [TestMethod]
    [TestProperty("TestCase", "All-UCs-Firefox")]
    public void RunAllTestsInFirefox()
    {
        RunAllTestsForBrowser(BrowserType.Firefox);
    }

    [TestMethod] 
    [TestProperty("TestCase", "All-UCs-Edge")]
    public void RunAllTestsInEdge()
    {
        RunAllTestsForBrowser(BrowserType.Edge);
    }

    private void RunAllTestsForBrowser(BrowserType browserType)
    {
        _logger.Information($"Starting all tests for {browserType}");
        
        // UC-1: Empty credentials test
        using (var driver = WebDriverFactory.CreateDriver(browserType))
        {
            try
            {
                driver.Manage().Window.Maximize();
                var loginPage = new LoginPage(driver);
                
                _logger.Information($"Running UC-1 in {browserType}");
                loginPage.NavigateToLoginPage();
                loginPage.EnterUsername("test_user");
                loginPage.EnterPassword("test_password");
                

                loginPage.EnterUsername("");
                loginPage.EnterPassword("");

                loginPage.ClickLogin();

                bool isErrorDisplayed = loginPage.IsErrorMessageDisplayed();
                isErrorDisplayed.Should().BeTrue("Error message should be displayed");
                loginPage.VerifyErrorMessage("Username is required");
                _logger.Information($"UC-1 completed in {browserType}");
            }
            finally
            {
                driver.Quit();
            }
        }

        // UC-2: Empty password test
        using (var driver = WebDriverFactory.CreateDriver(browserType))
        {
            try
            {
                driver.Manage().Window.Maximize();
                var loginPage = new LoginPage(driver);
                
                _logger.Information($"Running UC-2 in {browserType}");
                loginPage.NavigateToLoginPage();
                loginPage.EnterUsername("test_user");
                loginPage.EnterPassword("test_password");
                loginPage.EnterPassword("");
                loginPage.ClickLogin();
                
                bool isErrorDisplayed = loginPage.IsErrorMessageDisplayed();
                isErrorDisplayed.Should().BeTrue("Error message should be displayed");
                loginPage.VerifyErrorMessage("Password is required");
                _logger.Information($"UC-2 completed in {browserType}");
            }
            finally
            {
                driver.Quit();
            }
        }

        // UC-3: Valid credentials test
        using (var driver = WebDriverFactory.CreateDriver(browserType))
        {
            try
            {
                driver.Manage().Window.Maximize();
                var loginPage = new LoginPage(driver);
                var inventoryPage = new InventoryPage(driver);
                
                _logger.Information($"Running UC-3 in {browserType}");
                loginPage.NavigateToLoginPage();
                loginPage.Login("standard_user", "secret_sauce");
                
                bool isInventoryLoaded = inventoryPage.IsInventoryPageLoaded();
                isInventoryLoaded.Should().BeTrue("Inventory page should be loaded");
                inventoryPage.VerifyPageTitle("Products");
                driver.Title.Should().Be("Swag Labs");
                _logger.Information($"UC-3 completed in {browserType}");
            }
            finally
            {
                driver.Quit();
            }
        }

        _logger.Information($"All tests completed for {browserType}");
    }
}