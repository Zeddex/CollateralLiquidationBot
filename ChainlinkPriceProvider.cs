using System.Numerics;
using Nethereum.Web3;

public class ChainlinkPriceProvider(Web3 web3, Dictionary<string, string> chainlinkFeeds) : ITokenPriceProvider
{
    private const string Abi = @"[
        { 
            'inputs': [], 
            'name': 'latestAnswer', 
            'outputs': [
                { 'internalType': 'int256', 'name': '', 'type': 'int256' }
            ], 
            'stateMutability': 'view', 'type': 'function' 
        }
    ]";

    public async Task<decimal> GetPriceInEthAsync(string tokenAddress)
    {
        if (!chainlinkFeeds.TryGetValue(tokenAddress, out var feedAddress))
            throw new Exception("Missing Chainlink feed for token " + tokenAddress);

        var contract = web3.Eth.GetContract(Abi, feedAddress);
        var func = contract.GetFunction("latestAnswer");
        var result = await func.CallAsync<BigInteger>();

        decimal ethPrice = (decimal)result / (decimal)Math.Pow(10, 18); // assuming 18 decimals for ETH price

        return ethPrice;
    }
}