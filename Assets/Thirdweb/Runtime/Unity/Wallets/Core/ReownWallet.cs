using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.EIP712;
using Reown.AppKit.Unity;

namespace Thirdweb.Unity
{
    public class ReownWallet : IThirdwebWallet
    {
        public ThirdwebClient Client => _client;
        public string WalletId => "reown";
        public ThirdwebAccountType AccountType => ThirdwebAccountType.ExternalAccount;

        public static BigInteger ActiveChainId;

        private static ThirdwebClient _client;

        protected ReownWallet() { }

        public static async Task<ReownWallet> Create(
            ThirdwebClient client,
            BigInteger activeChainId,
            string projectId,
            string name,
            string description,
            string url,
            string iconUrl,
            string[] includedWalletIds,
            string[] excludedWalletIds
        )
        {
            _client = client;

            var wallet = new ReownWallet();

            var wcChains = new List<Chain>(); // ChainConstants.Chains.All.ToList();

            if (wcChains.Any(c => c.ChainReference == activeChainId.ToString()))
            {
                ThirdwebDebug.Log($"The chain with ID {activeChainId} is already supported by Reown. No need to add it manually.");
            }
            else
            {
                ThirdwebDebug.Log($"The chain with ID {activeChainId} is not supported by Reown. Adding it manually.");
                wcChains.Add(ToWcChain(client, activeChainId));
            }

            var appKitConfig = new AppKitConfig
            {
                projectId = projectId,
                includedWalletIds = includedWalletIds,
                excludedWalletIds = excludedWalletIds,
                metadata = new Metadata(name: name, description: description, url: url, iconUrl: iconUrl, redirect: new RedirectData() { Native = ThirdwebManager.Instance.MobileRedirectScheme }),
                enableEmail = false,
                enableOnramp = false,
                enableCoinbaseWallet = false,
                socials = Array.Empty<SocialLogin>(),
                supportedChains = wcChains.ToArray(),
            };

            if (!AppKit.IsInitialized)
            {
                await AppKit.InitializeAsync(appKitConfig);
            }

            ThirdwebDebug.Log("Reown AppKit initialized.");

            var connected = false;

            try
            {
                connected = await AppKit.ConnectorController.TryResumeSessionAsync();
            }
            catch (Exception e)
            {
                ThirdwebDebug.LogWarning($"Failed to resume Reown session: {e.Message}");
                connected = false;
            }

            if (connected)
            {
                ThirdwebDebug.Log("Resumed previous Reown session.");
            }
            else
            {
                ThirdwebDebug.Log("Opening Reown modal... Timeout in 120 seconds.");
                AppKit.AccountConnected += (_, e) => connected = true;
                AppKit.OpenModal();

                var timeoutMs = 120000; // 120 seconds
                var intervalMs = 250; // 0.25 second
                while (!connected && timeoutMs > 0)
                {
                    await ThirdwebTask.Delay(intervalMs);
                    timeoutMs -= intervalMs;
                }
            }

            if (!connected)
            {
                throw new Exception("Reown connection timed out.");
            }

            ThirdwebDebug.Log("Reown wallet connected.");

            if (AppKit.Account.ChainId != activeChainId.ToString())
            {
                ThirdwebDebug.Log($"Switching Reown from current chain id {AppKit.Account.ChainId} to chain ID {activeChainId}...");
                await wallet.SwitchNetwork(activeChainId);
            }

            ActiveChainId = activeChainId;

            return wallet;
        }

        public Task<string> GetAddress()
        {
            var addy = AppKit.Account.Address;
            if (string.IsNullOrEmpty(addy))
            {
                throw new Exception("No connected account address found");
            }
            return Task.FromResult(addy.ToChecksumAddress());
        }

        public Task<string> EthSign(byte[] rawMessage)
        {
            throw new NotImplementedException("Reown does not support signing raw messages.");
        }

        public Task<string> EthSign(string message)
        {
            throw new NotImplementedException("Reown does not support signing raw messages.");
        }

        public Task<string> RecoverAddressFromEthSign(string message, string signature)
        {
            throw new NotImplementedException();
        }

        public async Task<string> PersonalSign(byte[] rawMessage)
        {
            return await AppKit.Evm.SignMessageAsync(rawMessage);
        }

        public Task<string> PersonalSign(string message)
        {
            var rawMessage = System.Text.Encoding.UTF8.GetBytes(message);
            return this.PersonalSign(rawMessage);
        }

        public Task<string> RecoverAddressFromPersonalSign(string message, string signature)
        {
            var signer = new Nethereum.Signer.EthereumMessageSigner();
            var addressRecovered = signer.EncodeUTF8AndEcRecover(message, signature);
            return Task.FromResult(addressRecovered);
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
            return this.SignTypedDataV4(safeJson);
        }

        public Task<string> RecoverAddressFromTypedDataV4<T, TDomain>(T data, TypedData<TDomain> typedData, string signature)
            where TDomain : IDomain
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsConnected()
        {
            return Task.FromResult(AppKit.ConnectorController.IsAccountConnected);
        }

        public Task<string> SignTransaction(ThirdwebTransactionInput transaction)
        {
            throw new NotImplementedException("MetaMask does not support raw transaction signing.");
        }

        public async Task<string> SendTransaction(ThirdwebTransactionInput transaction)
        {
            return await AppKit.Evm.SendTransactionAsync(addressTo: transaction.To, value: transaction.Value?.Value ?? 0, data: transaction.Data);
        }

        public async Task<ThirdwebTransactionReceipt> ExecuteTransaction(ThirdwebTransactionInput transaction)
        {
            var hash = await this.SendTransaction(transaction);
            return await ThirdwebTransaction.WaitForTransactionReceipt(this.Client, ActiveChainId, hash);
        }

        public Task Disconnect()
        {
            AppKit.CloseModal();
            return AppKit.ConnectorController.DisconnectAsync();
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
            throw new InvalidOperationException("Reown does not support linking accounts.");
        }

        public Task<List<LinkedAccount>> UnlinkAccount(LinkedAccount accountToUnlink)
        {
            throw new InvalidOperationException("Reown does not support unlinking accounts.");
        }

        public Task<List<LinkedAccount>> GetLinkedAccounts()
        {
            throw new InvalidOperationException("Reown does not support linked accounts.");
        }

        public Task<EIP7702Authorization> SignAuthorization(BigInteger chainId, string contractAddress, bool willSelfExecute)
        {
            throw new InvalidOperationException("Reown does not support signing EIP-7702 authorizations.");
        }

        public async Task SwitchNetwork(BigInteger chainId)
        {
            await AppKit.NetworkController.ChangeActiveChainAsync(ToWcChain(this.Client, chainId));
            ThirdwebDebug.Log($"Switched Reown to chain ID {chainId}.");
        }

        internal static Chain ToWcChain(ThirdwebClient client, BigInteger chainId)
        {
            var wcChain = ChainConstants.Chains.All.FirstOrDefault(c => c.ChainReference == chainId.ToString());

            if (wcChain != null)
            {
                return wcChain;
            }

            var twChainMeta = Utils.GetChainMetadata(client, chainId).GetAwaiter().GetResult();
            return new Chain(
                chainNamespace: ChainConstants.Namespaces.Evm,
                chainReference: chainId.ToString(),
                name: twChainMeta.Name ?? "Ethereum",
                nativeCurrency: new Currency(twChainMeta.NativeCurrency?.Name, twChainMeta.NativeCurrency?.Symbol, twChainMeta.NativeCurrency.Decimals == 0 ? 18 : twChainMeta.NativeCurrency.Decimals),
                blockExplorer: new BlockExplorer(name: twChainMeta.Explorers?[0].Name ?? "Etherscan", url: twChainMeta.Explorers?[0].Url ?? "https://etherscan.io"),
                rpcUrl: $"https://{chainId}.rpc.thirdweb.com/{client.ClientId}",
                isTestnet: twChainMeta.Testnet,
                imageUrl: twChainMeta.Icon?.Url ?? string.Empty
            );
        }
    }
}
