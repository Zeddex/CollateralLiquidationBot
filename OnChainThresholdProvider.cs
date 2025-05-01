using System.Numerics;
using Nethereum.Web3;

public class OnChainThresholdProvider : ILiquidationThresholdProvider
{
    private readonly Web3 _web3;
    private readonly string _dataProviderAddress;
    private const string Abi = @"[{""inputs"":[{""internalType"":""address"",""name"":""asset"",""type"":""address""}],""name"":""getReserveConfigurationData"",""outputs"":[{""internalType"":""uint256"",""name"":""decimals"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""ltv"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""liquidationThreshold"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""liquidationBonus"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""reserveFactor"",""type"":""uint256""},{""internalType"":""bool"",""name"":""usageAsCollateralEnabled"",""type"":""bool""},{""internalType"":""bool"",""name"":""borrowingEnabled"",""type"":""bool""},{""internalType"":""bool"",""name"":""stableBorrowRateEnabled"",""type"":""bool""},{""internalType"":""bool"",""name"":""isActive"",""type"":""bool""},{""internalType"":""bool"",""name"":""isFrozen"",""type"":""bool""}],""stateMutability"":""view"",""type"":""function""}]";

    public OnChainThresholdProvider(Web3 web3, string dataProviderAddress)
    {
        _web3 = web3;
        _dataProviderAddress = dataProviderAddress;
    }

    public async Task<decimal> GetLiquidationThresholdAsync(string tokenAddress)
    {
        var contract = _web3.Eth.GetContract(Abi, _dataProviderAddress);
        var func = contract.GetFunction("getReserveConfigurationData");
        var output = await func.CallDecodingToDefaultAsync(tokenAddress);

        var liquidationThreshold = output[1].Result; // index 1 = liquidationThreshold
        return (decimal)liquidationThreshold / 10000m; // e.g., 8250 → 0.825
    }
}