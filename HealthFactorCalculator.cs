public class TokenPosition
{
    public string AssetAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }         // In token units (not wei)
    public decimal PriceInEth { get; set; }     // Price of asset in ETH
    public decimal LiquidationThreshold { get; set; }  // E.g., 0.8 = 80%
}

public class HealthFactorInput
{
    public List<TokenPosition> Deposits { get; set; } = new();
    public List<TokenPosition> Borrows { get; set; } = new();
}

public static class HealthFactorCalculator
{
    public static decimal CalculateHealthFactor(HealthFactorInput input)
    {
        decimal totalCollateralETH = input.Deposits.Sum(d =>
            d.Amount * d.PriceInEth * d.LiquidationThreshold);

        decimal totalBorrowedETH = input.Borrows.Sum(b =>
            b.Amount * b.PriceInEth);

        if (totalBorrowedETH == 0)
            return decimal.MaxValue;  // Fully safe

        return totalCollateralETH / totalBorrowedETH;
    }
}