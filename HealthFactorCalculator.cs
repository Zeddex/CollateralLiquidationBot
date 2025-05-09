public class HealthFactorInput
{
    public List<TokenPosition> Deposits { get; set; } = new();
    public List<TokenPosition> Borrows { get; set; } = new();
}

public static class HealthFactorCalculator
{
    public static decimal CalculateHealthFactor(HealthFactorInput input)
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