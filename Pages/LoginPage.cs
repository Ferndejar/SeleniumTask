using System;
using OpenQA.Selenium;
using SeleniumTask.Utilities;
using Serilog;

namespace SeleniumTask.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly string _url = "https://www.saucedemo.com/";

        private readonly By _usernameLocator = By.Id("user-name");
        private readonly By _passwordLocator = By.Id("password");
        private readonly By _loginButtonLocator = By.Id("login-button");
        private readonly By _errorLocator = By.CssSelector("[data-test='error']");

        public LoginPage(IWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public void NavigateToLoginPage()
        {
            _driver.Navigate().GoToUrl(_url);

            // Best-effort: disable autocomplete/ autofill and clear storage to reduce autofill interference
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "document.querySelectorAll('input,textarea').forEach(e => e.setAttribute('autocomplete', 'off'));");
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
                _driver.Manage().Cookies.DeleteAllCookies();
                Log.Debug("NavigateToLoginPage: attempted to disable autocomplete and cleared local/session storage + cookies.");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "NavigateToLoginPage: failed to run diagnostics JS or clear storage/cookies.");
            }
        }

        public void EnterUsername(string username)
        {
            ElementHelpers.ClearAndNotify(_driver, _usernameLocator);

            // Ensure front-end framework (React) updates its internal state too
            try
            {
                ElementHelpers.NotifyFrameworkAboutClear(_driver, _usernameLocator);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "EnterUsername: NotifyFrameworkAboutClear failed for username.");
            }

            try
            {
                var current = _driver.FindElement(_usernameLocator).GetAttribute("value");
                Log.Debug("EnterUsername: username after clear = '{Value}'", current);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "EnterUsername: failed to read username after clear.");
            }

            if (!string.IsNullOrEmpty(username))
            {
                _driver.FindElement(_usernameLocator).SendKeys(username);
                Log.Debug("EnterUsername: sent keys to username field (length {Len}).", username.Length);
            }
        }

        public void EnterPassword(string password)
        {
            ElementHelpers.ClearAndNotify(_driver, _passwordLocator);

            // Ensure front-end framework (React) updates its internal state too
            try
            {
                ElementHelpers.NotifyFrameworkAboutClear(_driver, _passwordLocator);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "EnterPassword: NotifyFrameworkAboutClear failed for password.");
            }

            try
            {
                var current = _driver.FindElement(_passwordLocator).GetAttribute("value");
                Log.Debug("EnterPassword: password after clear = '{Value}'", string.IsNullOrEmpty(current) ? "<empty>" : "<masked>");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "EnterPassword: failed to read password after clear.");
            }

            if (!string.IsNullOrEmpty(password))
            {
                _driver.FindElement(_passwordLocator).SendKeys(password);
                Log.Debug("EnterPassword: sent keys to password field (length {Len}).", password.Length);
            }
        }

        public void ClickLogin()
        {
            _driver.FindElement(_loginButtonLocator).Click();
            Log.Debug("ClickLogin: login button clicked.");
        }

        public bool IsErrorMessageDisplayed()
        {
            try
            {
                var displayed = _driver.FindElement(_errorLocator).Displayed;
                Log.Debug("IsErrorMessageDisplayed: {Displayed}", displayed);
                return displayed;
            }
            catch (NoSuchElementException)
            {
                Log.Debug("IsErrorMessageDisplayed: error element not found.");
                return false;
            }
        }

        public void VerifyErrorMessage(string expected)
        {
            // Read the visible error text and do a trimmed, case-insensitive substring check.
            var actual = _driver.FindElement(_errorLocator).Text?.Trim() ?? string.Empty;
            Log.Debug("VerifyErrorMessage: actual='{Actual}', expectedSubstring='{Expected}'", actual, expected);

            if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Expected error to contain '{expected}', but was '{actual}'");
        }

        // convenience wrapper for full-login used by UC-3
        public void Login(string username, string password)
        {
            NavigateToLoginPage();
            EnterUsername(username);
            EnterPassword(password);
            ClickLogin();
        }
    }
}