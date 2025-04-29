using System.Numerics;
using Nethereum.Web3;

public class LiquidationMonitor
{
    private readonly BorrowerFetcher _borrowerFetcher;
    private readonly LiquidationSender _liquidationSender;
    private readonly GasPriceManager _gasManager;
    private readonly Notifier _notifier;
    private readonly int _pageSize;
    private readonly int _maxPages;
    private readonly int _refreshDelaySeconds;

    public LiquidationMonitor(
        BorrowerFetcher borrowerFetcher, 
        LiquidationSender liquidationSender,
        GasPriceManager gasManager,
        Notifier notifier, 
        int pageSize = 1000, 
        int maxPages = 10, 
        int refreshDelaySeconds = 60)
    {
        _borrowerFetcher = borrowerFetcher;
        _liquidationSender = liquidationSender;
        _gasManager = gasManager;
        _notifier = notifier;
        _pageSize = pageSize;
        _maxPages = maxPages;
        _refreshDelaySeconds = refreshDelaySeconds;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("🚀 Starting Liquidation Monitor...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"🔄 Fetching and ranking dangerous borrowers at {DateTime.UtcNow}...");
                var rankedBorrowers = await _borrowerFetcher.FetchAndRankDangerousBorrowersAsync(_pageSize, _maxPages);

                if (rankedBorrowers.Count == 0)
                {
                    Console.WriteLine("❌ No dangerous borrowers found.");
                }
                else
                {
                    Console.WriteLine($"🎯 Top Dangerous Borrowers:");

                    foreach (var borrower in rankedBorrowers)
                    {
                        decimal debtETH = Web3.Convert.FromWei(BigInteger.Parse(borrower.TotalDebtETH));
                        decimal health = Web3.Convert.FromWei(BigInteger.Parse(borrower.HealthFactor));

                        Console.WriteLine($"👤 {borrower.Id} | Debt: {debtETH:F4} ETH | Health: {health:F4}");

                        if (health < 1.0m && debtETH > 0.1m)
                        {
                            //bool gasOk = await _gasManager.IsGasCheapEnoughAsync();
                            bool gasOk = true; // For testing, assume gas is always ok
                            if (!gasOk)
                            {
                                Console.WriteLine("⏳ Gas price is too high, skipping liquidation.");
                                continue;
                            }

                            string alert = $"🚨 *Liquidation Opportunity!*\n👤 {borrower.Id}\n💰 Debt: {debtETH:F4} ETH\n❤️ Health Factor: {health:F4}";
                            await _notifier.SendMessageAsync(alert);
                        }

                        // Simulate profitability
                        var simulator = new ProfitabilitySimulator(
                            new Web3(Environment.GetEnvironmentVariable("RPC_URL_ARBITRUM")),
                            flashloanPremiumPercent: 0.09m,
                            liquidationBonusPercent: 5.0m,
                            slippagePercent: 1.0m,
                            gasPriceGwei: 30,
                            estimatedGasUnits: 400_000
                        );

                        string debtAssetAddress = Environment.GetEnvironmentVariable("DEBT_ASSET_ADDRESS");
                        string collateralAssetAddress = Environment.GetEnvironmentVariable("COLLATERAL_ASSET_ADDRESS");
                        decimal debtAssetDecimals = decimal.Parse(Environment.GetEnvironmentVariable("DEBT_ASSET_DECIMALS"));
                        decimal liquidationBonusPercent = decimal.Parse(Environment.GetEnvironmentVariable("LIQUIDATION_BONUS_PERCENT"));
                        decimal debtToCover = debtETH * (1 + liquidationBonusPercent / 100m);
                        bool = await simulator.IsProfitableAsync(
                            debtToCover,
                            debtAssetDecimals,
                            collateralAssetAddress,
                            debtAssetAddress,
                            Environment.GetEnvironmentVariable("DEX_ROUTER_ADDRESS")
                        );

                        await _liquidationSender.TriggerLiquidationAsync(
                            borrower.Id,
                            debtAssetAddress,       // e.g. USDC address
                            collateralAssetAddress, // e.g. WETH address
                            debtToCover,            // amount you want to repay (can be 50% of debt)
                            debtAssetDecimals       // 6 for USDC, 18 for DAI, etc.
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during monitoring: {ex.Message}");
            }

            Console.WriteLine($"⏳ Sleeping for {_refreshDelaySeconds} seconds...\n");
            await Task.Delay(TimeSpan.FromSeconds(_refreshDelaySeconds), cancellationToken);
        }
    }
}