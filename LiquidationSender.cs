using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;

public class LiquidationSender
{
    private static readonly string LiquidationAbi = @"[
      {
        'inputs': [
          { 'internalType': 'address','name': 'borrower','type': 'address' },
          { 'internalType': 'address','name': 'debtAsset','type': 'address' },
          { 'internalType': 'address','name': 'collateralAsset','type': 'address' },
          { 'internalType': 'uint256','name': 'debtToCover','type': 'uint256' }
        ],
        'name': 'executeLiquidation',
        'outputs': [],
        'stateMutability': 'nonpayable',
        'type': 'function'
      }
    ]";

    private readonly string _contractAddress;

    public Web3 Web3 { get; set; }
    public Account Account { get; set; }

    public LiquidationSender(string jsonRpc, string liquidationContract, string privateKey)
    {
        _contractAddress = liquidationContract;

        Account = new Account(privateKey);
        Web3 = new Web3(Account, jsonRpc);
    }

    public async Task TriggerLiquidationAsync(string borrower, string debtAsset, string collateralAsset, decimal debtToCoverHuman, int debtDecimals)
    {
        var contract = Web3.Eth.GetContract(LiquidationAbi, _contractAddress);
        var function = contract.GetFunction("executeLiquidation");

        var debtToCoverWei = Web3.Convert.ToWei(debtToCoverHuman, debtDecimals);

        var gasEstimate = await function.EstimateGasAsync(
            Account, null, null,
            borrower, debtAsset, collateralAsset, debtToCoverWei
        );

        var txHash = await function.SendTransactionAsync(
            Account.Address, 
            new HexBigInteger(gasEstimate),
            null, // auto gas price
            borrower, debtAsset, collateralAsset, debtToCoverWei
        );

        Console.WriteLine($"✅ Liquidation Transaction Sent: {txHash}");
    }
}