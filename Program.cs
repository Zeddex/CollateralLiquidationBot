using System.Numerics;

var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
DotEnv.Load(dotenv);

var aaveMonitor = new AaveMonitor(
    Environment.GetEnvironmentVariable("RPC_URL_ARBITRUM"),
    "0x794a61358D6845594F94dc1DB02A252b5b4814aD", // Arbitrum Aave V3 LendingPool
    Environment.GetEnvironmentVariable("LIQUIDATION_CONTRACT_ARBITRUM")
);

//string borrowerAddress = "0xBorrowerAddress";
//await aaveMonitor.MonitorBorrowerAsync(borrowerAddress);

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

var monitor = new LiquidationMonitor(borrowerFetcher, notifier, pageSize: 1000, maxPages: 10, refreshDelaySeconds: 60);

var cancellationTokenSource = new CancellationTokenSource();

await monitor.StartMonitoringAsync(cancellationTokenSource.Token);

// cancellationTokenSource.Cancel(); // uncomment to stop monitoring