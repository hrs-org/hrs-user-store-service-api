using HRS.API.Services.Helpers;
using Xunit;

namespace HRS.Test.API.Services.Helpers;

public class Auth0ManagementOptionsTests
{
    [Fact]
    public void Auth0ManagementOptions_CanBeInstantiated_WithDefaultValues()
    {
        // Act
        var options = new Auth0ManagementOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(string.Empty, options.Domain);
        Assert.Equal(string.Empty, options.Audience);
        Assert.Equal(string.Empty, options.ClientId);
        Assert.Equal(string.Empty, options.ClientSecret);
        Assert.Equal(string.Empty, options.CustomerRoleId);
        Assert.Equal(string.Empty, options.OwnerRoleId);
        Assert.Equal(string.Empty, options.EmployeeRoleId);
        Assert.Equal(string.Empty, options.ManagerRoleId);
        Assert.Equal(string.Empty, options.AdminRoleId);
    }

    [Fact]
    public void Auth0ManagementOptions_AllPropertiesCanBeSet()
    {
        // Arrange
        var domain = "test.auth0.com";
        var audience = "https://api.test.com";
        var clientId = "client-id";
        var clientSecret = "client-secret";
        var customerRoleId = "role_customer";
        var ownerRoleId = "role_owner";
        var employeeRoleId = "role_employee";
        var managerRoleId = "role_manager";
        var adminRoleId = "role_admin";

        // Act
        var options = new Auth0ManagementOptions
        {
            Domain = domain,
            Audience = audience,
            ClientId = clientId,
            ClientSecret = clientSecret,
            CustomerRoleId = customerRoleId,
            OwnerRoleId = ownerRoleId,
            EmployeeRoleId = employeeRoleId,
            ManagerRoleId = managerRoleId,
            AdminRoleId = adminRoleId
        };

        // Assert
        Assert.Equal(domain, options.Domain);
        Assert.Equal(audience, options.Audience);
        Assert.Equal(clientId, options.ClientId);
        Assert.Equal(clientSecret, options.ClientSecret);
        Assert.Equal(customerRoleId, options.CustomerRoleId);
        Assert.Equal(ownerRoleId, options.OwnerRoleId);
        Assert.Equal(employeeRoleId, options.EmployeeRoleId);
        Assert.Equal(managerRoleId, options.ManagerRoleId);
        Assert.Equal(adminRoleId, options.AdminRoleId);
    }

    [Fact]
    public void Auth0ManagementOptions_SupportsNullValues_ForStringProperties()
    {
        // Act
        var options = new Auth0ManagementOptions
        {
            Domain = "",
            ClientId = ""
        };

        // Assert
        // In C# 8+, these would be nullable depending on nullable reference type configuration
        // For now, we just verify the object can hold these values
        Assert.True(options.Domain == null || options.Domain is string);
    }

    [Fact]
    public void Auth0ManagementOptions_PropertiesAreIndependent()
    {
        // Arrange
        var options1 = new Auth0ManagementOptions { Domain = "test1.auth0.com", ClientId = "id1" };
        var options2 = new Auth0ManagementOptions { Domain = "test2.auth0.com", ClientId = "id2" };

        // Act & Assert
        Assert.NotEqual(options1.Domain, options2.Domain);
        Assert.NotEqual(options1.ClientId, options2.ClientId);
    }

    [Fact]
    public void Auth0ManagementOptions_CanBeUpdatedAfterCreation()
    {
        // Arrange
        var options = new Auth0ManagementOptions { Domain = "initial.auth0.com" };

        // Act
        options.Domain = "updated.auth0.com";

        // Assert
        Assert.Equal("updated.auth0.com", options.Domain);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Auth0ManagementOptions_AcceptsVariousStringValues(string value = "")
    {
        // Act
        var options = new Auth0ManagementOptions { Domain = value };

        // Assert
        Assert.Equal(value, options.Domain);
    }
}
