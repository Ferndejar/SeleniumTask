using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SauceDemoTests.Utilities;

public class WebDriverFactory
{
    private static readonly Lazy<WebDriverFactory> _instance = new(() => new WebDriverFactory());
    public static WebDriverFactory Instance => _instance.Value;

    private WebDriverFactory() { }

    public static IWebDriver CreateDriver(BrowserType browserType)
    {
        return browserType switch
        {
            BrowserType.Firefox => CreateFirefoxDriver(),
            BrowserType.Edge => CreateEdgeDriver(),
            _ => throw new ArgumentException($"Unsupported browser: {browserType}")
        };
    }

    private static IWebDriver CreateFirefoxDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return new FirefoxDriver(options);
    }

    private static IWebDriver CreateEdgeDriver()
    {
        try
        {
            // Use WebDriverManager to automatically manage Edge driver
            new DriverManager().SetUpDriver(new EdgeConfig());

            var options = new EdgeOptions();

            // Basic stable configuration
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--no-first-run");
            options.AddArgument("--no-default-browser-check");

            // Remove automation flags that might block loading
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            // Set page load strategy
            options.PageLoadStrategy = PageLoadStrategy.Normal;

            var service = EdgeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = false;

            var driver = new EdgeDriver(service, options);

            // Set reasonable timeouts
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            return driver;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create Edge driver with WebDriverManager: {ex.Message}", ex);
        }
    }

    public enum BrowserType
    {
        Firefox,
        Edge
    }
}