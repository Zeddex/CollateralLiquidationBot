using Nethereum.Web3;

public class DecimalsMap
{
    private readonly Dictionary<ChainId, Web3> _web3Clients;
    private readonly Dictionary<ChainId, Dictionary<string, int>> _chainDecimals;

    public ChainId CurrentNetwork { get; set; }

    private const string ERC20_ABI = @"[
        { 
            'constant': true, 
            'inputs': [], 
            'name': 'decimals', 
            'outputs': [
                { 'name': '', 'type': 'uint8' }
            ], 
            'payable': false, 
            'stateMutability': 'view', 
            'type': 'function' }
    ]";

    public DecimalsMap(Dictionary<ChainId, string> rpcUrls, ChainId currentNetwork)
    {
        _web3Clients = new Dictionary<ChainId, Web3>();
        _chainDecimals = new Dictionary<ChainId, Dictionary<string, int>>();

        CurrentNetwork = currentNetwork;

        foreach (var entry in rpcUrls)
        {
            _web3Clients[entry.Key] = new Web3(entry.Value);
            _chainDecimals[entry.Key] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        SeedKnownDecimals();
    }

    private void SeedKnownDecimals()
    {
        _chainDecimals[ChainId.Ethereum]["0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"] = 6; // USDC
        _chainDecimals[ChainId.Ethereum]["0xdAC17F958D2ee523a2206206994597C13D831ec7"] = 6; // USDT
        _chainDecimals[ChainId.Ethereum]["0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2"] = 18; // WETH
        _chainDecimals[ChainId.Ethereum]["0x6B175474E89094C44Da98b954EedeAC495271d0F"] = 18; // DAI
        _chainDecimals[ChainId.Ethereum]["0x2260FAC5E5542a773Aa44fBCfeDf7C193bc2C599"] = 8; // WBTC

        _chainDecimals[ChainId.Arbitrum]["0xaf88d065e77c8cC2239327C5EDb3A432268e5831"] = 6; // USDC on Arbitrum
        _chainDecimals[ChainId.Arbitrum]["0xFd086bC7CD5C481DCC9C85ebE478A1C0b69FCbb9"] = 6; // USDT0 on Arbitrum
        _chainDecimals[ChainId.Arbitrum]["0x82aF49447D8a07e3bd95BD0d56f35241523fBab1"] = 18; // WETH on Arbitrum
        _chainDecimals[ChainId.Arbitrum]["0xDA10009cBd5D07dd0CeCc66161FC93D7c9000da1"] = 18; // DAI on Arbitrum
        _chainDecimals[ChainId.Arbitrum]["0x2f2a2543B76A4166549F7aaB2e75Bef0aefC5B0f"] = 8; // WBTC on Arbitrum

        _chainDecimals[ChainId.Polygon]["0x3c499c542cEF5E3811e1192ce70d8cC03d5c3359"] = 6; // USDC on Polygon
        _chainDecimals[ChainId.Polygon]["0xc2132D05D31c914a87C6611C10748AEb04B58e8F"] = 6; // USDT on Polygon
        _chainDecimals[ChainId.Polygon]["0x7ceB23fD6bC0adD59E62ac25578270cFf1b9f619"] = 18; // WETH on Polygon
        _chainDecimals[ChainId.Polygon]["0x8f3Cf7ad23Cd3CaDbD9735AFf958023239c6A063"] = 18; // DAI on Polygon
        _chainDecimals[ChainId.Polygon]["0x1BFD67037B42Cf73acF2047067bd4F2C47D9BfD6"] = 8; // WBTC on Polygon
    }

    public async Task<int> GetDecimalsAsync(string tokenAddress)
    {
        if (_chainDecimals.TryGetValue(CurrentNetwork, out var tokenMap) &&
            tokenMap.TryGetValue(tokenAddress, out var knownDecimals))
        {
            return knownDecimals;
        }

        // fallback to on-chain fetch
        try
        {
            var web3 = _web3Clients[CurrentNetwork];
            var contract = web3.Eth.GetContract(ERC20_ABI, tokenAddress);
            var func = contract.GetFunction("decimals");
            var result = await func.CallAsync<byte>();
            int decimals = result;

            // cache it
            _chainDecimals[CurrentNetwork][tokenAddress] = decimals;

            Console.WriteLine($"Decimals fetched on {CurrentNetwork} for {tokenAddress}: {decimals}");
            return decimals;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to fetch decimals for {tokenAddress} on {CurrentNetwork}: {ex.Message}");
            throw;
        }
    }
}
