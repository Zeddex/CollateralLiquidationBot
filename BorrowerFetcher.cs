using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

public class BorrowerFetcher(string subgraphUrl)
{
    private readonly GraphQLHttpClient _client = new(subgraphUrl, new NewtonsoftJsonSerializer());

    public async Task<List<BorrowerReserveData>> FetchSortedBorrowersAsync(int pageSize = 1000, int maxPages = 10)
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

    public async Task<List<BorrowerAccount>> FetchBorrowersAsync(int pageSize = 100, int maxPages = 10)
    {
        var borrowerAccs = new List<BorrowerAccount>();

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

            if (accounts.Count == 0) break;

            borrowerAccs.AddRange(accounts);
        }

        return borrowerAccs;
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
