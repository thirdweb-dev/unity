using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;

namespace Thirdweb.Unity
{
    public enum WalletProvider
    {
        PrivateKeyWallet,
        InAppWallet,
        WalletConnectWallet,
        MetaMaskWallet,
        EcosystemWallet
    }

    public class InAppWalletOptions : EcosystemWalletOptions
    {
        public InAppWalletOptions(
            string email = null,
            string phoneNumber = null,
            AuthProvider authprovider = AuthProvider.Default,
            string jwtOrPayload = null,
            string legacyEncryptionKey = null,
            string storageDirectoryPath = null,
            IThirdwebWallet siweSigner = null
        )
            : base(
                email: email,
                phoneNumber: phoneNumber,
                authprovider: authprovider,
                jwtOrPayload: jwtOrPayload,
                storageDirectoryPath: storageDirectoryPath,
                siweSigner: siweSigner,
                legacyEncryptionKey: legacyEncryptionKey
            ) { }
    }

    public class EcosystemWalletOptions
    {
        public string EcosystemId;
        public string EcosystemPartnerId;
        public string Email;
        public string PhoneNumber;
        public AuthProvider AuthProvider;
        public string JwtOrPayload;
        public string StorageDirectoryPath;
        public IThirdwebWallet SiweSigner;
        public string LegacyEncryptionKey;

        public EcosystemWalletOptions(
            string ecosystemId = null,
            string ecosystemPartnerId = null,
            string email = null,
            string phoneNumber = null,
            AuthProvider authprovider = AuthProvider.Default,
            string jwtOrPayload = null,
            string storageDirectoryPath = null,
            IThirdwebWallet siweSigner = null,
            string legacyEncryptionKey = null
        )
        {
            EcosystemId = ecosystemId;
            EcosystemPartnerId = ecosystemPartnerId;
            Email = email;
            PhoneNumber = phoneNumber;
            AuthProvider = authprovider;
            JwtOrPayload = jwtOrPayload;
            StorageDirectoryPath = storageDirectoryPath ?? Path.Combine(Application.persistentDataPath, "Thirdweb", "EcosystemWallet");
            SiweSigner = siweSigner;
            LegacyEncryptionKey = legacyEncryptionKey;
        }
    }

    public class SmartWalletOptions
    {
        public bool SponsorGas;
        public string FactoryAddress;
        public string AccountAddressOverride;
        public string EntryPoint;
        public string BundlerUrl;
        public string PaymasterUrl;
        public TokenPaymaster TokenPaymaster;

        public SmartWalletOptions(
            bool sponsorGas,
            string factoryAddress = null,
            string accountAddressOverride = null,
            string entryPoint = null,
            string bundlerUrl = null,
            string paymasterUrl = null,
            TokenPaymaster tokenPaymaster = TokenPaymaster.NONE
        )
        {
            SponsorGas = sponsorGas;
            FactoryAddress = factoryAddress;
            AccountAddressOverride = accountAddressOverride;
            EntryPoint = entryPoint;
            BundlerUrl = bundlerUrl;
            PaymasterUrl = paymasterUrl;
            TokenPaymaster = tokenPaymaster;
        }
    }

    public class WalletOptions
    {
        public WalletProvider Provider;
        public BigInteger ChainId;
        public InAppWalletOptions InAppWalletOptions;
        public EcosystemWalletOptions EcosystemWalletOptions;
        public SmartWalletOptions SmartWalletOptions;

        public WalletOptions(
            WalletProvider provider,
            BigInteger chainId,
            InAppWalletOptions inAppWalletOptions = null,
            EcosystemWalletOptions ecosystemWalletOptions = null,
            SmartWalletOptions smartWalletOptions = null
        )
        {
            Provider = provider;
            ChainId = chainId;
            InAppWalletOptions = inAppWalletOptions ?? new InAppWalletOptions();
            SmartWalletOptions = smartWalletOptions;
            EcosystemWalletOptions = ecosystemWalletOptions;
        }
    }

    [HelpURL("http://portal.thirdweb.com/unity/v5/thirdwebmanager")]
    public class ThirdwebManager : MonoBehaviour
    {
        [field: SerializeField]
        private string ClientId { get; set; }

        [field: SerializeField]
        private string BundleId { get; set; }

        [field: SerializeField]
        private bool InitializeOnAwake { get; set; } = true;

        [field: SerializeField]
        private bool ShowDebugLogs { get; set; } = true;

        [field: SerializeField]
        private bool OptOutUsageAnalytics { get; set; } = false;

        [field: SerializeField]
        private ulong[] SupportedChains { get; set; } = new ulong[] { 421614 };

        [field: SerializeField]
        private string RedirectPageHtmlOverride { get; set; } = null;

        public ThirdwebClient Client { get; private set; }

        public IThirdwebWallet ActiveWallet { get; private set; }

        public static ThirdwebManager Instance { get; private set; }

        public static readonly string THIRDWEB_UNITY_SDK_VERSION = "5.13.1";

        private bool _initialized;

        private Dictionary<string, IThirdwebWallet> _walletMapping;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            ThirdwebDebug.IsEnabled = ShowDebugLogs;

            if (InitializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                ThirdwebDebug.LogError("ClientId and must be set in order to initialize ThirdwebManager. Get your API key from https://thirdweb.com/create-api-key");
                return;
            }

            if (string.IsNullOrEmpty(BundleId))
            {
                BundleId = null;
            }

            BundleId ??= Application.identifier ?? $"com.{Application.companyName}.{Application.productName}";

            Client = ThirdwebClient.Create(
                clientId: ClientId,
                bundleId: BundleId,
                httpClient: new CrossPlatformUnityHttpClient(),
                sdkName: Application.platform == RuntimePlatform.WebGLPlayer ? "UnitySDK_WebGL" : "UnitySDK",
                sdkOs: Application.platform.ToString(),
                sdkPlatform: "unity",
                sdkVersion: THIRDWEB_UNITY_SDK_VERSION
            );

            ThirdwebDebug.Log("ThirdwebManager initialized.");

            _walletMapping = new Dictionary<string, IThirdwebWallet>();

            _initialized = true;
        }

        public async Task<ThirdwebContract> GetContract(string address, BigInteger chainId, string abi = null)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("ThirdwebManager is not initialized.");
            }

            return await ThirdwebContract.Create(Client, address, chainId, abi);
        }

        public IThirdwebWallet GetActiveWallet()
        {
            return ActiveWallet;
        }

        public void SetActiveWallet(IThirdwebWallet wallet)
        {
            ActiveWallet = wallet;
        }

        public IThirdwebWallet GetWallet(string address)
        {
            if (_walletMapping.TryGetValue(address, out var wallet))
            {
                return wallet;
            }

            throw new KeyNotFoundException($"Wallet with address {address} not found.");
        }

        public async Task<IThirdwebWallet> AddWallet(IThirdwebWallet wallet)
        {
            var address = await wallet.GetAddress();
            _walletMapping.TryAdd(address, wallet);
            return wallet;
        }

        public void RemoveWallet(string address)
        {
            if (_walletMapping.ContainsKey(address))
            {
                _walletMapping.Remove(address, out var wallet);
            }
        }

        public async Task<IThirdwebWallet> ConnectWallet(WalletOptions walletOptions)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("ThirdwebManager is not initialized.");
            }

            if (walletOptions == null)
            {
                throw new ArgumentNullException(nameof(walletOptions));
            }

            if (walletOptions.ChainId <= 0)
            {
                throw new ArgumentException("ChainId must be greater than 0.");
            }

            IThirdwebWallet wallet = null;

            switch (walletOptions.Provider)
            {
                case WalletProvider.PrivateKeyWallet:
                    wallet = await PrivateKeyWallet.Generate(client: Client);
                    break;
                case WalletProvider.InAppWallet:
                    wallet = await InAppWallet.Create(
                        client: Client,
                        email: walletOptions.InAppWalletOptions.Email,
                        phoneNumber: walletOptions.InAppWalletOptions.PhoneNumber,
                        authProvider: walletOptions.InAppWalletOptions.AuthProvider,
                        storageDirectoryPath: walletOptions.InAppWalletOptions.StorageDirectoryPath,
                        siweSigner: walletOptions.InAppWalletOptions.SiweSigner,
                        legacyEncryptionKey: walletOptions.InAppWalletOptions.LegacyEncryptionKey
                    );
                    break;
                case WalletProvider.EcosystemWallet:
                    if (walletOptions.EcosystemWalletOptions == null)
                    {
                        throw new ArgumentException("EcosystemWalletOptions must be provided for EcosystemWallet provider.");
                    }
                    if (string.IsNullOrEmpty(walletOptions.EcosystemWalletOptions.EcosystemId))
                    {
                        throw new ArgumentException("EcosystemId must be provided for EcosystemWallet provider.");
                    }
                    wallet = await EcosystemWallet.Create(
                        client: Client,
                        ecosystemId: walletOptions.EcosystemWalletOptions.EcosystemId,
                        ecosystemPartnerId: walletOptions.EcosystemWalletOptions.EcosystemPartnerId,
                        email: walletOptions.EcosystemWalletOptions.Email,
                        phoneNumber: walletOptions.EcosystemWalletOptions.PhoneNumber,
                        authProvider: walletOptions.EcosystemWalletOptions.AuthProvider,
                        storageDirectoryPath: walletOptions.EcosystemWalletOptions.StorageDirectoryPath,
                        siweSigner: walletOptions.EcosystemWalletOptions.SiweSigner,
                        legacyEncryptionKey: walletOptions.EcosystemWalletOptions.LegacyEncryptionKey
                    );
                    break;
                case WalletProvider.WalletConnectWallet:
                    var supportedChains = SupportedChains.Select(chain => new BigInteger(chain)).ToArray();
                    wallet = await WalletConnectWallet.Create(client: Client, initialChainId: walletOptions.ChainId, supportedChains: supportedChains);
                    break;
                case WalletProvider.MetaMaskWallet:
                    wallet = await MetaMaskWallet.Create(client: Client, activeChainId: walletOptions.ChainId);
                    break;
            }

            if (walletOptions.Provider == WalletProvider.InAppWallet && !await wallet.IsConnected())
            {
                ThirdwebDebug.Log("Session does not exist or is expired, proceeding with InAppWallet authentication.");

                var inAppWallet = wallet as InAppWallet;

                if (walletOptions.InAppWalletOptions.AuthProvider == AuthProvider.Default)
                {
                    await inAppWallet.SendOTP();
                    _ = await InAppWalletModal.LoginWithOtp(inAppWallet);
                }
                else if (walletOptions.InAppWalletOptions.AuthProvider == AuthProvider.Siwe)
                {
                    _ = await inAppWallet.LoginWithSiwe(walletOptions.ChainId);
                }
                else if (walletOptions.InAppWalletOptions.AuthProvider == AuthProvider.JWT)
                {
                    _ = await inAppWallet.LoginWithJWT(walletOptions.InAppWalletOptions.JwtOrPayload);
                }
                else if (walletOptions.InAppWalletOptions.AuthProvider == AuthProvider.AuthEndpoint)
                {
                    _ = await inAppWallet.LoginWithAuthEndpoint(walletOptions.InAppWalletOptions.JwtOrPayload);
                }
                else if (walletOptions.InAppWalletOptions.AuthProvider == AuthProvider.Guest)
                {
                    _ = await inAppWallet.LoginWithGuest();
                }
                else
                {
                    _ = await inAppWallet.LoginWithOauth(
                        isMobile: Application.isMobilePlatform,
                        browserOpenAction: (url) => Application.OpenURL(url),
                        mobileRedirectScheme: BundleId + "://",
                        browser: new CrossPlatformUnityBrowser(RedirectPageHtmlOverride)
                    );
                }
            }

            if (walletOptions.Provider == WalletProvider.EcosystemWallet && !await wallet.IsConnected())
            {
                ThirdwebDebug.Log("Session does not exist or is expired, proceeding with EcosystemWallet authentication.");

                var ecosystemWallet = wallet as EcosystemWallet;

                if (walletOptions.EcosystemWalletOptions.AuthProvider == AuthProvider.Default)
                {
                    await ecosystemWallet.SendOTP();
                    _ = await EcosystemWalletModal.LoginWithOtp(ecosystemWallet);
                }
                else if (walletOptions.EcosystemWalletOptions.AuthProvider == AuthProvider.Siwe)
                {
                    _ = await ecosystemWallet.LoginWithSiwe(walletOptions.ChainId);
                }
                else if (walletOptions.EcosystemWalletOptions.AuthProvider == AuthProvider.JWT)
                {
                    _ = await ecosystemWallet.LoginWithJWT(walletOptions.EcosystemWalletOptions.JwtOrPayload);
                }
                else if (walletOptions.EcosystemWalletOptions.AuthProvider == AuthProvider.AuthEndpoint)
                {
                    _ = await ecosystemWallet.LoginWithAuthEndpoint(walletOptions.EcosystemWalletOptions.JwtOrPayload);
                }
                else if (walletOptions.EcosystemWalletOptions.AuthProvider == AuthProvider.Guest)
                {
                    _ = await ecosystemWallet.LoginWithGuest();
                }
                else
                {
                    _ = await ecosystemWallet.LoginWithOauth(
                        isMobile: Application.isMobilePlatform,
                        browserOpenAction: (url) => Application.OpenURL(url),
                        mobileRedirectScheme: BundleId + "://",
                        browser: new CrossPlatformUnityBrowser(RedirectPageHtmlOverride)
                    );
                }
            }

            var address = await wallet.GetAddress();
            ThirdwebDebug.Log($"Wallet address: {address}");

            var isSmartWallet = walletOptions.SmartWalletOptions != null;

            if (!OptOutUsageAnalytics)
            {
                TrackUsage("connectWallet", "connect", isSmartWallet ? "smartWallet" : walletOptions.Provider.ToString()[..1].ToLower() + walletOptions.Provider.ToString()[1..], address);
            }

            if (isSmartWallet)
            {
                ThirdwebDebug.Log("Upgrading to SmartWallet.");
                return await UpgradeToSmartWallet(wallet, walletOptions.ChainId, walletOptions.SmartWalletOptions);
            }
            else
            {
                await AddWallet(wallet);
                SetActiveWallet(wallet);
                return wallet;
            }
        }

        public async Task<SmartWallet> UpgradeToSmartWallet(IThirdwebWallet personalWallet, BigInteger chainId, SmartWalletOptions smartWalletOptions)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("ThirdwebManager is not initialized.");
            }

            if (personalWallet.AccountType == ThirdwebAccountType.SmartAccount)
            {
                ThirdwebDebug.LogWarning("Wallet is already a SmartWallet.");
                return personalWallet as SmartWallet;
            }

            if (smartWalletOptions == null)
            {
                throw new ArgumentNullException(nameof(smartWalletOptions));
            }

            if (chainId <= 0)
            {
                throw new ArgumentException("ChainId must be greater than 0.");
            }

            var wallet = await SmartWallet.Create(
                personalWallet: personalWallet,
                chainId: chainId,
                gasless: smartWalletOptions.SponsorGas,
                factoryAddress: smartWalletOptions.FactoryAddress,
                accountAddressOverride: smartWalletOptions.AccountAddressOverride,
                entryPoint: smartWalletOptions.EntryPoint,
                bundlerUrl: smartWalletOptions.BundlerUrl,
                paymasterUrl: smartWalletOptions.PaymasterUrl,
                tokenPaymaster: smartWalletOptions.TokenPaymaster
            );

            await AddWallet(wallet);
            SetActiveWallet(wallet);

            return wallet;
        }

        public async Task<List<LinkedAccount>> LinkAccount(IThirdwebWallet mainWallet, IThirdwebWallet walletToLink, string otp = null, BigInteger? chainId = null, string jwtOrPayload = null)
        {
            return await mainWallet.LinkAccount(
                walletToLink: walletToLink,
                otp: otp,
                isMobile: Application.isMobilePlatform,
                browserOpenAction: (url) => Application.OpenURL(url),
                mobileRedirectScheme: BundleId + "://",
                browser: new CrossPlatformUnityBrowser(RedirectPageHtmlOverride),
                chainId: chainId,
                jwt: jwtOrPayload,
                payload: jwtOrPayload
            );
        }

        private async void TrackUsage(string source, string action, string walletType, string walletAddress)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(action) || string.IsNullOrEmpty(walletType) || string.IsNullOrEmpty(walletAddress))
            {
                ThirdwebDebug.LogWarning("Invalid usage analytics parameters.");
                return;
            }

            try
            {
                var content = new System.Net.Http.StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        new
                        {
                            source,
                            action,
                            walletAddress,
                            walletType,
                        }
                    ),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                _ = await Client.HttpClient.PostAsync("https://c.thirdweb.com/event", content);
            }
            catch
            {
                ThirdwebDebug.LogWarning($"Failed to report usage analytics.");
            }
        }
    }
}
