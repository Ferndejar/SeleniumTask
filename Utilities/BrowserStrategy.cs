using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace SeleniumTask.Utilities;

public interface IBrowserStrategy
{
    IWebDriver CreateDriver();
}

public class FirefoxStrategy : IBrowserStrategy
{
    public IWebDriver CreateDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return new FirefoxDriver(options);
    }
}

public class EdgeStrategy : IBrowserStrategy
{
    public IWebDriver CreateDriver()
    {
        var options = new EdgeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return new EdgeDriver(options);
    }
}