using DiscountServer.Models;

namespace DiscountServer.Models
{
    // Request to generate discount codes
    public record GenerateRequest(ushort Count, byte Length);

    // Response for generate request
    public record GenerateResponse(bool Result);

    // Request to use a discount code
    public record UseCodeRequest(string Code);

    // Response for use code request
    // The Result field should use DiscountCodeResult enum values (as byte)
    public record UseCodeResponse(byte Result);
}