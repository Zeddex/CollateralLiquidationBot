public interface ITokenPriceProvider
{
    Task<decimal> GetPriceInEthAsync(string tokenAddress);
}