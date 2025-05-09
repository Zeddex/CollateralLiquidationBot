using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;

public class LiquidationExecutor(Web3 web3, string privateKey, string liquidationContract)
{
    private static readonly string Abi = @"[
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

    public Account Account { get; set; } = new(privateKey);

    public async Task TriggerLiquidationAsync(string borrower, string debtAsset, string collateralAsset, decimal debtToCoverHuman, int debtDecimals)
    {
        var contract = web3.Eth.GetContract(Abi, liquidationContract);
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

        Console.WriteLine($"Liquidation Transaction Sent: {txHash}");
    }
}