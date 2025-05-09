using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

public class BorrowerFetcher(string subgraphUrl, ITokenPriceProvider priceProvider, AaveDataProvider dataProvider)
{
    private readonly GraphQLHttpClient _client = new(subgraphUrl, new NewtonsoftJsonSerializer());

    public async Task<List<(string borrower, decimal healthFactor)>> FetchBorrowersWithHealthAsync(int pageSize = 100, int maxPages = 10)
    {
        var results = new List<(string, decimal)>();

        for (int page = 0; page < maxPages; page++)
        {
            int skip = page * pageSize;

            var query = new GraphQLRequest
            {
                Query = @"
                {
                  accounts(
                    where: {borrows_: {amount_gt: ""0""}},
                    first: " + pageSize + @",
                    skip: " + skip + @"
                  ) {
                    id
                    borrows {
                      amount
                      asset {
                        id
                        symbol
                        decimals
                      }
                    }
                    deposits {
                      amount
                      asset {
                        id
                        symbol
                        decimals
                      }
                    }
                  }
                }"
            };

            var response = await _client.SendQueryAsync<AccountResponse>(query);
            if (response.Data.Accounts.Count == 0) break;

            foreach (var account in response.Data.Accounts)
            {
                var input = new HealthFactorInput
                {
                    Deposits = new List<TokenPosition>(),
                    Borrows = new List<TokenPosition>()
                };

                foreach (var deposit in account.Deposits)
                {
                    if (!decimal.TryParse(deposit.Amount, out var amt) || amt == 0) continue;

                    var price = await priceProvider.GetPriceInEthAsync(deposit.Asset.Id);
                    var reserveConfigData = await dataProvider.GetReserveConfigDataAsync(deposit.Asset.Id);

                    input.Deposits.Add(new TokenPosition
                    {
                        AssetAddress = deposit.Asset.Id,
                        Amount = amt,
                        PriceInEth = price,
                        LiquidationThreshold = (decimal)reserveConfigData.LiquidationThreshold / 10000m, // e.g., 8250 → 0.825
                        IsActive = reserveConfigData.IsActive,
                        IsFrozen = reserveConfigData.IsFrozen,
                        UsageAsCollateral = reserveConfigData.UsageAsCollateralEnabled
                    });
                }

                foreach (var borrow in account.Borrows)
                {
                    if (!decimal.TryParse(borrow.Amount, out var amt) || amt == 0) continue;

                    var price = await priceProvider.GetPriceInEthAsync(borrow.Asset.Id);

                    input.Borrows.Add(new TokenPosition
                    {
                        AssetAddress = borrow.Asset.Id,
                        Amount = amt,
                        PriceInEth = price,
                        LiquidationThreshold = 0 // not used for borrows
                    });
                }

                var hf = HealthFactorCalculator.CalculateHealthFactor(input);
                results.Add((account.Id, hf));
            }
        }

        return results;
    }

    public async Task<List<BorrowerReserveData>> FetchBorrowersAsync(int pageSize = 1000, int maxPages = 10)
    {
        Console.WriteLine("Fetching data from subgraph...");

        var allBorrowers = new List<BorrowerReserveData>();

        for (int page = 0; page < maxPages; page++)
        {
            int skip = page * pageSize;

            var query = new GraphQLRequest
            {
                Query = @"
                {
                  accounts(
                    where: {borrows_: {amount_gt: ""0""}},
                    first: " + pageSize + @",
                    skip: " + skip + @"
                  ) {
                    id
                    borrows {
                      amount
                      asset {
                        id
                        symbol
                        decimals
                      }
                    }
                    deposits {
                      amount
                      asset {
                        id
                        symbol
                        decimals
                      }
                    }
                  }
                }"
            };

            var response = await _client.SendQueryAsync<AccountResponse>(query);

            var accounts = response.Data.Accounts;

            if (accounts.Count == 0)
                break;

            foreach (var account in accounts)
            {
                var parsed = ParseAccount(account);
                if (parsed != null)
                    allBorrowers.Add(parsed);
            }

            //Console.WriteLine($"Page {page + 1}: {accounts.Count} borrowers with debt");
        }

        return allBorrowers;
    }

    private BorrowerReserveData? ParseAccount(BorrowerAccount account)
    {
        if (account.Borrows == null || account.Deposits == null)
            return null;

        var bestDebt = account.Borrows
            .OrderByDescending(b => decimal.TryParse(b.Amount, out var amt) ? amt : 0)
            .FirstOrDefault();

        var bestCollateral = account.Deposits
            .OrderByDescending(d => decimal.TryParse(d.Amount, out var amt) ? amt : 0)
            .FirstOrDefault();

        if (bestDebt == null || bestCollateral == null)
            return null;

        return new BorrowerReserveData
        {
            Borrower = account.Id,
            DebtAssetAddress = bestDebt.Asset.Id,
            CollateralAssetAddress = bestCollateral.Asset.Id,
            DebtAssetSymbol = bestDebt.Asset.Symbol,
            CollateralAssetSymbol = bestCollateral.Asset.Symbol,
            DebtAmount = decimal.Parse(bestDebt.Amount),
            CollateralAmount = decimal.Parse(bestCollateral.Amount),
            DebtAssetDecimals = bestDebt.Asset.Decimals,
            CollateralAssetDecimals = bestCollateral.Asset.Decimals
        };
    }
}

public class AccountResponse
{
    public List<BorrowerAccount> Accounts { get; set; }
}

public class BorrowerAccount
{
    public string Id { get; set; }
    public List<Borrow> Borrows { get; set; }
    public List<Deposit> Deposits { get; set; }
}

public class Borrow
{
    public string Amount { get; set; }
    public Asset Asset { get; set; }
}

public class Deposit
{
    public string Amount { get; set; }
    public Asset Asset { get; set; }
}

public class Asset
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}

public class BorrowerReserveData
{
    public string Borrower { get; set; }
    public string DebtAssetAddress { get; set; }
    public string CollateralAssetAddress { get; set; }
    public string DebtAssetSymbol { get; set; }
    public string CollateralAssetSymbol { get; set; }
    public decimal DebtAmount { get; set; }
    public decimal CollateralAmount { get; set; }
    public int DebtAssetDecimals { get; set; }
    public int CollateralAssetDecimals { get; set; }

}
