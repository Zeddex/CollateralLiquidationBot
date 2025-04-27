using System.Numerics;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;

public class AaveMonitor
{
    private readonly Web3 _web3;
    private readonly string _lendingPoolAddress;
    private readonly string _liquidationContractAddress;

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

    public AaveMonitor(string rpcUrl, string lendingPoolAddress, string liquidationContractAddress)
    {
        _web3 = new Web3(rpcUrl);
        _lendingPoolAddress = lendingPoolAddress;
        _liquidationContractAddress = liquidationContractAddress;
    }

    public async Task MonitorBorrowerAsync(string borrowerAddress)
    {
        var contract = _web3.Eth.GetContract(LendingPoolAbi, _lendingPoolAddress);
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

    private async Task TriggerLiquidationAsync(string borrower)
    {
        Console.WriteLine($"Triggering liquidation for {borrower}...");

        // Here you would call your smart contract
        // E.g., send transaction to liquidation contract with borrower address

        // Example only: real implementation needs ABI + Flashloan start params
        // await liquidationContract.StartLiquidation(borrower);
    }
}

public class UserAccountDataDTO
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