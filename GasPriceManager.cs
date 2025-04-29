using Nethereum.Util;
using Nethereum.Web3;

public class GasPriceManager
{
    private readonly Web3 _web3;
    private readonly decimal _maxGasGwei;

    public GasPriceManager(Web3 web3, decimal maxGasGwei)
    {
        _web3 = web3;
        _maxGasGwei = maxGasGwei;
    }

    public async Task<bool> IsGasCheapEnoughAsync()
    {
        var gasPriceWei = await _web3.Eth.GasPrice.SendRequestAsync();
        var gasPriceGwei = Web3.Convert.FromWei(gasPriceWei, UnitConversion.EthUnit.Gwei);

        Console.WriteLine($"⛽ Current Gas Price: {gasPriceGwei:F2} gwei");

        return gasPriceGwei <= _maxGasGwei;
    }
}