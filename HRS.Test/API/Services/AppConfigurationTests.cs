using System.Threading.Tasks;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;
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
        Assert.Equal("smtp.test.com", appConfig.SmtpHost);
        Assert.Equal(587, appConfig.SmtpPort);
        Assert.Equal("user", appConfig.SmtpUsername);
        Assert.Equal("pass", appConfig.SmtpPassword);
        Assert.Equal("from@test.com", appConfig.FromEmail);
        Assert.Equal("http://frontend", appConfig.FrontendUrl);
        Assert.Equal("jwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkey", appConfig.JwtKey);
        Assert.Equal("issuer", appConfig.JwtIssuer);
        Assert.Equal("audience", appConfig.JwtAudience);
    }
}
