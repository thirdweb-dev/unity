using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thirdweb.Unity 
{
    public class DefaultOTPVerifyModal : AbstractOTPVerifyModal 
    {
        [field: SerializeField, Header("UI Settings")]
        private Canvas InAppWalletCanvas { get; set; }

        [field: SerializeField]
        private TMP_InputField OTPInputField { get; set; }

        [field: SerializeField]
        private Button SubmitButton { get; set; }

        public override Task<InAppWallet> LoginWithOtp(InAppWallet wallet)
        {
            SubmitButton.onClick.RemoveAllListeners();
            OTPInputField.text = string.Empty;
            InAppWalletCanvas.gameObject.SetActive(true);

            OTPInputField.interactable = true;
            SubmitButton.interactable = true;

            var tcs = new TaskCompletionSource<InAppWallet>();

            SubmitButton.onClick.AddListener(async () =>
            {
                try
                {
                    var otp = OTPInputField.text;
                    if (string.IsNullOrEmpty(otp))
                    {
                        return;
                    }

                    OTPInputField.interactable = false;
                    SubmitButton.interactable = false;
                    (var address, var canRetry) = await wallet.LoginWithOtp(otp);
                    if (address != null)
                    {
                        InAppWalletCanvas.gameObject.SetActive(false);
                        tcs.SetResult(wallet);
                    }
                    else if (!canRetry)
                    {
                        InAppWalletCanvas.gameObject.SetActive(false);
                        tcs.SetException(new UnityException("Failed to verify OTP."));
                    }
                    else
                    {
                        OTPInputField.text = string.Empty;
                        OTPInputField.interactable = true;
                        SubmitButton.interactable = true;
                    }
                }
                catch (System.Exception e)
                {
                    InAppWalletCanvas.gameObject.SetActive(false);
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }
    }
}