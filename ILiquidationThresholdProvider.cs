public interface ILiquidationThresholdProvider
{
    Task<decimal> GetLiquidationThresholdAsync(string tokenAddress);
}