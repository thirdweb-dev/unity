using UnityEngine;
using System.Linq;
using System.Numerics;

namespace Thirdweb.Unity
{
    public class ThirdwebManagerServer : ThirdwebManagerBase
    {
        [field: SerializeField]
        private string SecretKey { get; set; }

        public new static ThirdwebManagerServer Instance
        {
            get => ThirdwebManagerBase.Instance as ThirdwebManagerServer;
        }

        protected override ThirdwebClient CreateClient()
        {
            if (string.IsNullOrEmpty(SecretKey))
            {
                ThirdwebDebug.LogError("SecretKey must be set in order to initialize ThirdwebManagerServer.");
                return null;
            }

            return ThirdwebClient.Create(
                secretKey: SecretKey,
                httpClient: new CrossPlatformUnityHttpClient(),
                sdkName: Application.platform == RuntimePlatform.WebGLPlayer ? "UnitySDK_WebGL" : "UnitySDK",
                sdkOs: Application.platform.ToString(),
                sdkPlatform: "unity",
                sdkVersion: THIRDWEB_UNITY_SDK_VERSION,
                rpcOverrides: (RpcOverrides == null || RpcOverrides.Count == 0)
                    ? null
                    : RpcOverrides.ToDictionary(rpcOverride => new BigInteger(rpcOverride.ChainId), rpcOverride => rpcOverride.RpcUrl)
            );
        }

        protected override string MobileRedirectScheme => "tw-server://";
    }
}
