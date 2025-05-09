using Nethereum.Web3;

var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
DotEnv.Load(dotenv);

string privateKey = Environment.GetEnvironmentVariable("WALLET_PRIVATE_KEY");

string? rpcArbitrum = Environment.GetEnvironmentVariable("RPC_URL_ARBITRUM");
string? rpcEthereum = Environment.GetEnvironmentVariable("RPC_URL_ETHEREUM");
string? rpcPolygon = Environment.GetEnvironmentVariable("RPC_URL_POLYGON");

string aavePoolArbitrum = "0x7d2768D9B4B3C2C6eA1D5b8E2Ff3A4cA0E5aFf0B";
string aavePoolEthereum = "0x87870Bca3F3fD6335C3F4ce8392D69350B4fA4E2";
string aavePoolPolygon = "0x7d2768D9B4B3C2C6eA1D5b8E2Ff3A4cA0E5aFf0B";

string aaveDataProviderArbitrum = "0x14496b405D62c24F91f04Cda1c69Dc526D56fDE5";
string aaveDataProviderEthereum = "0x497a1994c46d4f6C864904A9f1fac6328Cb7C8a6";
string aaveDataProviderPolygon = "0x14496b405D62c24F91f04Cda1c69Dc526D56fDE5";

string liquidationContractArbitrum = Environment.GetEnvironmentVariable("LIQUIDATION_CONTRACT_ARBITRUM");
//string liquidationContractEthereum = Environment.GetEnvironmentVariable("LIQUIDATION_CONTRACT_ETHEREUM");
//string liquidationContractPolygon = Environment.GetEnvironmentVariable("LIQUIDATION_CONTRACT_POLYGON");

string thegraphApiKey = Environment.GetEnvironmentVariable("THEGRAPH_API_KEY");

var web3Arb = new Web3(rpcArbitrum);
var web3Eth = new Web3(rpcEthereum);
var web3Pol = new Web3(rpcPolygon);

var rpcMap = new Dictionary<ChainId, string>
{
    { ChainId.Arbitrum, rpcArbitrum },
    { ChainId.Ethereum, rpcEthereum },
    { ChainId.Polygon, rpcPolygon }
};

var decimalsMap = new DecimalsMap(rpcMap, ChainId.Arbitrum);

string aaveV3ArbitrumSubgraphUrl = $"https://gateway.thegraph.com/api/{thegraphApiKey}/subgraphs/id/4xyasjQeREe7PxnF6wVdobZvCw5mhoHZq3T7guRpuNPf"; // https://thegraph.com/explorer/subgraphs/4xyasjQeREe7PxnF6wVdobZvCw5mhoHZq3T7guRpuNPf
string aaveV3EthereumSubgraphUrl = $"https://gateway.thegraph.com/api/{thegraphApiKey}/subgraphs/id/JCNWRypm7FYwV8fx5HhzZPSFaMxgkPuw4TnR3Gpi81zk"; // https://thegraph.com/explorer/subgraphs/JCNWRypm7FYwV8fx5HhzZPSFaMxgkPuw4TnR3Gpi81zk
string aaveV3PolygonSubgraphUrl = $"https://gateway.thegraph.com/api/{thegraphApiKey}/subgraphs/id/6yuf1C49aWEscgk5n9D1DekeG1BCk5Z9imJYJT3sVmAT"; // https://thegraph.com/explorer/subgraphs/6yuf1C49aWEscgk5n9D1DekeG1BCk5Z9imJYJT3sVmAT

var priceProvider = new ChainlinkPriceProvider(web3Eth, new Dictionary<string, string> {
    { "0x7Fc66500c84A76Ad7e9c93437bFc5Ac33E2DDaE9", "0x6Df09E975c830ECae5bd4eD9d90f3A95a4f88012" }, // AAVE → AAVE/ETH Chainlink feed
    { "0x2260FAC5E5542a773Aa44fBCfeDf7C193bc2C599", "0xdeb288F737066589598e9214E782fa5A8eD689e8" }, // WBTC → BTC/ETH Chainlink feed
    { "0x6B175474E89094C44Da98b954EedeAC495271d0F", "0x773616E4d11A78F511299002da57A0a94577F1f4" }, // DAI → DAI/ETH Chainlink feed
    { "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48", "0x986b5E1e1755e3C2440e960477f25201B0a8bbD4" }, // USDC → USDC/ETH Chainlink feed
    { "0xdAC17F958D2ee523a2206206994597C13D831ec7", "0xEe9F2375b4bdF6387aa8265dD4FB8F16512A1d46" }, // USDT → USDT/ETH Chainlink feed
});

//decimal aaveEthPrice = await priceProvider.GetPriceInEthAsync("0x7Fc66500c84A76Ad7e9c93437bFc5Ac33E2DDaE9");

var dataProvider = new AaveDataProvider(web3Eth, aaveDataProviderEthereum);

var fetcher = new BorrowerFetcher(aaveV3EthereumSubgraphUrl);

//var borrowers = await fetcher.FetchSortedBorrowersAsync();
//foreach (var b in borrowers)
//{
//    Console.WriteLine($"Borrower {b.Borrower} | Owes {b.DebtAmount} {b.DebtAssetSymbol}, collateral {b.CollateralAmount} {b.CollateralAssetSymbol}");
//}

var notifier = new Notifier();

var gasManager = new GasPriceManager(web3Arb, 10); // Only liquidate if gas <= 10 gwei

string routerAddress = Dex.GetRouterV2Address(ChainId.Arbitrum);

var profitabilitySimulator = new ProfitabilitySimulator(
    web3Arb,
    routerAddress,
    flashloanPremiumPercent: 0.09m,
    liquidationBonusPercent: 5.0m,
    slippagePercent: 1.0m,
    gasPriceGwei: 1,
    estimatedGasUnits: 500_000
);

var liquidationExecutor = new LiquidationExecutor(
    web3Arb,
    privateKey,
    liquidationContractArbitrum
);

var monitor = new LiquidationMonitor(
    fetcher,
    liquidationExecutor,
    gasManager,
    profitabilitySimulator,
    decimalsMap,
    notifier,
    pageSize: 1000,
    maxPages: 10,
    refreshDelaySeconds: 60
);

var cancellationTokenSource = new CancellationTokenSource();

await monitor.StartMonitoringAsync(cancellationTokenSource.Token);

// cancellationTokenSource.Cancel(); // uncomment to stop monitoring

//----------------------------------------------
//----------------------------------------------

//var aaveMonitor = new AavePool(
//    web3Eth,
//    liquidationExecutor,
//    aavePoolEthereum
//);
//string borrowerAddress = "0x3b5656b74a07c5d6e1da6317ac0d1a745929c16d";
//await aaveMonitor.MonitorBorrowerAsync(borrowerAddress);

//----------------------------------------------

//var input = new HealthFactorInput
//{
//    Deposits = new List<TokenPosition>
//    {
//        new TokenPosition { AssetAddress = "0xAsset1", Amount = 10m, PriceInEth = 0.05m, LiquidationThreshold = 0.8m }, // $400
//        new TokenPosition { AssetAddress = "0xAsset2", Amount = 5m, PriceInEth = 0.1m, LiquidationThreshold = 0.85m }   // $425
//    },
//    Borrows = new List<TokenPosition>
//    {
//        new TokenPosition { AssetAddress = "0xDebt1", Amount = 700m, PriceInEth = 0.00142857m } // $1000
//    }
//};

//var healthFactor = HealthFactorCalculator.CalculateHealthFactor(input);
//Console.WriteLine($"Health Factor: {healthFactor:F4}");

//----------------------------------------------