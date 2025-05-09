using System.Numerics;
using Nethereum.Web3;

public class LiquidationMonitor
{
    private readonly BorrowerFetcher _borrowerFetcher;
    private readonly LiquidationExecutor _liquidationExecutor;
    private readonly GasPriceManager _gasManager;
    private readonly ProfitabilitySimulator _profitabilitySimulator;
    private readonly DecimalsMap _decimalsMap;
    private readonly Notifier _notifier;
    private readonly int _pageSize;
    private readonly int _maxPages;
    private readonly int _refreshDelaySeconds;

    public LiquidationMonitor(
        BorrowerFetcher borrowerFetcher, 
        LiquidationExecutor liquidationExecutor,
        GasPriceManager gasManager,
        ProfitabilitySimulator profitabilitySimulator,
        DecimalsMap decimalsMap,
        Notifier notifier, 
        int pageSize = 1000, 
        int maxPages = 10, 
        int refreshDelaySeconds = 60)
    {
        _borrowerFetcher = borrowerFetcher;
        _liquidationExecutor = liquidationExecutor;
        _gasManager = gasManager;
        _profitabilitySimulator = profitabilitySimulator;
        _decimalsMap = decimalsMap;
        _notifier = notifier;
        _pageSize = pageSize;
        _maxPages = maxPages;
        _refreshDelaySeconds = refreshDelaySeconds;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Liquidation Monitor...");

        //while (!cancellationToken.IsCancellationRequested)
        //{
        //    try
        //    {
        //        Console.WriteLine($"Fetching and ranking dangerous borrowers at {DateTime.UtcNow}...");
        //        var rankedBorrowers = await _borrowerFetcher.FetchAndRankDangerousBorrowersAsync(_pageSize, _maxPages);

        //        if (rankedBorrowers.Count == 0)
        //        {
        //            Console.WriteLine("No dangerous borrowers found.");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Top Dangerous Borrowers:");

        //            foreach (var borrower in rankedBorrowers)
        //            {
        //                decimal debtETH = Web3.Convert.FromWei(BigInteger.Parse(borrower.TotalDebtETH));
        //                decimal health = Web3.Convert.FromWei(BigInteger.Parse(borrower.HealthFactor));

        //                Console.WriteLine($"{borrower.Id} | Debt: {debtETH:F4} ETH | Health: {health:F4}");

        //                if (health >= 1.0m || debtETH <= 0.1m)
        //                {
        //                    continue;
        //                }

        //                //bool gasOk = await _gasManager.IsGasCheapEnoughAsync();
        //                bool gasOk = true; // For testing, assume gas is always ok
        //                if (!gasOk)
        //                {
        //                    Console.WriteLine("Gas price is too high, skipping liquidation.");
        //                    continue;
        //                }

        //                // Fetch debt and collateral assets for borrower
        //                var borrowerReserveData = await _borrowerFetcher.FetchBorrowerReserveDataAsync(borrower.Id);

        //                if (borrowerReserveData == null)
        //                {
        //                    Console.WriteLine($"Failed to fetch reserves for {borrower.Id}");
        //                    continue;
        //                }

        //                string debtAssetAddress = borrowerReserveData.DebtAssetAddress;
        //                string collateralAssetAddress = borrowerReserveData.CollateralAssetAddress;

        //                if (string.IsNullOrEmpty(debtAssetAddress) || string.IsNullOrEmpty(collateralAssetAddress))
        //                {
        //                    Console.WriteLine($"Missing debt/collateral asset addresses for {borrower.Id}");
        //                    continue;
        //                }

        //                int debtAssetDecimals = await _decimalsMap.GetDecimalsAsync(debtAssetAddress);

        //                // Calculate exact debt to cover
        //                BigInteger totalDebtWei = borrowerReserveData.TotalDebtWei;
        //                BigInteger debtToCoverWei;

        //                if (health < 0.95m) // Assume close factor threshold is 0.95
        //                {
        //                    debtToCoverWei = totalDebtWei; // 100% allowed
        //                }
        //                else
        //                {
        //                    debtToCoverWei = totalDebtWei / 2; // 50% allowed
        //                }

        //                decimal debtToCover = Web3.Convert.FromWei(debtToCoverWei, debtAssetDecimals);

        //                // Simulate profitability
        //                bool isProfitable = await _profitabilitySimulator.IsProfitableAsync(
        //                    debtToCover,
        //                    debtAssetDecimals,
        //                    collateralAssetAddress,
        //                    debtAssetAddress
        //                );

        //                if (!isProfitable)
        //                {
        //                    Console.WriteLine("❌ Not profitable, skipping liquidation.");
        //                    continue;
        //                }

        //                string alert = $"*Liquidation Opportunity!*\n{borrower.Id}\nDebt: {debtETH:F4} ETH\n❤Health Factor: {health:F4}";
        //                await _notifier.SendMessageAsync(alert);

        //                await _liquidationExecutor.TriggerLiquidationAsync(
        //                    borrower.Id,
        //                    debtAssetAddress,
        //                    collateralAssetAddress,
        //                    debtToCover,
        //                    debtAssetDecimals
        //                );
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error during monitoring: {ex.Message}");
        //    }

        //    Console.WriteLine($"Sleeping for {_refreshDelaySeconds} seconds...\n");
        //    await Task.Delay(TimeSpan.FromSeconds(_refreshDelaySeconds), cancellationToken);
        //}
    }
}