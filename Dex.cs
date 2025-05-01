public static class Dex
{
    private static List<(ChainId network, string routerV2)> Dexes { get; set; } =
    [
        (ChainId.Arbitrum, "0x4752ba5dbc23f44d87826276bf6fd6b1c372ad24"), // Uniswap V2
        (ChainId.Ethereum, "0x7a250d5630B4cF539739dF2C5dAcb4c659F2488D"), // Uniswap V2
        (ChainId.Polygon, "0xedf6066a2b290C185783862C7F4776A2C8077AD1"), // Uniswap V2
    ];

    public static string GetRouterV2Address(ChainId network)
    {
        var dex = Dexes.FirstOrDefault(d => d.network == network);

        return dex.routerV2;
    }
}