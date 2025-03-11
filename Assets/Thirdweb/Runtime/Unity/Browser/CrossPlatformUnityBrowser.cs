using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Thirdweb.Unity
{
    public class CrossPlatformUnityBrowser : IThirdwebBrowser
    {
        IThirdwebBrowser _unityBrowser;

        public CrossPlatformUnityBrowser(string htmlOverride = null)
        {
            if (string.IsNullOrEmpty(htmlOverride) || string.IsNullOrWhiteSpace(htmlOverride))
            {
                htmlOverride = null;
            }

#if UNITY_EDITOR
            _unityBrowser = new InAppWalletBrowser(htmlOverride);
#elif UNITY_WEBGL
#if UNITY_6000_0_OR_NEWER
            var existingBrowser = UnityEngine.Object.FindAnyObjectByType<WebGLInAppWalletBrowser>();
#else
            var existingBrowser = GameObject.FindObjectOfType<WebGLInAppWalletBrowser>();
#endif
            if (existingBrowser != null)
            {
                _unityBrowser = existingBrowser;
            }
            else
            {
                var go = new GameObject("WebGLInAppWalletBrowser");
                _unityBrowser = go.AddComponent<WebGLInAppWalletBrowser>();
            }
#elif UNITY_ANDROID
            _unityBrowser = new AndroidBrowser();
#elif UNITY_IOS
            _unityBrowser = new IOSBrowser();
#else
            _unityBrowser = new InAppWalletBrowser(htmlOverride);
#endif
        }

        public async Task<BrowserResult> Login(ThirdwebClient client, string loginUrl, string customScheme, Action<string> browserOpenAction, CancellationToken cancellationToken = default)
        {
            return await _unityBrowser.Login(client, loginUrl, customScheme, browserOpenAction, cancellationToken);
        }
    }
}
