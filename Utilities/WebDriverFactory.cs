using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;

namespace SauceDemoTests.Utilities;

public class WebDriverFactory
{
    private static readonly Lazy<WebDriverFactory> _instance = new Lazy<WebDriverFactory>(() => new WebDriverFactory());
    public static WebDriverFactory Instance => _instance.Value;

    private WebDriverFactory() { }

    public IWebDriver CreateDriver(BrowserType browserType)
    {
        return browserType switch
        {
            BrowserType.Firefox => CreateFirefoxDriver(),
            BrowserType.Edge => CreateEdgeDriver(),
            _ => throw new ArgumentException($"Unsupported browser: {browserType}")
        };
    }

    private IWebDriver CreateFirefoxDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return new FirefoxDriver(options);
    }

    private IWebDriver CreateEdgeDriver()
    {
        var options = new EdgeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return new EdgeDriver(options);
    }
}

public enum BrowserType
{
    Firefox,
    Edge
}