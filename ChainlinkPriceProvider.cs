using System.Numerics;
using Nethereum.Web3;

public class ChainlinkPriceProvider : ITokenPriceProvider
{
    private readonly Web3 _web3;
    private readonly Dictionary<string, string> _chainlinkFeeds;
    private const string AggregatorAbi = @"[{ 'inputs': [], 'name': 'latestAnswer', 'outputs': [{ 'internalType': 'int256', 'name': '', 'type': 'int256' }], 'stateMutability': 'view', 'type': 'function' }]";

    public ChainlinkPriceProvider(Web3 web3, Dictionary<string, string> chainlinkFeeds)
    {
        _web3 = web3;
        _chainlinkFeeds = chainlinkFeeds;
    }

    public async Task<decimal> GetPriceInEthAsync(string tokenAddress)
    {
        if (!_chainlinkFeeds.TryGetValue(tokenAddress, out var feedAddress))
            throw new Exception("Missing Chainlink feed for token " + tokenAddress);

        var contract = _web3.Eth.GetContract(AggregatorAbi, feedAddress);
        var func = contract.GetFunction("latestAnswer");
        var result = await func.CallAsync<BigInteger>();

        return (decimal)result / (decimal)Math.Pow(10, 18); // assuming 18 decimals for ETH price
    }
}