using System.Numerics;
using Nethereum.Web3;

public class LiquidationMonitor(
    BorrowerFetcher borrowerFetcher,
    HealthFactor healthFactor,
    LiquidationExecutor liquidationExecutor,
    GasPriceManager gasManager,
    ProfitabilitySimulator profitabilitySimulator,
    DecimalsMap decimalsMap,
    Notifier notifier,
    int pageSize = 1000,
    int maxPages = 10,
    int refreshDelaySeconds = 60)
{
    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Liquidation Monitor...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"Fetching and ranking dangerous borrowers at {DateTime.UtcNow}...");

                var borrowers = await borrowerFetcher.FetchBorrowersAsync(pageSize, maxPages);
                var rankedBorrowers = borrowerFetcher.ParseTopBorrowers(borrowers);

                var healthFactors = await healthFactor.CalculateHealthFactorAsync(borrowers);

                if (rankedBorrowers.Count == 0)
                {
                    Console.WriteLine("No dangerous borrowers found.");
                }
                else
                {
                    Console.WriteLine("Top Dangerous Borrowers:");

                    foreach (var borrower in rankedBorrowers)
                    {
                        decimal debtETH = borrower.DebtAmount;
                        decimal health = healthFactors[borrower.Borrower];

                        Console.WriteLine($"{borrower.Borrower} | Debt: {debtETH:F4} ETH | Health: {health:F4}");

                        if (health >= 1.0m || debtETH <= 0.1m)
                        {
                            continue;
                        }

                        //bool gasOk = await _gasManager.IsGasCheapEnoughAsync();
                        bool gasOk = true; // For testing, assume gas is always ok
                        if (!gasOk)
                        {
                            Console.WriteLine("Gas price is too high, skipping liquidation.");
                            continue;
                        }

                        // Fetch debt and collateral assets for borrower
                        var borrowerReserveData = await borrowerFetcher.FetchBorrowerAccountByIdParsedAsync(borrower.Borrower);

                        if (borrowerReserveData == null)
                        {
                            Console.WriteLine($"Failed to fetch reserves for {borrower.Borrower}");
                            continue;
                        }

                        string debtAssetAddress = borrowerReserveData.DebtAssetAddress;
                        string collateralAssetAddress = borrowerReserveData.CollateralAssetAddress;

                        if (string.IsNullOrEmpty(debtAssetAddress) || string.IsNullOrEmpty(collateralAssetAddress))
                        {
                            Console.WriteLine($"Missing debt/collateral asset addresses for {borrower.Borrower}");
                            continue;
                        }

                        int debtAssetDecimals = await decimalsMap.GetDecimalsAsync(debtAssetAddress);

                        // Calculate exact debt to cover
                        BigInteger totalDebtWei = new BigInteger(borrowerReserveData.DebtAmount);
                        BigInteger debtToCoverWei;

                        if (health < 0.95m) // Assume close factor threshold is 0.95
                        {
                            debtToCoverWei = totalDebtWei; // 100% allowed
                        }
                        else
                        {
                            debtToCoverWei = totalDebtWei / 2; // 50% allowed
                        }

                        decimal debtToCover = Web3.Convert.FromWei(debtToCoverWei, debtAssetDecimals);

                        // Simulate profitability
                        bool isProfitable = await profitabilitySimulator.IsProfitableAsync(
                            debtToCover,
                            debtAssetDecimals,
                            collateralAssetAddress,
                            debtAssetAddress
                        );

                        if (!isProfitable)
                        {
                            Console.WriteLine("Not profitable, skipping liquidation.");
                            continue;
                        }

                        string alert = $"*Liquidation Opportunity!*\n{borrower.Borrower}\nDebt: {debtETH:F4} ETH\n❤Health Factor: {health:F4}";
                        await notifier.SendMessageAsync(alert);

                        await liquidationExecutor.TriggerLiquidationAsync(
                            borrower.Borrower,
                            debtAssetAddress,
                            collateralAssetAddress,
                            debtToCover,
                            debtAssetDecimals
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during monitoring: {ex.Message}");
            }

            Console.WriteLine($"Sleeping for {refreshDelaySeconds} seconds...\n");
            await Task.Delay(TimeSpan.FromSeconds(refreshDelaySeconds), cancellationToken);
        }
    }
}