using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.EIP712;
using Reown.AppKit.Unity;

namespace Thirdweb.Unity
{
    public class WalletConnectWallet : IThirdwebWallet
    {
        public ThirdwebClient Client => _client;

        public ThirdwebAccountType AccountType => ThirdwebAccountType.ExternalAccount;

        protected ThirdwebClient _client;

        protected static Exception _exception;
        protected static bool _isConnected;
        protected static string[] _supportedChains;
        protected static string[] _includedWalletIds;
        protected static string[] _excludedWalletIds;
        protected static Account ActiveAccount;

        protected WalletConnectWallet(ThirdwebClient client)
        {
            _client = client;
        }

        public async static Task<WalletConnectWallet> Create(ThirdwebClient client, BigInteger initialChainId, AppKitConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "AppKitConfig cannot be null, set in ThirdwebManager.");
            }

            if (_isConnected)
            {
                _isConnected = false;
                await AppKit.DisconnectAsync();
            }

            if (!AppKit.IsInitialized)
            {
                await AppKit.InitializeAsync(config);
            }

            AppKit.OpenModal(ViewType.Connect);

            AppKit.AccountConnected += OnConnected;

            while (!_isConnected && _exception == null)
            {
                await Task.Delay(100);
            }

            if (_exception != null)
            {
                throw _exception;
            }

            var wcw = new WalletConnectWallet(client);

            try
            {
                await wcw.SwitchNetwork(initialChainId);
            }
            catch (Exception ex)
            {
                ThirdwebDebug.LogWarning($"Failed to switch network: {ex}");
            }

            return wcw;
        }

        private static async void OnConnected(object sender, Connector.AccountConnectedEventArgs e)
        {
            try
            {
                _isConnected = true;
                AppKit.AccountConnected -= OnConnected;
                ActiveAccount = await e.GetAccount();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        public async Task SwitchNetwork(BigInteger chainId)
        {
            var wcChain =
                AppKit.Config.supportedChains.ToList().Find(c => c.ChainId == $"eip155:{chainId}")
                ?? throw new InvalidOperationException($"Chain ID {chainId} is not supported by the current AppKit configuration in your ThirdwebManager.");
            await AppKit.NetworkController.ChangeActiveChainAsync(wcChain);
        }

        #region IThirdwebWallet

        public Task<string> GetAddress()
        {
            return Task.FromResult(ActiveAccount.Address?.ToChecksumAddress());
        }

        public Task<string> EthSign(byte[] rawMessage)
        {
            throw new InvalidOperationException("EthSign is not supported by external wallets.");
        }

        public Task<string> EthSign(string message)
        {
            throw new InvalidOperationException("EthSign is not supported by external wallets.");
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
                throw new ArgumentNullException(nameof(message), "Message to sign cannot be null.");
            }

            return await AppKit.Evm.SignMessageAsync(message.StartsWith("0x") ? message : message.StringToHex());
        }

        public async Task<string> SignTypedDataV4(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json), "Json to sign cannot be null.");
            }

            return await AppKit.Evm.SignTypedDataAsync(json);
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

        public Task<string> SignTransaction(ThirdwebTransactionInput transaction)
        {
            throw new NotImplementedException("Raw transaction signing is not supported.");
        }

        public async Task<string> SendTransaction(ThirdwebTransactionInput transaction)
        {
            return await AppKit.Evm.SendTransactionAsync(transaction.To, transaction.Value?.Value ?? 0, transaction.Data);
        }

        public async Task<ThirdwebTransactionReceipt> ExecuteTransaction(ThirdwebTransactionInput transaction)
        {
            var hash = await SendTransaction(transaction);
            return await ThirdwebTransaction.WaitForTransactionReceipt(client: _client, chainId: WebGLMetaMask.Instance.GetActiveChainId(), txHash: hash);
        }

        public Task<bool> IsConnected()
        {
            return Task.FromResult(_isConnected);
        }

        public async Task Disconnect()
        {
            _isConnected = false;
            await AppKit.DisconnectAsync();
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

        public Task<List<LinkedAccount>> LinkAccount(
            IThirdwebWallet walletToLink,
            string otp = null,
            bool? isMobile = null,
            Action<string> browserOpenAction = null,
            string mobileRedirectScheme = "thirdweb://",
            IThirdwebBrowser browser = null,
            BigInteger? chainId = null,
            string jwt = null,
            string payload = null
        )
        {
            throw new InvalidOperationException("LinkAccount is not supported by external wallets.");
        }

        public Task<List<LinkedAccount>> GetLinkedAccounts()
        {
            throw new InvalidOperationException("GetLinkedAccounts is not supported by external wallets.");
        }

        #endregion
    }
}
