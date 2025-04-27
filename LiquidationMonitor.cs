using System.Numerics;
using Nethereum.Web3;

public class LiquidationMonitor
{
    private readonly BorrowerFetcher _borrowerFetcher;
    private readonly int _pageSize;
    private readonly int _maxPages;
    private readonly int _refreshDelaySeconds;

    public LiquidationMonitor(BorrowerFetcher borrowerFetcher, int pageSize = 1000, int maxPages = 10, int refreshDelaySeconds = 60)
    {
        _borrowerFetcher = borrowerFetcher;
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

                        // Optional future: trigger flashloan here if health < 1.0
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