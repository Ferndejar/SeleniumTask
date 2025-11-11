using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using FluentAssertions;

namespace SauceDemoTests.Pages;

public class InventoryPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    private By PageTitle => By.CssSelector(".title");
    private By InventoryContainer => By.CssSelector("#inventory_container");

    public InventoryPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void VerifyPageLoaded()
    {
        _wait.Until(d => d.FindElement(InventoryContainer).Displayed);
    }

    public string GetPageTitle()
    {
        return _wait.Until(d => d.FindElement(PageTitle)).Text;
    }

    public void VerifyPageTitle(string expectedTitle)
    {
        var actualTitle = GetPageTitle();
        actualTitle.Should().Be(expectedTitle);
    }
}