using System.Threading.Tasks;
using UnityEngine;

namespace Thirdweb.Unity 
{
    public abstract class AbstractOTPVerifyModal : MonoBehaviour 
    {
        public abstract Task<InAppWallet> LoginWithOtp(InAppWallet wallet);
    }
}