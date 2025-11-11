using OpenQA.Selenium;
using FluentAssertions;

namespace SeleniumTask.Pages;

public class InventoryPage : BasePage
{
    private static By PageTitle => By.CssSelector(".title");
    private static By InventoryContainer => By.CssSelector("#inventory_container");
    private static By BurgerMenu => By.CssSelector("#react-burger-menu-btn");

    public InventoryPage(IWebDriver driver) : base(driver, 2) { }

    public void VerifyPageLoaded()
    {
        WaitForElementVisible(InventoryContainer);
        WaitForElementVisible(BurgerMenu);
    }

    public string GetPageTitle()
    {
        return WaitForElementVisible(PageTitle).Text;
    }

    public void VerifyPageTitle(string expectedTitle)
    {
        var actualTitle = GetPageTitle();
        actualTitle.Should().Be(expectedTitle);
    }

    public bool IsInventoryPageLoaded()
    {
        return IsElementVisible(InventoryContainer, 5) && IsElementVisible(BurgerMenu, 5);
    }
}