# SauceDemo Automation Tests

This project contains automated tests for the SauceDemo website (https://www.saucedemo.com/) using Selenium WebDriver with C# and MSTest.

## Test Scenarios

### UC-1: Test Login form with empty credentials
- Type any credentials into "Username" and "Password" fields
- Clear the inputs
- Hit the "Login" button
- Check the error message: "Username is required"

### UC-2: Test Login form with credentials by passing Username
- Type any credentials in username
- Enter password
- Clear the "Password" input
- Hit the "Login" button
- Check the error message: "Password is required"

### UC-3: Test Login form with valid credentials
- Type credentials from accepted usernames section
- Enter password as "secret_sauce"
- Click on Login and validate the title "Swag Labs" in the dashboard

## Technical Stack
- Test Automation: Selenium WebDriver
- Browsers: Firefox, Edge
- Locators: CSS
- Test Runner: MSTest
- Patterns: Singleton, Strategy
- Assertions: FluentAssertions
- Logging: Serilog
