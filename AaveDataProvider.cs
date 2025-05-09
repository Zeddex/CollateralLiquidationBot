using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Web3;

public class AaveDataProvider(Web3 web3, string dataProviderAddress)
{
    private const string Abi = @"[
    {
        'inputs':[{'internalType':'address','name':'asset','type':'address'}],
        'name':'getReserveConfigurationData',
        'outputs':[
            {'internalType':'uint256','name':'decimals','type':'uint256'},
            {'internalType':'uint256','name':'ltv','type':'uint256'},
            {'internalType':'uint256','name':'liquidationThreshold','type':'uint256'},
            {'internalType':'uint256','name':'liquidationBonus','type':'uint256'},
            {'internalType':'uint256','name':'reserveFactor','type':'uint256'},
            {'internalType':'bool','name':'usageAsCollateralEnabled','type':'bool'},
            {'internalType':'bool','name':'borrowingEnabled','type':'bool'},
            {'internalType':'bool','name':'stableBorrowRateEnabled','type':'bool'},
            {'internalType':'bool','name':'isActive','type':'bool'},
            {'internalType':'bool','name':'isFrozen','type':'bool'}
        ],
        'stateMutability':'view',
        'type':'function'
    }]";
    public async Task<ReserveConfigurationDataDTO> GetReserveConfigDataAsync(string tokenAddress)
    {
        var contract = web3.Eth.GetContract(Abi, dataProviderAddress);
        var func = contract.GetFunction("getReserveConfigurationData");
        var data = await func.CallDeserializingToObjectAsync<ReserveConfigurationDataDTO>(tokenAddress);

        return data;
    }
}

[FunctionOutput]
public class ReserveConfigurationDataDTO : IFunctionOutputDTO
{
    [Parameter("uint256", "decimals")]
    public BigInteger Decimals { get; set; }

    [Parameter("uint256", "ltv")]
    public BigInteger Ltv { get; set; }

    [Parameter("uint256", "liquidationThreshold")]
    public BigInteger LiquidationThreshold { get; set; }

    [Parameter("uint256", "liquidationBonus")]
    public BigInteger LiquidationBonus { get; set; }

    [Parameter("uint256", "reserveFactor")]
    public BigInteger ReserveFactor { get; set; }

    [Parameter("bool", "usageAsCollateralEnabled")]
    public bool UsageAsCollateralEnabled { get; set; }

    [Parameter("bool", "borrowingEnabled")]
    public bool BorrowingEnabled { get; set; }

    [Parameter("bool", "stableBorrowRateEnabled")]
    public bool StableBorrowRateEnabled { get; set; }

    [Parameter("bool", "isActive")]
    public bool IsActive { get; set; }

    [Parameter("bool", "isFrozen")]
    public bool IsFrozen { get; set; }
}
