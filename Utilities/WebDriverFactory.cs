using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using Serilog;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SeleniumTask.Utilities;

public class WebDriverFactory
{
    private static readonly Lazy<WebDriverFactory> _instance = new(() => new WebDriverFactory());
    public static WebDriverFactory Instance => _instance.Value;

    private WebDriverFactory() { }

    /// <summary>
    /// Controls whether we start browser instances with ephemeral (temporary) profiles.
    /// Enable by setting environment variable EPHEMERAL_BROWSER_PROFILE=1 or true
    /// </summary>
    private static bool UseEphemeralProfiles =>
        (Environment.GetEnvironmentVariable("EPHEMERAL_BROWSER_PROFILE") ?? string.Empty).Equals("1", StringComparison.OrdinalIgnoreCase)
        || (Environment.GetEnvironmentVariable("EPHEMERAL_BROWSER_PROFILE") ?? string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase);

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
        try
        {
            new DriverManager().SetUpDriver(new FirefoxConfig());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "WebDriverManager could not setup geckodriver; assuming driver available on PATH.");
        }

        var options = new FirefoxOptions();

        // Preserve existing default args (headless for CI can be changed if desired)
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");

        if (UseEphemeralProfiles)
        {
            var profileDir = Path.Combine(Path.GetTempPath(), "ff-profile-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(profileDir);
                // Two common ways: pass -profile argument, and also try to set options.Profile when possible.
                // Passing -profile argument is broadly compatible.
                options.AddArgument("-profile");
                options.AddArgument(profileDir);
                Log.Debug("Firefox will use ephemeral profile directory: {Dir}", profileDir);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to create ephemeral Firefox profile directory. Falling back to default profile behavior.");
            }

            // Try to set preferences that reduce autofill/password manager behavior
            try
            {
                options.SetPreference("signon.rememberSignons", false);
                options.SetPreference("signon.autofillForms", false);
                options.SetPreference("browser.formfill.enable", false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Firefox: unable to set some preferences (older/newer Selenium binding).");
            }
        }

        Log.Information("Creating FirefoxDriver.");
        var driver = new FirefoxDriver(options);

        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        return driver;
    }

    private static IWebDriver CreateEdgeDriver()
    {
        try
        {
            try
            {
                new DriverManager().SetUpDriver(new EdgeConfig());
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "WebDriverManager could not setup msedgedriver; assuming driver available on PATH.");
            }

            var options = new EdgeOptions();

            // Try to set UseChromium if present (best-effort)
            try
            {
                var prop = options.GetType().GetProperty("UseChromium", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(options, true);
                    Log.Debug("Set EdgeOptions.UseChromium = true via reflection.");
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "EdgeOptions.UseChromium property not present or could not be set.");
            }

            // Use an ephemeral user-data-dir for Edge if requested
            if (UseEphemeralProfiles)
            {
                var profileDir = Path.Combine(Path.GetTempPath(), "edge-profile-" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(profileDir);
                    options.AddArgument($"--user-data-dir={profileDir}");
                    Log.Debug("Edge will use ephemeral user-data-dir: {Dir}", profileDir);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to create ephemeral user-data-dir for Edge. Falling back to default profile behavior.");
                }
            }

            // Standard stability/privacy flags
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--no-first-run");
            options.AddArgument("--no-default-browser-check");
            options.AddArgument("--inprivate");

            // Additional flags to reduce async autofill and other features that may re-populate values
            options.AddArgument("--disable-background-networking");
            options.AddArgument("--disable-features=TranslateUI,AutofillServerCommunication,AutofillServerPrefetch");
            options.AddArgument("--disable-save-password-bubble");
            options.AddArgument("--disable-sync");
            options.AddArgument("--disable-blink-features=AutomationControlled");

            options.AddExcludedArgument("enable-automation");
            try
            {
                options.AddAdditionalOption("useAutomationExtension", false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not set useAutomationExtension via AddAdditionalOption.");
            }

            // Preferences to disable autofill / password manager features
            var prefs = new Dictionary<string, object>
            {
                { "profile.password_manager_enabled", false },
                { "credentials_enable_service", false },
                { "autofill.profile_enabled", false },
                { "autofill.credit_card_enabled", false }
            };

            var addPrefMethod = options.GetType().GetMethod("AddUserProfilePreference", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (addPrefMethod != null)
            {
                foreach (var kvp in prefs)
                {
                    try
                    {
                        addPrefMethod.Invoke(options, new object[] { kvp.Key, kvp.Value });
                        Log.Debug("Edge pref set: {Key} = {Value}", kvp.Key, kvp.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Failed to set Edge preference {Key}", kvp.Key);
                    }
                }
            }
            else
            {
                try
                {
                    options.AddAdditionalOption("prefs", prefs);
                    Log.Debug("Added Edge prefs via AddAdditionalOption.");
                }
                catch (ArgumentException)
                {
                    Log.Debug("Edge prefs were already present; skipping AddAdditionalOption.");
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to add Edge prefs via AddAdditionalOption.");
                }
            }

            options.PageLoadStrategy = PageLoadStrategy.Normal;

            var service = EdgeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            Log.Information("Creating EdgeDriver.");
            var driver = new EdgeDriver(service, options);

            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            
            return driver;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create Edge driver.");
            throw new Exception($"Failed to create Edge driver: {ex.Message}", ex);
        }
    }

    public enum BrowserType
    {
        Firefox,
        Edge
    }
}