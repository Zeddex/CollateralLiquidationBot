using System.Numerics;
using Nethereum.Web3;
using Nethereum.Util;

public class ProfitabilitySimulator
{
    private readonly Web3 _web3;
    private readonly decimal _flashloanPremiumPercent;
    private readonly decimal _liquidationBonusPercent;
    private readonly decimal _slippagePercent;
    private readonly decimal _gasPriceGwei;
    private readonly long _estimatedGasUnits;

    private static readonly string _routerV2Abi = "[{\"name\":\"getAmountsOut\",\"type\":\"function\",\"stateMutability\":\"view\",\"inputs\":[{\"name\":\"amountIn\",\"type\":\"uint256\"},{\"name\":\"path\",\"type\":\"address[]\"}],\"outputs\":[{\"name\":\"amounts\",\"type\":\"uint256[]\"}]}]";

    public ProfitabilitySimulator(
        Web3 web3,
        decimal flashloanPremiumPercent = 0.09m,
        decimal liquidationBonusPercent = 5.0m,
        decimal slippagePercent = 1.0m,
        decimal gasPriceGwei = 30,
        long estimatedGasUnits = 400_000
    )
    {
        _web3 = web3;
        _flashloanPremiumPercent = flashloanPremiumPercent;
        _liquidationBonusPercent = liquidationBonusPercent;
        _slippagePercent = slippagePercent;
        _gasPriceGwei = gasPriceGwei;
        _estimatedGasUnits = estimatedGasUnits;
    }

    public async Task<decimal> SimulateProfitAsync(
        decimal debtAmount, // in human units (e.g., 5000 USDC)
        int debtAssetDecimals,
        string collateralAsset,
        string debtAsset,
        string dexRouterAddress
    )
    {
        var contract = _web3.Eth.GetContract(_routerV2Abi, dexRouterAddress);
        var getAmountsOutFunc = contract.GetFunction("getAmountsOut");

        var debtAmountWei = UnitConversion.Convert.ToWei(debtAmount, debtAssetDecimals);

        var path = new[] { collateralAsset, debtAsset };

        // Estimate how much debtAsset we get when swapping collateral
        var expectedAmounts = await getAmountsOutFunc.CallAsync<List<BigInteger>>(debtAmountWei, path);

        if (expectedAmounts.Count != 2)
            throw new Exception("Invalid amountsOut length");

        var expectedSwapOutput = Web3.Convert.FromWei(expectedAmounts[1], debtAssetDecimals);

        // Apply liquidation bonus
        var collateralReceived = debtAmount * (1 + _liquidationBonusPercent / 100m);

        // Apply slippage
        var collateralAfterSlippage = collateralReceived * (1 - _slippagePercent / 100m);

        // Flashloan premium
        var flashloanCost = debtAmount * (_flashloanPremiumPercent / 100m);

        // Gas cost (in ETH)
        var gasPriceWei = UnitConversion.Convert.ToWei(_gasPriceGwei, UnitConversion.EthUnit.Gwei);
        var gasCostEth = (decimal)(_estimatedGasUnits * gasPriceWei) / (decimal)Math.Pow(10, 18);

        // Gross profit
        var grossProfit = collateralAfterSlippage - (debtAmount + flashloanCost);

        // Subtract gas cost in USD terms (approximate)
        // You could use ETH/USD price from Chainlink to be more accurate
        var gasCostUsd = gasCostEth * 3100; // assume ETH price is 3100 USD
        var netProfit = grossProfit - gasCostUsd;

        return netProfit;
    }

    public async Task<bool> IsProfitableAsync(
        decimal debtAmount,
        int debtAssetDecimals,
        string collateralAsset,
        string debtAsset,
        string dexRouterAddress
    )
    {
        var profit = await SimulateProfitAsync(debtAmount, debtAssetDecimals, collateralAsset, debtAsset, dexRouterAddress);
        return profit > 0;
    }
}