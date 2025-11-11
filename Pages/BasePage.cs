using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SauceDemoTests.Pages;

public abstract class BasePage
{
    protected readonly IWebDriver Driver;
    protected readonly WebDriverWait Wait;

    protected BasePage(IWebDriver driver, int timeoutInSeconds = 2)
    {
        Driver = driver;
        Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
        Wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    protected IWebElement WaitForElementVisible(By locator, int customTimeout = 2)
    {
        var wait = customTimeout != 2 
            ? new WebDriverWait(Driver, TimeSpan.FromSeconds(customTimeout))
            : Wait;

        return wait.Until(d =>
        {
            var element = d.FindElement(locator);
            return element.Displayed ? element : throw new WebDriverTimeoutException($"Element {locator} nie jest widoczny.");
        })!;
    }

    protected bool IsElementVisible(By locator, int timeoutInSeconds = 2)
    {
        try
        {
            WaitForElementVisible(locator, timeoutInSeconds);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    protected IWebElement WaitForElementClickable(By locator, int customTimeout = 2)
    {
        var wait = customTimeout != 2 
            ? new WebDriverWait(Driver, TimeSpan.FromSeconds(customTimeout))
            : Wait;

        return wait.Until(d =>
        {
            var element = d.FindElement(locator);
            return (element.Displayed && element.Enabled) ? element : throw new WebDriverTimeoutException($"Element {locator} nie jest klikalny.");
        })!;
    }

    protected void WaitForPageLoad(int timeoutInSeconds = 10)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutInSeconds));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
    }
}