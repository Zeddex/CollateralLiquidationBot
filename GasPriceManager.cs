using Nethereum.Util;
using Nethereum.Web3;

public class GasPriceManager(Web3 web3, decimal maxGasGwei)
{
    public async Task<bool> IsGasCheapEnoughAsync()
    {
        var gasPriceWei = await web3.Eth.GasPrice.SendRequestAsync();
        var gasPriceGwei = Web3.Convert.FromWei(gasPriceWei, UnitConversion.EthUnit.Gwei);

        Console.WriteLine($"Current Gas Price: {gasPriceGwei:F2} gwei");

        return gasPriceGwei <= maxGasGwei;
    }
}