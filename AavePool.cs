using System.Numerics;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;

public class AavePool(Web3 web3, LiquidationExecutor liquidationExecutor, string poolAddress)
{
    private static readonly string LendingPoolAbi = @"[
    {
        'inputs': [{ 'internalType': 'address','name': 'user','type': 'address' }],
        'name': 'getUserAccountData',
        'outputs': [
            { 'internalType': 'uint256','name': 'totalCollateralBase','type': 'uint256' },
            { 'internalType': 'uint256','name': 'totalDebtBase','type': 'uint256' },
            { 'internalType': 'uint256','name': 'availableBorrowsBase','type': 'uint256' },
            { 'internalType': 'uint256','name': 'currentLiquidationThreshold','type': 'uint256' },
            { 'internalType': 'uint256','name': 'ltv','type': 'uint256' },
            { 'internalType': 'uint256','name': 'healthFactor','type': 'uint256' }
        ],
        'stateMutability': 'view',
        'type': 'function'
    }]";

    public async Task MonitorBorrowerAsync(string borrowerAddress)
    {
        var contract = web3.Eth.GetContract(LendingPoolAbi, poolAddress);
        var func = contract.GetFunction("getUserAccountData");

        var output = await func.CallDeserializingToObjectAsync<UserAccountDataDTO>(borrowerAddress);

        decimal healthFactor = Web3.Convert.FromWei(output.HealthFactor, 18);

        Console.WriteLine($"Borrower: {borrowerAddress} | Health Factor: {healthFactor:F4}");

        if (healthFactor < 1.0m)
        {
            Console.WriteLine($"Borrower is liquidatable!");
            await TriggerLiquidationAsync(borrowerAddress);
        }
    }

    public async Task MonitorBorrowersAsync(List<string> borrowers)
    {
        var tasks = new List<Task>();

        foreach (var borrower in borrowers)
        {
            tasks.Add(MonitorBorrowerAsync(borrower));
        }

        await Task.WhenAll(tasks);
    }

    private async Task TriggerLiquidationAsync(string borrowerAddress)
    {
        Console.WriteLine($"Triggering liquidation for {borrowerAddress}...");

        //await liquidationExecutor.TriggerLiquidationAsync(
        //    borrowerAddress,
        //    debtAssetAddress,
        //    collateralAssetAddress,
        //    debtToCover,
        //    debtAssetDecimals
        //);
    }
}

[FunctionOutput]
public class UserAccountDataDTO : IFunctionOutputDTO
{
    [Parameter("uint256", "totalCollateralBase")]
    public BigInteger TotalCollateralBase { get; set; }

    [Parameter("uint256", "totalDebtBase")]
    public BigInteger TotalDebtBase { get; set; }

    [Parameter("uint256", "availableBorrowsBase")]
    public BigInteger AvailableBorrowsBase { get; set; }

    [Parameter("uint256", "currentLiquidationThreshold")]
    public BigInteger CurrentLiquidationThreshold { get; set; }

    [Parameter("uint256", "ltv")]
    public BigInteger Ltv { get; set; }

    [Parameter("uint256", "healthFactor")]
    public BigInteger HealthFactor { get; set; }
}