public class HealthFactor(ITokenPriceProvider priceProvider, AaveDataProvider dataProvider)
{
    public async Task<List<(string borrower, decimal healthFactor)>> GetBorrowersWithHealt(List<BorrowerAccount> borrowerAccs)
    {
        var borrowersWithHealth = new List<(string, decimal)>();

        foreach (var account in borrowerAccs)
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

            var hf = CalculateHealthFactor(input);
            borrowersWithHealth.Add((account.Id, hf));
        }

        return borrowersWithHealth;
    }

    public decimal CalculateHealthFactor(HealthFactorInput input)
    {
        decimal totalCollateralETH = input.Deposits
            .Where(d => d.IsActive && !d.IsFrozen && d.UsageAsCollateral)
            .Sum(d => d.Amount * d.PriceInEth * d.LiquidationThreshold);

        decimal totalBorrowedETH = input.Borrows
            .Where(b => b.IsActive && !b.IsFrozen)
            .Sum(b => b.Amount * b.PriceInEth);

        if (totalBorrowedETH == 0)
            return decimal.MaxValue;  // Fully safe

        decimal healthFactor = totalCollateralETH / totalBorrowedETH;

        return healthFactor;
    }
}

public class HealthFactorInput
{
    public List<TokenPosition> Deposits { get; set; } = new();
    public List<TokenPosition> Borrows { get; set; } = new();
}