using Nethereum.Web3;
using System.Numerics;

var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
DotEnv.Load(dotenv);

var web3 = new Web3(Environment.GetEnvironmentVariable("RPC_URL_ARBITRUM"));

var rpcMap = new Dictionary<ChainId, string>
{
    { ChainId.Ethereum, "https://eth-mainnet.g.alchemy.com/v2/YOUR_KEY" },
    { ChainId.Polygon, "https://polygon-rpc.com" }
};

var decimalsMap = new DecimalsMap(rpcMap);

// Get decimals for USDC on Polygon
int usdcDecimals = await decimalsMap.GetDecimalsAsync(
    ChainId.Polygon,
    "0x2791Bca1f2de4661ED88A30C99A7a9449Aa84174"
);

var aaveMonitor = new AaveMonitor(
    Environment.GetEnvironmentVariable("RPC_URL_ETHEREUM"),
    Environment.GetEnvironmentVariable("AAVE_POOL_ADDRESS_ETHEREUM"),
    Environment.GetEnvironmentVariable("LIQUIDATION_CONTRACT_ETHEREUM")
);

string borrowerAddress = "0xaf5c88245cd02ff3df332ef1e1ffd5bc5d1d87cd";
await aaveMonitor.MonitorBorrowerAsync(borrowerAddress);

//----------------------------------------------

//var borrowerFetcher = new BorrowerFetcher("https://api.thegraph.com/subgraphs/name/aave/protocol-v3");

//var rankedBorrowers = await borrowerFetcher.FetchAndRankDangerousBorrowersAsync(1000, 10);

//foreach (var borrower in rankedBorrowers)
//{
//    decimal debtETH = Web3.Convert.FromWei(BigInteger.Parse(borrower.TotalDebtETH));
//    decimal health = Web3.Convert.FromWei(BigInteger.Parse(borrower.HealthFactor));

//    Console.WriteLine($"{borrower.Id} | Debt: {debtETH:F4} ETH | Health: {health:F4}");
//}

//----------------------------------------------

var borrowerFetcher = new BorrowerFetcher("https://api.thegraph.com/subgraphs/name/aave/protocol-v3");
var notifier = new Notifier();
var gasManager = new GasPriceManager(web3, 50); // Only liquidate if gas <= 50 gwei
var liquidationSender = new LiquidationSender(
    Environment.GetEnvironmentVariable("RPC_URL_ARBITRUM"),
    Environment.GetEnvironmentVariable("LIQUIDATION_CONTRACT_ARBITRUM"),
    Environment.GetEnvironmentVariable("PRIVATE_KEY")
);

var monitor = new LiquidationMonitor(borrowerFetcher, liquidationSender, gasManager, notifier, pageSize: 1000, maxPages: 10, refreshDelaySeconds: 60);

var cancellationTokenSource = new CancellationTokenSource();

await monitor.StartMonitoringAsync(cancellationTokenSource.Token);

// cancellationTokenSource.Cancel(); // uncomment to stop monitoring