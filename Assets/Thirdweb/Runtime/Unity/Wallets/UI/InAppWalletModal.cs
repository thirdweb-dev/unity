using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thirdweb.Unity
{
    public class InAppWalletModal : MonoBehaviour
    {
        [field: SerializeField, Header("UI Settings")]
        private Canvas InAppWalletCanvas { get; set; }

        [field: SerializeField]
        private TMP_InputField OTPInputField { get; set; }

        [field: SerializeField]
        private Button SubmitButton { get; set; }

        public static Task<InAppWallet> LoginWithOtp(InAppWallet wallet)
        {
#if UNITY_6000_0_OR_NEWER
            var modal = FindAnyObjectByType<InAppWalletModal>();
#else
            var modal = FindObjectOfType<InAppWalletModal>();
#endif
            if (modal == null)
            {
                modal = new GameObject("InAppWalletModal").AddComponent<InAppWalletModal>();
            }

            modal.SubmitButton.onClick.RemoveAllListeners();
            modal.OTPInputField.text = string.Empty;
            modal.InAppWalletCanvas.gameObject.SetActive(true);

            modal.OTPInputField.interactable = true;
            modal.SubmitButton.interactable = true;

            var tcs = new TaskCompletionSource<InAppWallet>();

            modal.SubmitButton.onClick.AddListener(async () =>
            {
                try
                {
                    var otp = modal.OTPInputField.text;
                    if (string.IsNullOrEmpty(otp))
                    {
                        return;
                    }

                    modal.OTPInputField.interactable = false;
                    modal.SubmitButton.interactable = false;
                    var address = await wallet.LoginWithOtp(otp);
                    if (address != null)
                    {
                        modal.InAppWalletCanvas.gameObject.SetActive(false);
                        tcs.SetResult(wallet);
                    }
                    else
                    {
                        modal.OTPInputField.text = string.Empty;
                        modal.OTPInputField.interactable = true;
                        modal.SubmitButton.interactable = true;
                    }
                }
                catch (System.Exception e)
                {
                    modal.InAppWalletCanvas.gameObject.SetActive(false);
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }
    }
}
