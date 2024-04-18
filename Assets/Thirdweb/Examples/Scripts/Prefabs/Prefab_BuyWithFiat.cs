using UnityEngine;
using Thirdweb;
using Thirdweb.Pay;
using Newtonsoft.Json;

public class Prefab_BuyWithFiat : MonoBehaviour
{
    private BuyWithFiatQuoteResult _quote;
    private string _quoteId;

    public async void GetQuote()
    {
        string connectedAddress = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();

        _quote = null;

        var fiatQuoteParams = new BuyWithFiatQuoteParams(fromCurrencySymbol: "USD", toAddress: connectedAddress, toChainId: "1", toTokenAddress: Utils.NativeTokenAddress, toAmount: "3");

        _quote = await ThirdwebPay.GetBuyWithFiatQuote(fiatQuoteParams);
        ThirdwebDebug.Log($"Fiat Quote: {JsonConvert.SerializeObject(_quote, Formatting.Indented)}");
    }

    public void Buy()
    {
        if (_quote == null)
        {
            ThirdwebDebug.Log("Get a quote first.");
            return;
        }

        try
        {
            _quoteId = ThirdwebPay.BuyWithFiat(_quote);
            ThirdwebDebug.Log($"Quote ID: {_quoteId}");
        }
        catch (System.Exception e)
        {
            ThirdwebDebug.Log($"Error: {e.Message}");
        }
    }

    public async void GetStatus()
    {
        if (string.IsNullOrEmpty(_quoteId))
        {
            ThirdwebDebug.Log("Quote ID is empty. Please buy first.");
            return;
        }

        var status = await ThirdwebPay.GetBuyWithFiatStatus(_quoteId);
        if (
            status.Status == OnRampStatus.PAYMENT_FAILED.ToString()
            || status.Status == OnRampStatus.ON_RAMP_TRANSFER_FAILED.ToString()
            || status.Status == OnRampStatus.ON_RAMP_TRANSFER_FAILED.ToString()
        )
            ThirdwebDebug.LogWarning($"Failed! Reason: {status.FailureMessage}");

        ThirdwebDebug.Log($"Status: {JsonConvert.SerializeObject(status, Formatting.Indented)}");
    }

    [ContextMenu("Get Supported Currencies")]
    public async void GetSupportedCurrencies()
    {
        var currencies = await ThirdwebPay.GetBuyWithFiatCurrencies();
        ThirdwebDebug.Log($"Supported Currencies: {JsonConvert.SerializeObject(currencies, Formatting.Indented)}");
    }
}
