using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Serilog;
using static SauceDemoTests.Utilities.WebDriverFactory;

namespace SauceDemoTests.Pages;

public class LoginPage : BasePage
{
    // CSS Locators
    private static By UsernameField => By.CssSelector("#user-name");
    private static By PasswordField => By.CssSelector("#password");
    private static By LoginButton => By.CssSelector("#login-button");
    private static By ErrorMessage => By.CssSelector(".error-message-container");
    private static By LoginContainer => By.CssSelector(".login_container");

    private readonly ILogger _logger;

    private readonly BrowserType _browserType;


    public LoginPage(IWebDriver driver, BrowserType browserType = BrowserType.Firefox) : base(driver, 2)
    {
        _browserType = browserType;
    }

    public LoginPage(IWebDriver driver) : base(driver, 7)
    {
        _logger = Log.Logger;
    }

    public void NavigateToLoginPage()
    {
        Driver.Navigate().GoToUrl("https://www.saucedemo.com/");
        WaitForElementVisible(LoginContainer);
    }

    public void EnterUsername(string username)
    {
        var element = WaitForElementVisible(UsernameField);
        ClearTextFieldRobust(element);
        if (!string.IsNullOrEmpty(username))
        {
            element.SendKeys(username);
        }
    }

    public void EnterPassword(string password)
    {
        var element = WaitForElementVisible(PasswordField);
        ClearTextFieldRobust(element);
        if (!string.IsNullOrEmpty(password))
        {
            element.SendKeys(password);
        }
    }

    private void ClearTextFieldRobust(IWebElement element)
    {
        try
        {
            // Method 1: Standard Clear()
            element.Clear();

            // Wait a bit and check if field is actually cleared
            Thread.Sleep(100);

            // If not cleared, try other methods
            if (!string.IsNullOrEmpty(element.GetAttribute("value")))
            {
                _logger.Information("Standard Clear() failed, trying alternative methods...");

                // Method 2: Ctrl+A + Delete
                element.SendKeys(Keys.Control + "a");
                element.SendKeys(Keys.Delete);

                Thread.Sleep(100);

                // Method 3: Backspace repeatedly
                if (!string.IsNullOrEmpty(element.GetAttribute("value")))
                {
                    string currentValue = element.GetAttribute("value");
                    for (int i = 0; i < currentValue.Length; i++)
                    {
                        element.SendKeys(Keys.Backspace);
                    }
                }

                // Method 4: JavaScript as last resort
                if (!string.IsNullOrEmpty(element.GetAttribute("value")))
                {
                    ((IJavaScriptExecutor)Driver).ExecuteScript(
                        "arguments[0].value = '';", element);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Error clearing text field: {ex.Message}");
            // Fallback to JavaScript
            try
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript(
                    "arguments[0].value = '';", element);
            }
            catch (Exception jsEx)
            {
                _logger.Error($"JavaScript clear also failed: {jsEx.Message}");
            }
        }
    }

    public void ClickLogin()
    {
        WaitForElementClickable(LoginButton).Click();
    }

    public string GetErrorMessage()
    {
        var element = WaitForElementVisible(ErrorMessage, 3); // 3 second timeout for error
        return element.Text;
    }

    public void VerifyErrorMessage(string expectedError)
    {
        var actualError = GetErrorMessage();
        actualError.Should().Contain(expectedError);
    }

    public bool IsErrorMessageDisplayed()
    {
        return IsElementVisible(ErrorMessage, 3);
    }

    public void Login(string username, string password)
    {
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
    }

    public void ClearWithJavaScript(By locator)
    {
        var element = Driver.FindElement(locator);
        IJavaScriptExecutor js = (IJavaScriptExecutor)Driver;
        js.ExecuteScript("arguments[0].value = '';", element);
    }

    public void SendTabKey()
    {
        // Zak³adamy, ¿e pole nazwy u¿ytkownika jest pierwsze na stronie logowania
        var usernameElement = Driver.FindElement(UsernameField);
        usernameElement.SendKeys(OpenQA.Selenium.Keys.Tab);
    }

    // Alternative: Refresh the page to ensure clean state (most reliable for Edge)
    public void RefreshPageForCleanState()
    {
        Driver.Navigate().Refresh();
        WaitForElementVisible(LoginContainer);
        _logger.Information("Page refreshed for clean state");
    }
}