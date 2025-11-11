using static SeleniumTask.Utilities.WebDriverFactory;

namespace SeleniumTask.Utilities;

public static class TestDataProvider
{
    public static IEnumerable<object[]> GetValidUsers()
    {
        yield return new object[] { "standard_user" };
        yield return new object[] { "problem_user" };
        yield return new object[] { "performance_glitch_user" };
        // Note: excluded "locked_out_user" as it shows different behavior
    }

    public static IEnumerable<object[]> GetBrowsers()
    {
        yield return new object[] { BrowserType.Firefox };
        yield return new object[] { BrowserType.Edge };
    }
}