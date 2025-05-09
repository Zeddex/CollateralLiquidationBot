public class TokenPosition
{
    public string AssetAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }         // In token units (not wei)
    public decimal PriceInEth { get; set; }     // Price of asset in ETH
    public decimal LiquidationThreshold { get; set; }  // E.g., 0.8 = 80%
    public bool IsActive { get; set; } = true;
    public bool IsFrozen { get; set; } = false;
    public bool UsageAsCollateral { get; set; } = true;
}