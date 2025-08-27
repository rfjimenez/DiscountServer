namespace DiscountServer.Models
{
    public enum DiscountCodeResult : byte
    {
        Success = 0,
        AlreadyUsed = 1,
        NotFound = 2,
        InvalidRequest = 3
    }
}
