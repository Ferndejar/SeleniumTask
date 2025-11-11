using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using FluentAssertions;

namespace SauceDemoTests.Pages;

public class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    // CSS Locators
    private By UsernameField => By.CssSelector("#user-name");
    private By PasswordField => By.CssSelector("#password");
    private By LoginButton => By.CssSelector("#login-button");
    private By ErrorMessage => By.CssSelector(".error-message-container");
    private By LoginContainer => By.CssSelector(".login_container");

    public LoginPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void NavigateToLoginPage()
    {
        _driver.Navigate().GoToUrl("https://www.saucedemo.com/");
        _wait.Until(d => d.FindElement(LoginContainer).Displayed);
    }

    public void EnterUsername(string username)
    {
        var element = _wait.Until(d => d.FindElement(UsernameField));
        element.Clear();
        if (!string.IsNullOrEmpty(username))
        {
            element.SendKeys(username);
        }
    }

    public void EnterPassword(string password)
    {
        var element = _wait.Until(d => d.FindElement(PasswordField));
        element.Clear();
        if (!string.IsNullOrEmpty(password))
        {
            element.SendKeys(password);
        }
    }

    public void ClickLogin()
    {
        _wait.Until(d => d.FindElement(LoginButton)).Click();
    }

    public string GetErrorMessage()
    {
        var element = _wait.Until(d => d.FindElement(ErrorMessage));
        return element.Text;
    }

    public void VerifyErrorMessage(string expectedError)
    {
        var actualError = GetErrorMessage();
        actualError.Should().Contain(expectedError);
    }

    public void Login(string username, string password)
    {
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
    }
}