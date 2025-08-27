using DiscountServer.Models;
using DiscountServer.Services;
using Xunit;
using Microsoft.Extensions.Configuration;
using System.IO;

public class DiscountServiceTests
{
    /// <summary>
    /// Helper method to provide in-memory configuration for DiscountService.
    /// </summary>
    private IConfiguration GetTestConfiguration(string path = "Storage/test_discount_codes.json")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DiscountCodeStorage:Path", path)
            })
            .Build();
        return config;
    }

    /// <summary>
    /// Verifies that GenerateCodes returns the correct number of codes,
    /// and that each code has the specified length.
    /// </summary>
    [Fact]
    public void GenerateCodes_ReturnsCorrectCountAndLength()
    {
        var config = GetTestConfiguration();
        var service = new DiscountService(config);
        var codes = service.GenerateCodes(10, 8);
        Assert.Equal(10, codes.Count);
        Assert.All(codes, code => Assert.Equal(8, code.Length));
    }

    /// <summary>
    /// Ensures that GenerateCodes enforces the maximum allowed codes per request.
    /// Requests exceeding the limit should return an empty list.
    /// </summary>
    [Fact]
    public void GenerateCodes_EnforcesMaxCodesPerRequest()
    {
        var config = GetTestConfiguration();
        var service = new DiscountService(config);
        var codes = service.GenerateCodes(3000, 8);
        Assert.Empty(codes);
    }

    /// <summary>
    /// Tests that a generated code can be used once successfully,
    /// and subsequent attempts to use the same code return AlreadyUsed.
    /// </summary>
    [Fact]
    public void UseCode_SuccessAndAlreadyUsed()
    {
        var config = GetTestConfiguration();
        var service = new DiscountService(config);
        var codes = service.GenerateCodes(1, 8);
        var code = codes[0];
        Assert.Equal(DiscountCodeResult.Success, service.UseCode(code));
        Assert.Equal(DiscountCodeResult.AlreadyUsed, service.UseCode(code));
    }

    /// <summary>
    /// Verifies that using a code that does not exist returns NotFound.
    /// </summary>
    [Fact]
    public void UseCode_NotFound()
    {
        var config = GetTestConfiguration();
        var service = new DiscountService(config);
        Assert.Equal(DiscountCodeResult.NotFound, service.UseCode("INVALIDCODE"));
    }

    /// <summary>
    /// Tests persistence by generating and using codes, then reloading the service.
    /// Ensures that used codes remain marked as used and unused codes can still be used.
    /// </summary>
    [Fact]
    public void Persistence_WritesAndReadsCodesCorrectly()
    {
        var testPath = "Storage/test_discount_codes.json";
        if (File.Exists(testPath))
            File.Delete(testPath);

        var config = GetTestConfiguration(testPath);
        var service1 = new DiscountService(config);
        var codes = service1.GenerateCodes(2, 8);
        var code = codes[0];
        service1.UseCode(code);

        // Reload service to verify persistence
        var service2 = new DiscountService(config);

        Assert.Equal(DiscountCodeResult.AlreadyUsed, service2.UseCode(code));
        Assert.Equal(DiscountCodeResult.Success, service2.UseCode(codes[1]));

        if (File.Exists(testPath))
            File.Delete(testPath);
    }
}