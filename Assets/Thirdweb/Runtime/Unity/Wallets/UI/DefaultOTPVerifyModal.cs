using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thirdweb.Unity
{
    public class DefaultOTPVerifyModal : AbstractOTPVerifyModal
    {
        [field: SerializeField, Header("UI Settings")]
        private Canvas OTPCanvas { get; set; }

        [field: SerializeField]
        private TMP_InputField OTPInputField { get; set; }

        [field: SerializeField]
        private Button OTPSubmitButton { get; set; }

        public override Task<IThirdwebWallet> LoginWithOtp(IThirdwebWallet wallet)
        {
            OTPSubmitButton.onClick.RemoveAllListeners();
            OTPInputField.text = string.Empty;
            OTPCanvas.gameObject.SetActive(true);

            OTPInputField.interactable = true;
            OTPSubmitButton.interactable = true;

            var tcs = new TaskCompletionSource<IThirdwebWallet>();

            OTPSubmitButton.onClick.AddListener(async () =>
            {
                try
                {
                    var otp = OTPInputField.text;
                    if (string.IsNullOrEmpty(otp))
                    {
                        return;
                    }

                    OTPInputField.interactable = false;
                    OTPSubmitButton.interactable = false;
                    var address = await (wallet as EcosystemWallet).LoginWithOtp(otp);
                    if (address != null)
                    {
                        OTPCanvas.gameObject.SetActive(false);
                        tcs.SetResult(wallet);
                    }
                    else
                    {
                        OTPInputField.text = string.Empty;
                        OTPInputField.interactable = true;
                        OTPSubmitButton.interactable = true;
                    }
                }
                catch (System.Exception e)
                {
                    OTPCanvas.gameObject.SetActive(false);
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }
    }
}
