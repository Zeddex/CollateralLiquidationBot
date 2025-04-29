using Nethereum.Web3;

public class DecimalsMap
{
    private readonly Dictionary<ChainId, Web3> _web3Clients;
    private readonly Dictionary<ChainId, Dictionary<string, int>> _chainDecimals;

    private const string ERC20_ABI = @"[
        { 'constant': true, 'inputs': [], 'name': 'decimals', 'outputs': [{ 'name': '', 'type': 'uint8' }], 'payable': false, 'stateMutability': 'view', 'type': 'function' }
    ]";

    public DecimalsMap(Dictionary<ChainId, string> rpcUrls)
    {
        _web3Clients = new Dictionary<ChainId, Web3>();
        _chainDecimals = new Dictionary<ChainId, Dictionary<string, int>>();

        foreach (var entry in rpcUrls)
        {
            _web3Clients[entry.Key] = new Web3(entry.Value);
            _chainDecimals[entry.Key] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        SeedKnownDecimals();
    }

    private void SeedKnownDecimals()
    {
        _chainDecimals[ChainId.Ethereum]["0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606EB48"] = 6;  // USDC
        _chainDecimals[ChainId.Ethereum]["0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2"] = 18; // WETH

        _chainDecimals[ChainId.Polygon]["0x2791Bca1f2de4661ED88A30C99A7a9449Aa84174"] = 6;  // USDC on Polygon
        _chainDecimals[ChainId.Polygon]["0x7ceB23fD6bC0adD59E62ac25578270cFf1b9f619"] = 18; // WETH on Polygon
        // Add more as needed...
    }

    public async Task<int> GetDecimalsAsync(ChainId chainId, string tokenAddress)
    {
        if (_chainDecimals.TryGetValue(chainId, out var tokenMap) &&
            tokenMap.TryGetValue(tokenAddress, out var knownDecimals))
        {
            return knownDecimals;
        }

        // fallback to on-chain fetch
        try
        {
            var web3 = _web3Clients[chainId];
            var contract = web3.Eth.GetContract(ERC20_ABI, tokenAddress);
            var func = contract.GetFunction("decimals");
            var result = await func.CallAsync<byte>();
            int decimals = result;

            // cache it
            _chainDecimals[chainId][tokenAddress] = decimals;

            Console.WriteLine($"🔍 Decimals fetched on {chainId} for {tokenAddress}: {decimals}");
            return decimals;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to fetch decimals for {tokenAddress} on {chainId}: {ex.Message}");
            throw;
        }
    }
}
