using System.Numerics;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

public class BorrowerFetcher
{
    private readonly GraphQLHttpClient _graphQLClient;

    public BorrowerFetcher(string subgraphUrl)
    {
        _graphQLClient = new GraphQLHttpClient(subgraphUrl, new NewtonsoftJsonSerializer());
    }

    public async Task<List<string>> FetchBorrowersAsync(int limit = 1000)
    {
        var query = new GraphQLRequest
        {
            Query = @"
            {
              users(where: {borrowedReservesCount_gt: 0}, first: " + limit + @") {
                id
              }
            }"
        };

        var response = await _graphQLClient.SendQueryAsync<BorrowerResponse>(query);

        var borrowerAddresses = new List<string>();
        foreach (var user in response.Data.Users)
        {
            borrowerAddresses.Add(user.Id);
        }

        return borrowerAddresses;
    }

    public async Task<List<string>> FetchBorrowersPaginatedAsync(int pageSize = 1000, int maxPages = 10)
    {
        var allBorrowers = new List<string>();

        for (int page = 0; page < maxPages; page++)
        {
            int skip = page * pageSize;

            var query = new GraphQLRequest
            {
                Query = @"
                {
                  users(where: {borrowedReservesCount_gt: 0}, first: " + pageSize + @", skip: " + skip + @") {
                    id
                  }
                }"
            };

            var response = await _graphQLClient.SendQueryAsync<BorrowerResponse>(query);

            var borrowers = new List<string>();
            foreach (var user in response.Data.Users)
            {
                borrowers.Add(user.Id);
            }

            if (borrowers.Count == 0)
            {
                Console.WriteLine($"All borrowers fetched. Total: {allBorrowers.Count}");
                break;
            }

            allBorrowers.AddRange(borrowers);

            Console.WriteLine($"Page {page + 1}: Fetched {borrowers.Count} borrowers");
        }

        return allBorrowers;
    }

    public async Task<List<DangerousUser>> FetchAndRankDangerousBorrowersAsync(int pageSize = 1000, int maxPages = 10)
    {
        var allBorrowers = new List<DangerousUser>();

        for (int page = 0; page < maxPages; page++)
        {
            int skip = page * pageSize;

            var query = new GraphQLRequest
            {
                Query = @"
                {
                  users(
                    where: {
                      borrowedReservesCount_gt: 0, 
                      healthFactor_lt: ""1100000000000000000""
                    },
                    first: " + pageSize + @",
                    skip: " + skip + @"
                  ) {
                    id
                    healthFactor
                    totalDebtETH
                  }
                }"
            };

            var response = await _graphQLClient.SendQueryAsync<DangerousBorrowerResponse>(query);

            var borrowers = response.Data.Users;

            if (borrowers.Count == 0)
            {
                break;
            }

            allBorrowers.AddRange(borrowers);

            Console.WriteLine($"Page {page + 1}: Fetched {borrowers.Count} dangerous borrowers");
        }

        var rankedBorrowers = allBorrowers
            .OrderByDescending(user => BigInteger.Parse(user.TotalDebtETH))
            .ThenBy(user => BigInteger.Parse(user.HealthFactor))
            .ToList();

        return rankedBorrowers;
    }
}

public class BorrowerResponse
{
    public List<User> Users { get; set; }
}

public class User
{
    public string Id { get; set; }
}

public class DangerousBorrowerResponse
{
    public List<DangerousUser> Users { get; set; }
}

public class DangerousUser
{
    public string Id { get; set; }
    public string HealthFactor { get; set; }
    public string TotalDebtETH { get; set; }
}