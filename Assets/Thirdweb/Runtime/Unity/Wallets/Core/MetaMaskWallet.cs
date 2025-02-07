using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.HostWallet;
using Newtonsoft.Json;
using UnityEngine;

namespace Thirdweb.Unity
{
    public class MetaMaskWallet : IThirdwebWallet
    {
        public ThirdwebClient Client => _client;
        public string WalletId => "metamask";
        public ThirdwebAccountType AccountType => ThirdwebAccountType.ExternalAccount;

        private static ThirdwebClient _client;

        protected MetaMaskWallet() { }

        public static async Task<MetaMaskWallet> Create(ThirdwebClient client, BigInteger activeChainId)
        {
            _client = client;

            if (Application.platform != RuntimePlatform.WebGLPlayer || Application.isEditor)
            {
                throw new Exception("MetaMaskWallet is only available in WebGL Builds. Please use a different wallet provider on native platforms.");
            }

            var metaMaskInstance = WebGLMetaMask.Instance;
            var mmWallet = new MetaMaskWallet();

            if (metaMaskInstance.IsConnected() && !string.IsNullOrEmpty(metaMaskInstance.GetAddress()))
            {
                ThirdwebDebug.Log("MetaMask already initialized.");
                await mmWallet.SwitchNetwork(activeChainId);
            }
            else
            {
                if (metaMaskInstance.IsMetaMaskAvailable())
                {
                    ThirdwebDebug.Log("MetaMask is available. Enabling Ethereum...");
                    var isEnabled = await metaMaskInstance.EnableEthereumAsync();
                    ThirdwebDebug.Log($"Ethereum enabled: {isEnabled}");
                    if (isEnabled && !string.IsNullOrEmpty(metaMaskInstance.GetAddress()))
                    {
                        ThirdwebDebug.Log("MetaMask initialized successfully.");
                        await mmWallet.SwitchNetwork(activeChainId);
                    }
                    else
                    {
                        throw new Exception("MetaMask initialization failed or address is empty.");
                    }
                }
                else
                {
                    throw new Exception("MetaMask is not available.");
                }
            }
            Utils.TrackConnection(mmWallet);
            return mmWallet;
        }

        #region IThirdwebWallet

        public Task<string> EthSign(byte[] rawMessage)
        {
            throw new NotImplementedException("MetaMask does not support signing raw messages.");
        }

        public Task<string> EthSign(string message)
        {
            throw new NotImplementedException("MetaMask does not support signing messages.");
        }

        public Task<string> GetAddress()
        {
            return Task.FromResult(WebGLMetaMask.Instance.GetAddress().ToChecksumAddress());
        }

        public Task<bool> IsConnected()
        {
            return Task.FromResult(WebGLMetaMask.Instance.IsConnected());
        }

        public Task<string> PersonalSign(byte[] rawMessage)
        {
            if (rawMessage == null)
            {
                throw new ArgumentNullException(nameof(rawMessage), "Message to sign cannot be null.");
            }

            var hex = Utils.BytesToHex(rawMessage);
            return PersonalSign(hex);
        }

        public async Task<string> PersonalSign(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message), "Message to sign cannot be null or empty.");
            }

            var rpcRequest = new RpcRequest { Method = "personal_sign", Params = new object[] { message.StartsWith("0x") ? message : message.StringToHex(), await GetAddress() } };
            return await WebGLMetaMask.Instance.RequestAsync<string>(rpcRequest);
        }

        public async Task<string> SendTransaction(ThirdwebTransactionInput transaction)
        {
            var rpcRequest = new RpcRequest
            {
                Method = "eth_sendTransaction",
                Params = new object[]
                {
                    new TransactionInput()
                    {
                        Nonce = transaction.Nonce,
                        From = await GetAddress(),
                        To = transaction.To,
                        Gas = transaction.Gas,
                        GasPrice = transaction.GasPrice,
                        Value = transaction.Value,
                        Data = transaction.Data,
                        MaxFeePerGas = transaction.MaxFeePerGas,
                        MaxPriorityFeePerGas = transaction.MaxPriorityFeePerGas,
                        ChainId = new HexBigInteger(WebGLMetaMask.Instance.GetActiveChainId()),
                    }
                }
            };
            return await WebGLMetaMask.Instance.RequestAsync<string>(rpcRequest);
        }

        public async Task<ThirdwebTransactionReceipt> ExecuteTransaction(ThirdwebTransactionInput transaction)
        {
            var hash = await SendTransaction(transaction);
            return await ThirdwebTransaction.WaitForTransactionReceipt(client: _client, chainId: WebGLMetaMask.Instance.GetActiveChainId(), txHash: hash);
        }

        public Task<string> SignTransaction(ThirdwebTransactionInput transaction)
        {
            throw new NotImplementedException("Raw transaction signing is not supported.");
        }

        public async Task<string> SignTypedDataV4(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json), "Json to sign cannot be null.");
            }

            var rpcRequest = new RpcRequest { Method = "eth_signTypedData_v4", Params = new object[] { await GetAddress(), json } };
            return await WebGLMetaMask.Instance.RequestAsync<string>(rpcRequest);
        }

        public Task<string> SignTypedDataV4<T, TDomain>(T data, TypedData<TDomain> typedData)
            where TDomain : IDomain
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Data to sign cannot be null.");
            }

            if (typedData == null)
            {
                throw new ArgumentNullException(nameof(typedData), "Typed data to sign cannot be null.");
            }

            var safeJson = Utils.ToJsonExternalWalletFriendly(typedData, data);
            return SignTypedDataV4(safeJson);
        }

        public Task<string> RecoverAddressFromEthSign(string message, string signature)
        {
            throw new NotImplementedException();
        }

        public Task<string> RecoverAddressFromPersonalSign(string message, string signature)
        {
            var signer = new Nethereum.Signer.EthereumMessageSigner();
            var addressRecovered = signer.EncodeUTF8AndEcRecover(message, signature);
            return Task.FromResult(addressRecovered);
        }

        public Task<string> RecoverAddressFromTypedDataV4<T, TDomain>(T data, TypedData<TDomain> typedData, string signature)
            where TDomain : IDomain
        {
            throw new NotImplementedException();
        }

        public async Task Disconnect()
        {
            try
            {
                _ = await WebGLMetaMask.Instance.RequestAsync<string>(
                    new RpcRequest { Method = "wallet_revokePermissions", Params = new object[] { new Dictionary<string, object> { { "eth_accounts", new object() } } } }
                );
            }
            catch
            {
                // no-op
            }
        }

        public Task<List<LinkedAccount>> LinkAccount(
            IThirdwebWallet walletToLink,
            string otp = null,
            bool? isMobile = null,
            Action<string> browserOpenAction = null,
            string mobileRedirectScheme = "thirdweb://",
            IThirdwebBrowser browser = null,
            BigInteger? chainId = null,
            string jwt = null,
            string payload = null,
            string defaultSessionIdOverride = null,
            List<string> forceWalletIds = null
        )
        {
            throw new InvalidOperationException("LinkAccount is not supported by external wallets.");
        }

        public Task<List<LinkedAccount>> GetLinkedAccounts()
        {
            throw new InvalidOperationException("GetLinkedAccounts is not supported by external wallets.");
        }

        public Task<List<LinkedAccount>> UnlinkAccount(LinkedAccount accountToUnlink)
        {
            throw new InvalidOperationException("UnlinkAccount is not supported by external wallets.");
        }

        public Task<EIP7702Authorization> SignAuthorization(BigInteger chainId, string contractAddress, bool willSelfExecute)
        {
            throw new InvalidOperationException("SignAuthorization is not supported by external wallets.");
        }

        public async Task SwitchNetwork(BigInteger chainId)
        {
            if (WebGLMetaMask.Instance.GetActiveChainId() != chainId)
            {
                try
                {
                    await SwitchNetworkInternal(chainId);
                }
                catch
                {
                    await AddNetworkInternal(chainId);
                    if (WebGLMetaMask.Instance.GetActiveChainId() == chainId)
                    {
                        return;
                    }
                    try
                    {
                        await SwitchNetworkInternal(chainId);
                    }
                    catch
                    {
                        // no-op, later metamask extension versions do not necessarily require switching post adding
                    }
                }
            }
        }

        #endregion

        #region Network Switching

        [Obsolete("Use IThirdwebWallet.SwitchNetwork instead.")]
        public static async Task EnsureCorrectNetwork(BigInteger chainId)
        {
            if (WebGLMetaMask.Instance.GetActiveChainId() != chainId)
            {
                try
                {
                    await SwitchNetworkInternal(chainId);
                }
                catch
                {
                    await AddNetworkInternal(chainId);
                    if (WebGLMetaMask.Instance.GetActiveChainId() == chainId)
                    {
                        return;
                    }
                    await SwitchNetworkInternal(chainId);
                }
            }
        }

        private static async Task SwitchNetworkInternal(BigInteger chainId)
        {
            var switchEthereumChainParameter = new SwitchEthereumChainParameter { ChainId = new HexBigInteger(chainId) };
            var rpcRequest = new RpcRequest { Method = "wallet_switchEthereumChain", Params = new object[] { switchEthereumChainParameter } };
            _ = await WebGLMetaMask.Instance.RequestAsync<string>(rpcRequest);
        }

        private static async Task AddNetworkInternal(BigInteger chainId)
        {
            ThirdwebDebug.Log($"Fetching chain data for chainId {chainId}...");
            var twChainData = await Utils.GetChainMetadata(_client, chainId) ?? throw new Exception($"Chain data for chainId {chainId} could not be fetched.");
            ThirdwebDebug.Log($"Chain data fetched: {JsonConvert.SerializeObject(twChainData)}");
            var explorers = twChainData.Explorers?.Select(e => e?.Url).Where(url => url != null).ToList() ?? new List<string>();
            var iconUrl = twChainData.Icon?.Url ?? "ipfs://QmdwQDr6vmBtXmK2TmknkEuZNoaDqTasFdZdu3DRw8b2wt";

            var nativeCurrency =
                twChainData.NativeCurrency
                ?? new ThirdwebChainNativeCurrency
                {
                    Name = "Ether",
                    Symbol = "ETH",
                    Decimals = 18
                };

            var addEthereumChainParameter = new AddEthereumChainParameter
            {
                ChainId = new HexBigInteger(chainId),
                BlockExplorerUrls = explorers,
                ChainName = twChainData.Name,
                IconUrls = new List<string> { iconUrl },
                NativeCurrency = new NativeCurrency
                {
                    Name = nativeCurrency.Name,
                    Symbol = nativeCurrency.Symbol,
                    Decimals = (uint)nativeCurrency.Decimals,
                },
                RpcUrls = new List<string> { $"https://{chainId}.rpc.thirdweb.com/" },
            };

            var rpcRequest = new RpcRequest { Method = "wallet_addEthereumChain", Params = new object[] { addEthereumChainParameter } };
            _ = await WebGLMetaMask.Instance.RequestAsync<string>(rpcRequest);

            ThirdwebDebug.Log($"Chain {chainId} added.");
        }

        #endregion
    }
}
