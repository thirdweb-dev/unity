using UnityEngine;
using System.Linq;
using System.Numerics;

namespace Thirdweb.Unity
{
    public class ThirdwebManager : ThirdwebManagerBase
    {
        [field: SerializeField]
        private string ClientId { get; set; }

        [field: SerializeField]
        private string BundleId { get; set; }

        public new static ThirdwebManager Instance
        {
            get => ThirdwebManagerBase.Instance as ThirdwebManager;
        }

        protected override ThirdwebClient CreateClient()
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                ThirdwebDebug.LogError("ClientId must be set in order to initialize ThirdwebManager. " + "Get your API key from https://thirdweb.com/create-api-key");
                return null;
            }

            if (string.IsNullOrWhiteSpace(BundleId))
            {
                BundleId = null;
            }

            BundleId ??= string.IsNullOrWhiteSpace(Application.identifier) ? $"com.{Application.companyName}.{Application.productName}" : Application.identifier;

            return ThirdwebClient.Create(
                clientId: ClientId,
                bundleId: BundleId,
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

        protected override string MobileRedirectScheme => BundleId + "://";
    }
}
