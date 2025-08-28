using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;
using UnityEngine;

namespace Thirdweb.Unity
{
    public class WebGLMetaMask : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void EnableEthereum(string gameObjectName, string callback, string fallback);

        [DllImport("__Internal")]
        private static extern void EthereumInit(string gameObjectName, string callBackAccountChange, string callBackChainChange);

        [DllImport("__Internal")]
        private static extern void GetChainId(string gameObjectName, string callback, string fallback);

        [DllImport("__Internal")]
        private static extern bool IsMetamaskAvailable();

        [DllImport("__Internal")]
        private static extern string GetSelectedAddress();

        [DllImport("__Internal")]
        private static extern void Request(string rpcRequestMessage, string gameObjectName, string callback, string fallback);
#endif

        private static WebGLMetaMask _instance;
        private BigInteger _activeChainId;
        private string _selectedAddress;
        private bool _isConnected;
        private TaskCompletionSource<bool> _enableEthereumTaskCompletionSource;
        private TaskCompletionSource<string> _rpcResponseCompletionSource;

        public static ThirdwebAccountType AccountType => ThirdwebAccountType.ExternalAccount;

        public static WebGLMetaMask Instance
        {
            get
            {
                if (_instance == null)
                {
                    var gameObject = new GameObject("WebGLMetaMask");
                    _instance = gameObject.AddComponent<WebGLMetaMask>();
                    DontDestroyOnLoad(gameObject);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public async Task<bool> EnableEthereumAsync(BigInteger chainId)
        {
            _activeChainId = chainId;
            _enableEthereumTaskCompletionSource = new TaskCompletionSource<bool>();

#if UNITY_WEBGL && !UNITY_EDITOR
            EnableEthereum(gameObject.name, nameof(OnEnableEthereum), nameof(OnEnableEthereumFallback));
#endif

            return await _enableEthereumTaskCompletionSource.Task;
        }

        public void OnEnableEthereum(string result)
        {
            ThirdwebDebug.Log($"OnEnableEthereum: {result}");
            _selectedAddress = result;
            _isConnected = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            EthereumInit(gameObject.name, nameof(OnAccountChange), nameof(OnChainChange));
#endif
            _enableEthereumTaskCompletionSource.SetResult(true);
        }

        public void OnEnableEthereumFallback(string result)
        {
            _isConnected = false;
            ThirdwebDebug.LogError($"OnEnableEthereumFallback: {result}");
            _enableEthereumTaskCompletionSource.SetResult(false);
        }

        public void OnAccountChange(string result)
        {
            ThirdwebDebug.Log($"OnAccountChange: {result}");
#if UNITY_WEBGL && !UNITY_EDITOR
            _selectedAddress = GetSelectedAddress();
#endif
        }

        public void OnChainChange(string result)
        {
            ThirdwebDebug.Log($"OnChainChange: {result}");
#if UNITY_WEBGL && !UNITY_EDITOR
            GetChainId(gameObject.name, nameof(OnChainId), nameof(OnChainIdFallback));
#endif
        }

        public void OnChainId(string result)
        {
            ThirdwebDebug.Log($"OnChainId: {result}");
            _activeChainId = new HexBigInteger(result).Value;
        }

        public void OnChainIdFallback(string result)
        {
            ThirdwebDebug.LogError($"OnChainIdFallback: {result}");
        }

        public async Task<T> RequestAsync<T>(RpcRequest rpcRequest)
        {
            var rpcRequestMessage = JsonConvert.SerializeObject(rpcRequest);
            _rpcResponseCompletionSource = new TaskCompletionSource<string>();

#if UNITY_WEBGL && !UNITY_EDITOR
            Request(rpcRequestMessage, gameObject.name, nameof(OnRequestCallback), nameof(OnRequestFallback));
#endif

            var response = await _rpcResponseCompletionSource.Task;
            var rpcResponseRaw = JsonConvert.DeserializeObject<RpcResponse<T>>(response);
            if (rpcResponseRaw.Error != null)
            {
                throw new Exception(rpcResponseRaw.Error.Message);
            }
            return rpcResponseRaw.Result;
        }

        public void OnRequestCallback(string result)
        {
            _rpcResponseCompletionSource.SetResult(result);
        }

        public void OnRequestFallback(string result)
        {
            _rpcResponseCompletionSource.SetResult(result);
        }

        public bool IsMetaMaskAvailable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsMetamaskAvailable();
#else
            return false;
#endif
        }

        public string GetAddress()
        {
            return _selectedAddress;
        }

        public BigInteger GetActiveChainId()
        {
            return _activeChainId;
        }

        public bool IsConnected()
        {
            return _isConnected;
        }
    }
}
