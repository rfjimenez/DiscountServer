using DiscountServer.Handlers;
using DiscountServer.Models;
using DiscountServer.Services;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;

public class WebSocketHandlerTest
{
    // Helper to provide in-memory configuration for DiscountService
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
    /// Tests that a valid GENERATE request returns a success response ("true").
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_GenerateRequest_Valid_ReturnsSuccess()
    {
        // Arrange: Mock DiscountService to return two codes
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        mockService.Setup(s => s.GenerateCodes(5, 8)).Returns(new List<string> { "ABC12345", "DEF67890" });
        var handler = new WebSocketHandler(mockService.Object);

        var fakeWebSocket = new FakeWebSocket("GENERATE|5|8");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.Contains("true", fakeWebSocket.SentTextMessages[0]);
    }

    /// <summary>
    /// Tests that a valid USE request returns a success response (DiscountCodeResult.Success).
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_UseRequest_Valid_ReturnsSuccess()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        mockService.Setup(s => s.UseCode("ABC12345")).Returns(DiscountCodeResult.Success);
        var handler = new WebSocketHandler(mockService.Object);

        var fakeWebSocket = new FakeWebSocket("USE|ABC12345");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.Contains(((byte)DiscountCodeResult.Success).ToString(), fakeWebSocket.SentTextMessages[0]);
    }

    /// <summary>
    /// Tests that an invalid request (empty message) returns an InvalidRequest response.
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_InvalidRequest_ReturnsInvalidRequest()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        var handler = new WebSocketHandler(mockService.Object);

        var fakeWebSocket = new FakeWebSocket(""); // Empty message
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.True(fakeWebSocket.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket.SentBinaryMessages[0][0]);
    }

    /// <summary>
    /// Tests that an unknown command returns an InvalidRequest response.
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_UnknownCommand_ReturnsInvalidRequest()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        var handler = new WebSocketHandler(mockService.Object);

        var fakeWebSocket = new FakeWebSocket("INVALIDCMD|5|8");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.True(fakeWebSocket.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket.SentBinaryMessages[0][0]);
    }

    /// <summary>
    /// Tests that a malformed GENERATE request returns an InvalidRequest response.
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_MalformedGenerateRequest_ReturnsInvalidRequest()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        var handler = new WebSocketHandler(mockService.Object);

        // Missing parameters
        var fakeWebSocket = new FakeWebSocket("GENERATE|5");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.True(fakeWebSocket.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket.SentBinaryMessages[0][0]);

        // Non-numeric parameters
        var fakeWebSocket2 = new FakeWebSocket("GENERATE|abc|xyz");
        await handler.HandleWebSocketAsync(fakeWebSocket2);

        Assert.True(fakeWebSocket2.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket2.SentBinaryMessages[0][0]);
    }

    /// <summary>
    /// Tests that a malformed USE request returns an InvalidRequest response.
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_MalformedUseRequest_ReturnsInvalidRequest()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        var handler = new WebSocketHandler(mockService.Object);

        // Missing code parameter
        var fakeWebSocket = new FakeWebSocket("USE");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.True(fakeWebSocket.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket.SentBinaryMessages[0][0]);
    }

    /// <summary>
    /// Tests that GENERATE request exceeding max allowed codes returns an InvalidRequest response.
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_GenerateRequest_ExceedsMax_ReturnsInvalidRequest()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        var handler = new WebSocketHandler(mockService.Object);

        // Exceeds max allowed codes (e.g., 3000)
        var fakeWebSocket = new FakeWebSocket("GENERATE|3000|8");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.True(fakeWebSocket.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket.SentBinaryMessages[0][0]);
    }

    /// <summary>
    /// Tests that GENERATE request with invalid code length returns an InvalidRequest response.
    /// </summary>
    [Fact]
    public async Task HandleWebSocketAsync_GenerateRequest_InvalidLength_ReturnsInvalidRequest()
    {
        var mockService = new Mock<DiscountService>(GetTestConfiguration()) { CallBase = true };
        var handler = new WebSocketHandler(mockService.Object);

        // Length too short
        var fakeWebSocket = new FakeWebSocket("GENERATE|5|3");
        await handler.HandleWebSocketAsync(fakeWebSocket);

        Assert.True(fakeWebSocket.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket.SentBinaryMessages[0][0]);

        // Length too long
        var fakeWebSocket2 = new FakeWebSocket("GENERATE|5|20");
        await handler.HandleWebSocketAsync(fakeWebSocket2);

        Assert.True(fakeWebSocket2.SentBinaryMessages.Count > 0);
        Assert.Equal((byte)DiscountCodeResult.InvalidRequest, fakeWebSocket2.SentBinaryMessages[0][0]);
    }
}