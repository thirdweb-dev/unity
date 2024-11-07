using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Thirdweb.Unity
{
    public static class InAppWalletModal
    {
        public static async Task<InAppWallet> LoginWithOtp(InAppWallet wallet)
        {
#if UNITY_6000_0_OR_NEWER
            var modal = Object.FindAnyObjectByType<AbstractOTPVerifyModal>();
#else
            var modal = Object.FindObjectOfType<AbstractOTPVerifyModal>();
#endif
            if (modal == null)
            {
                throw new Exception("No OTPVerifyModal found in the scene.");
            }
            return await modal.LoginWithOtp(wallet) as InAppWallet;
        }
    }
}
