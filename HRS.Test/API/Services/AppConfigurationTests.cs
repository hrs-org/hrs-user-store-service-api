using HRS.API.Services;
using NSubstitute;
using Xunit;

namespace HRS.Test.API.Services;

public class AppConfigurationTests
{
    [Fact]
    public void Setup_ShouldReadConfigurationValues()
    {
        // Arrange
        var config = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();
        config["Jwt:Key"].Returns("jwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkey");
        config["Jwt:Issuer"].Returns("issuer");
        config["Jwt:Audience"].Returns("audience");

        // Act
        var appConfig = new AppConfiguration(config);

        // Assert
        Assert.Equal("jwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkey", appConfig.JwtKey);
        Assert.Equal("issuer", appConfig.JwtIssuer);
        Assert.Equal("audience", appConfig.JwtAudience);
    }
}
