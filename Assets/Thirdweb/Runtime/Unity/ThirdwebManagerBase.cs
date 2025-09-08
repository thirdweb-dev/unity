using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Thirdweb.Unity
{
    [Serializable]
    public enum WalletProvider
    {
        PrivateKeyWallet,
        InAppWallet,
        ReownWallet,
        MetaMaskWallet,
        EcosystemWallet,
    }

    [Serializable]
    public class InAppWalletOptions : EcosystemWalletOptions
    {
        public InAppWalletOptions(
            string email = null,
            string phoneNumber = null,
            AuthProvider authprovider = AuthProvider.Default,
            string jwtOrPayload = null,
            string storageDirectoryPath = null,
            IThirdwebWallet siweSigner = null,
            string legacyEncryptionKey = null,
            string walletSecret = null,
            List<string> forceSiweExternalWalletIds = null,
            ExecutionMode executionMode = ExecutionMode.EOA
        )
            : base(
                email: email,
                phoneNumber: phoneNumber,
                authprovider: authprovider,
                jwtOrPayload: jwtOrPayload,
                storageDirectoryPath: storageDirectoryPath,
                siweSigner: siweSigner,
                legacyEncryptionKey: legacyEncryptionKey,
                walletSecret: walletSecret,
                forceSiweExternalWalletIds: forceSiweExternalWalletIds,
                executionMode: executionMode
            ) { }
    }

    [Serializable]
    public class EcosystemWalletOptions
    {
        [JsonProperty("ecosystemId")]
        public string EcosystemId;

        [JsonProperty("ecosystemPartnerId")]
        public string EcosystemPartnerId;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("phoneNumber")]
        public string PhoneNumber;

        [JsonProperty("authProvider")]
        public AuthProvider AuthProvider;

        [JsonProperty("jwtOrPayload")]
        public string JwtOrPayload;

        [JsonProperty("storageDirectoryPath")]
        public string StorageDirectoryPath;

        [JsonProperty("siweSigner")]
        public IThirdwebWallet SiweSigner;

        [JsonProperty("legacyEncryptionKey")]
        public string LegacyEncryptionKey;

        [JsonProperty("walletSecret")]
        public string WalletSecret;

        [JsonProperty("forceSiweExternalWalletIds")]
        public List<string> ForceSiweExternalWalletIds;

        [JsonProperty("executionMode")]
        public ExecutionMode ExecutionMode = ExecutionMode.EOA;

        public EcosystemWalletOptions(
            string ecosystemId = null,
            string ecosystemPartnerId = null,
            string email = null,
            string phoneNumber = null,
            AuthProvider authprovider = AuthProvider.Default,
            string jwtOrPayload = null,
            string storageDirectoryPath = null,
            IThirdwebWallet siweSigner = null,
            string legacyEncryptionKey = null,
            string walletSecret = null,
            List<string> forceSiweExternalWalletIds = null,
            ExecutionMode executionMode = ExecutionMode.EOA
        )
        {
            this.EcosystemId = ecosystemId;
            this.EcosystemPartnerId = ecosystemPartnerId;
            this.Email = email;
            this.PhoneNumber = phoneNumber;
            this.AuthProvider = authprovider;
            this.JwtOrPayload = jwtOrPayload;
            this.StorageDirectoryPath = storageDirectoryPath ?? Path.Combine(Application.persistentDataPath, "Thirdweb", "EcosystemWallet");
            this.SiweSigner = siweSigner;
            this.LegacyEncryptionKey = legacyEncryptionKey;
            this.WalletSecret = walletSecret;
            this.ForceSiweExternalWalletIds = forceSiweExternalWalletIds;
            this.ExecutionMode = executionMode;
        }
    }

    [Serializable]
    public class SmartWalletOptions
    {
        [JsonProperty("sponsorGas")]
        public bool SponsorGas;

        [JsonProperty("factoryAddress")]
        public string FactoryAddress;

        [JsonProperty("accountAddressOverride")]
        public string AccountAddressOverride;

        [JsonProperty("entryPoint")]
        public string EntryPoint;

        [JsonProperty("bundlerUrl")]
        public string BundlerUrl;

        [JsonProperty("paymasterUrl")]
        public string PaymasterUrl;

        [JsonProperty("tokenPaymaster")]
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
            this.SponsorGas = sponsorGas;
            this.FactoryAddress = factoryAddress;
            this.AccountAddressOverride = accountAddressOverride;
            this.EntryPoint = entryPoint;
            this.BundlerUrl = bundlerUrl;
            this.PaymasterUrl = paymasterUrl;
            this.TokenPaymaster = tokenPaymaster;
        }
    }

    [Serializable]
    public class ReownOptions
    {
        [JsonProperty("projectId")]
        public string ProjectId;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("url")]
        public string Url;

        [JsonProperty("iconUrl")]
        public string IconUrl;

        [JsonProperty("includedWalletIds")]
        public string[] IncludedWalletIds;

        [JsonProperty("excludedWalletIds")]
        public string[] ExcludedWalletIds;

        public ReownOptions(
            string projectId = null,
            string name = null,
            string description = null,
            string url = null,
            string iconUrl = null,
            string[] includedWalletIds = null,
            string[] excludedWalletIds = null
        )
        {
            this.ProjectId = projectId ?? "35603765088f9ed24db818100fdbb6f9";
            this.Name = name ?? "thirdweb";
            this.Description = description ?? "thirdweb powered game";
            this.Url = url ?? "https://thirdweb.com";
            this.IconUrl = iconUrl ?? "https://thirdweb.com/favicon.ico";
            this.IncludedWalletIds = includedWalletIds;
            this.ExcludedWalletIds = excludedWalletIds;
        }
    }

    [Serializable]
    public class WalletOptions
    {
        [JsonProperty("provider")]
        public WalletProvider Provider;

        [JsonProperty("chainId")]
        public BigInteger ChainId;

        [JsonProperty("inAppWalletOptions")]
        public InAppWalletOptions InAppWalletOptions;

        [JsonProperty("ecosystemWalletOptions", NullValueHandling = NullValueHandling.Ignore)]
        public EcosystemWalletOptions EcosystemWalletOptions;

        [JsonProperty("smartWalletOptions", NullValueHandling = NullValueHandling.Ignore)]
        public SmartWalletOptions SmartWalletOptions;

        [JsonProperty("reownOptions", NullValueHandling = NullValueHandling.Ignore)]
        public ReownOptions ReownOptions;

        public WalletOptions(
            WalletProvider provider,
            BigInteger chainId,
            InAppWalletOptions inAppWalletOptions = null,
            EcosystemWalletOptions ecosystemWalletOptions = null,
            SmartWalletOptions smartWalletOptions = null,
            ReownOptions reownOptions = null
        )
        {
            this.Provider = provider;
            this.ChainId = chainId;
            this.InAppWalletOptions = inAppWalletOptions ?? new InAppWalletOptions();
            this.SmartWalletOptions = smartWalletOptions;
            this.EcosystemWalletOptions = ecosystemWalletOptions;
            this.ReownOptions = reownOptions ?? new ReownOptions();
        }
    }

    [Serializable]
    public struct RpcOverride
    {
        public ulong ChainId;
        public string RpcUrl;
    }

    [HelpURL("http://portal.thirdweb.com/unity/v5/thirdwebmanager")]
    public abstract class ThirdwebManagerBase : MonoBehaviour
    {
        [field: SerializeField]
        protected bool InitializeOnAwake { get; set; } = true;

        [field: SerializeField]
        protected bool ShowDebugLogs { get; set; } = true;

        [field: SerializeField]
        protected bool AutoConnectLastWallet { get; set; } = false;

        [field: SerializeField]
        protected string RedirectPageHtmlOverride { get; set; } = null;

        [field: SerializeField]
        protected List<RpcOverride> RpcOverrides { get; set; } = null;

        public ThirdwebClient Client { get; protected set; }
        public IThirdwebWallet ActiveWallet { get; protected set; }
        public bool Initialized { get; protected set; }

        public static ThirdwebManagerBase Instance { get; protected set; }

        public static readonly string THIRDWEB_UNITY_SDK_VERSION = "6.0.0";

        protected const string THIRDWEB_AUTO_CONNECT_OPTIONS_KEY = "ThirdwebAutoConnectOptions";

        protected Dictionary<string, IThirdwebWallet> WalletMapping;

        public abstract string MobileRedirectScheme { get; }

        protected abstract ThirdwebClient CreateClient();

        // ------------------------------------------------------
        // Lifecycle Methods
        // ------------------------------------------------------

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
                return;
            }

            ThirdwebDebug.IsEnabled = this.ShowDebugLogs;

            if (this.InitializeOnAwake)
            {
                this.Initialize();
            }
        }

        public virtual async void Initialize()
        {
            this.Client = this.CreateClient();
            if (this.Client == null)
            {
                ThirdwebDebug.LogError("Failed to initialize ThirdwebManager.");
                return;
            }

            ThirdwebDebug.Log("ThirdwebManager initialized.");

            this.WalletMapping = new Dictionary<string, IThirdwebWallet>();

            if (this.AutoConnectLastWallet && this.GetAutoConnectOptions(out var lastWalletOptions))
            {
                ThirdwebDebug.Log("Auto-connecting to last wallet.");
                try
                {
                    _ = await this.ConnectWallet(lastWalletOptions);
                    ThirdwebDebug.Log("Auto-connected to last wallet.");
                }
                catch (Exception e)
                {
                    ThirdwebDebug.LogError("Failed to auto-connect to last wallet: " + e.Message);
                }
            }

            this.Initialized = true;
        }

        // ------------------------------------------------------
        // Contract Methods
        // ------------------------------------------------------

        public virtual async Task<ThirdwebContract> GetContract(string address, BigInteger chainId, string abi = null)
        {
            if (!this.Initialized)
            {
                throw new InvalidOperationException("ThirdwebManager is not initialized.");
            }

            return await ThirdwebContract.Create(this.Client, address, chainId, abi);
        }

        // ------------------------------------------------------
        // Active Wallet Methods
        // ------------------------------------------------------

        public virtual IThirdwebWallet GetActiveWallet()
        {
            return this.ActiveWallet;
        }

        public virtual void SetActiveWallet(IThirdwebWallet wallet)
        {
            this.ActiveWallet = wallet;
        }

        public virtual IThirdwebWallet GetWallet(string address)
        {
            if (this.WalletMapping.TryGetValue(address, out var wallet))
            {
                return wallet;
            }

            throw new KeyNotFoundException($"Wallet with address {address} not found.");
        }

        public virtual async Task<IThirdwebWallet> AddWallet(IThirdwebWallet wallet)
        {
            var address = await wallet.GetAddress();
            _ = this.WalletMapping.TryAdd(address, wallet);
            return wallet;
        }

        public virtual void RemoveWallet(string address)
        {
            if (this.WalletMapping.ContainsKey(address))
            {
                _ = this.WalletMapping.Remove(address, out _);
            }
        }

        // ------------------------------------------------------
        // Connection Methods
        // ------------------------------------------------------

        public virtual async Task<IThirdwebWallet> ConnectWallet(WalletOptions walletOptions)
        {
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
                    wallet = await PrivateKeyWallet.Generate(client: this.Client);
                    break;
                case WalletProvider.InAppWallet:
                    wallet = await InAppWallet.Create(
                        client: this.Client,
                        email: walletOptions.InAppWalletOptions.Email,
                        phoneNumber: walletOptions.InAppWalletOptions.PhoneNumber,
                        authProvider: walletOptions.InAppWalletOptions.AuthProvider,
                        storageDirectoryPath: walletOptions.InAppWalletOptions.StorageDirectoryPath,
                        siweSigner: walletOptions.InAppWalletOptions.SiweSigner,
                        legacyEncryptionKey: walletOptions.InAppWalletOptions.LegacyEncryptionKey,
                        walletSecret: walletOptions.InAppWalletOptions.WalletSecret,
                        executionMode: walletOptions.InAppWalletOptions.ExecutionMode
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
                        client: this.Client,
                        ecosystemId: walletOptions.EcosystemWalletOptions.EcosystemId,
                        ecosystemPartnerId: walletOptions.EcosystemWalletOptions.EcosystemPartnerId,
                        email: walletOptions.EcosystemWalletOptions.Email,
                        phoneNumber: walletOptions.EcosystemWalletOptions.PhoneNumber,
                        authProvider: walletOptions.EcosystemWalletOptions.AuthProvider,
                        storageDirectoryPath: walletOptions.EcosystemWalletOptions.StorageDirectoryPath,
                        siweSigner: walletOptions.EcosystemWalletOptions.SiweSigner,
                        legacyEncryptionKey: walletOptions.EcosystemWalletOptions.LegacyEncryptionKey,
                        walletSecret: walletOptions.EcosystemWalletOptions.WalletSecret,
                        executionMode: walletOptions.EcosystemWalletOptions.ExecutionMode
                    );
                    break;
                case WalletProvider.ReownWallet:
                    wallet = await ReownWallet.Create(
                        client: this.Client,
                        activeChainId: walletOptions.ChainId,
                        projectId: walletOptions.ReownOptions.ProjectId,
                        name: walletOptions.ReownOptions.Name,
                        description: walletOptions.ReownOptions.Description,
                        url: walletOptions.ReownOptions.Url,
                        iconUrl: walletOptions.ReownOptions.IconUrl,
                        includedWalletIds: walletOptions.ReownOptions.IncludedWalletIds,
                        excludedWalletIds: walletOptions.ReownOptions.ExcludedWalletIds
                    );
                    break;
                case WalletProvider.MetaMaskWallet:
                    wallet = await MetaMaskWallet.Create(client: this.Client, activeChainId: walletOptions.ChainId);
                    break;
                default:
                    throw new NotSupportedException($"Wallet provider {walletOptions.Provider} is not supported.");
            }

            // InAppWallet auth flow
            if (walletOptions.Provider == WalletProvider.InAppWallet && !await wallet.IsConnected())
            {
                ThirdwebDebug.Log("Session does not exist or is expired, proceeding with InAppWallet authentication.");

                var inAppWallet = wallet as InAppWallet;
                switch (walletOptions.InAppWalletOptions.AuthProvider)
                {
                    case AuthProvider.Default:
                        await inAppWallet.SendOTP();
                        _ = await InAppWalletModal.LoginWithOtp(inAppWallet);
                        break;
                    case AuthProvider.Siwe:
                        _ = await inAppWallet.LoginWithSiwe(walletOptions.ChainId);
                        break;
                    case AuthProvider.JWT:
                        _ = await inAppWallet.LoginWithJWT(walletOptions.InAppWalletOptions.JwtOrPayload);
                        break;
                    case AuthProvider.AuthEndpoint:
                        _ = await inAppWallet.LoginWithAuthEndpoint(walletOptions.InAppWalletOptions.JwtOrPayload);
                        break;
                    case AuthProvider.Guest:
                        _ = await inAppWallet.LoginWithGuest(SystemInfo.deviceUniqueIdentifier);
                        break;
                    case AuthProvider.Backend:
                        _ = await inAppWallet.LoginWithBackend();
                        break;
                    case AuthProvider.SiweExternal:
                        _ = await inAppWallet.LoginWithSiweExternal(
                            isMobile: Application.isMobilePlatform,
                            browserOpenAction: (url) => Application.OpenURL(url),
                            forceWalletIds: walletOptions.InAppWalletOptions.ForceSiweExternalWalletIds == null || walletOptions.InAppWalletOptions.ForceSiweExternalWalletIds.Count == 0
                                ? null
                                : walletOptions.InAppWalletOptions.ForceSiweExternalWalletIds,
                            mobileRedirectScheme: this.MobileRedirectScheme,
                            browser: new CrossPlatformUnityBrowser(this.RedirectPageHtmlOverride)
                        );
                        break;
                    case AuthProvider.Google:
                    case AuthProvider.Apple:
                    case AuthProvider.Facebook:
                    case AuthProvider.Discord:
                    case AuthProvider.Farcaster:
                    case AuthProvider.Telegram:
                    case AuthProvider.Line:
                    case AuthProvider.X:
                    case AuthProvider.TikTok:
                    case AuthProvider.Coinbase:
                    case AuthProvider.Github:
                    case AuthProvider.Twitch:
                    case AuthProvider.Steam:
                    default:
                        _ = await inAppWallet.LoginWithOauth(
                            isMobile: Application.isMobilePlatform,
                            browserOpenAction: (url) => Application.OpenURL(url),
                            mobileRedirectScheme: this.MobileRedirectScheme,
                            browser: new CrossPlatformUnityBrowser(this.RedirectPageHtmlOverride)
                        );
                        break;
                }
            }

            // EcosystemWallet auth flow
            if (walletOptions.Provider == WalletProvider.EcosystemWallet && !await wallet.IsConnected())
            {
                ThirdwebDebug.Log("Session does not exist or is expired, proceeding with EcosystemWallet authentication.");

                var ecosystemWallet = wallet as EcosystemWallet;
                switch (walletOptions.EcosystemWalletOptions.AuthProvider)
                {
                    case AuthProvider.Default:
                        await ecosystemWallet.SendOTP();
                        _ = await EcosystemWalletModal.LoginWithOtp(ecosystemWallet);
                        break;
                    case AuthProvider.Siwe:
                        _ = await ecosystemWallet.LoginWithSiwe(walletOptions.ChainId);
                        break;
                    case AuthProvider.JWT:
                        _ = await ecosystemWallet.LoginWithJWT(walletOptions.EcosystemWalletOptions.JwtOrPayload);
                        break;
                    case AuthProvider.AuthEndpoint:
                        _ = await ecosystemWallet.LoginWithAuthEndpoint(walletOptions.EcosystemWalletOptions.JwtOrPayload);
                        break;
                    case AuthProvider.Guest:
                        _ = await ecosystemWallet.LoginWithGuest(SystemInfo.deviceUniqueIdentifier);
                        break;
                    case AuthProvider.Backend:
                        _ = await ecosystemWallet.LoginWithBackend();
                        break;
                    case AuthProvider.SiweExternal:
                        _ = await ecosystemWallet.LoginWithSiweExternal(
                            isMobile: Application.isMobilePlatform,
                            browserOpenAction: (url) => Application.OpenURL(url),
                            forceWalletIds: walletOptions.EcosystemWalletOptions.ForceSiweExternalWalletIds == null || walletOptions.EcosystemWalletOptions.ForceSiweExternalWalletIds.Count == 0
                                ? null
                                : walletOptions.EcosystemWalletOptions.ForceSiweExternalWalletIds,
                            mobileRedirectScheme: this.MobileRedirectScheme,
                            browser: new CrossPlatformUnityBrowser(this.RedirectPageHtmlOverride)
                        );
                        break;
                    case AuthProvider.Google:
                    case AuthProvider.Apple:
                    case AuthProvider.Facebook:
                    case AuthProvider.Discord:
                    case AuthProvider.Farcaster:
                    case AuthProvider.Telegram:
                    case AuthProvider.Line:
                    case AuthProvider.X:
                    case AuthProvider.TikTok:
                    case AuthProvider.Coinbase:
                    case AuthProvider.Github:
                    case AuthProvider.Twitch:
                    case AuthProvider.Steam:
                    default:
                        _ = await ecosystemWallet.LoginWithOauth(
                            isMobile: Application.isMobilePlatform,
                            browserOpenAction: (url) => Application.OpenURL(url),
                            mobileRedirectScheme: this.MobileRedirectScheme,
                            browser: new CrossPlatformUnityBrowser(this.RedirectPageHtmlOverride)
                        );
                        break;
                }
            }

            var address = await wallet.GetAddress();
            var isSmartWallet = walletOptions.SmartWalletOptions != null;

            this.SetAutoConnectOptions(walletOptions);

            // If SmartWallet, do upgrade
            if (isSmartWallet)
            {
                ThirdwebDebug.Log("Upgrading to SmartWallet.");
                return await this.UpgradeToSmartWallet(wallet, walletOptions.ChainId, walletOptions.SmartWalletOptions);
            }
            else
            {
                _ = await this.AddWallet(wallet);
                this.SetActiveWallet(wallet);
                return wallet;
            }
        }

        public virtual async Task<SmartWallet> UpgradeToSmartWallet(IThirdwebWallet personalWallet, BigInteger chainId, SmartWalletOptions smartWalletOptions)
        {
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

            _ = await this.AddWallet(wallet);
            this.SetActiveWallet(wallet);

            // Persist "smartWalletOptions" to auto-connect
            if (this.AutoConnectLastWallet && this.GetAutoConnectOptions(out var lastWalletOptions))
            {
                lastWalletOptions.SmartWalletOptions = smartWalletOptions;
                this.SetAutoConnectOptions(lastWalletOptions);
            }

            return wallet;
        }

        public virtual async Task<List<LinkedAccount>> LinkAccount(IThirdwebWallet mainWallet, IThirdwebWallet walletToLink, string otp = null, BigInteger? chainId = null, string jwtOrPayload = null)
        {
            return await mainWallet.LinkAccount(
                walletToLink: walletToLink,
                otp: otp,
                isMobile: Application.isMobilePlatform,
                browserOpenAction: (url) => Application.OpenURL(url),
                mobileRedirectScheme: this.MobileRedirectScheme,
                browser: new CrossPlatformUnityBrowser(this.RedirectPageHtmlOverride),
                chainId: chainId,
                jwt: jwtOrPayload,
                payload: jwtOrPayload
            );
        }

        protected virtual bool GetAutoConnectOptions(out WalletOptions lastWalletOptions)
        {
            var connectOptionsStr = PlayerPrefs.GetString(THIRDWEB_AUTO_CONNECT_OPTIONS_KEY, null);
            if (!string.IsNullOrEmpty(connectOptionsStr))
            {
                try
                {
                    lastWalletOptions = JsonConvert.DeserializeObject<WalletOptions>(connectOptionsStr);
                    return true;
                }
                catch
                {
                    ThirdwebDebug.LogWarning("Failed to load last wallet options.");
                    PlayerPrefs.DeleteKey(THIRDWEB_AUTO_CONNECT_OPTIONS_KEY);
                    lastWalletOptions = null;
                    return false;
                }
            }
            lastWalletOptions = null;
            return false;
        }

        protected virtual void SetAutoConnectOptions(WalletOptions walletOptions)
        {
            if (this.AutoConnectLastWallet)
            {
                try
                {
                    PlayerPrefs.SetString(THIRDWEB_AUTO_CONNECT_OPTIONS_KEY, JsonConvert.SerializeObject(walletOptions));
                }
                catch
                {
                    ThirdwebDebug.LogWarning("Failed to save last wallet options.");
                    PlayerPrefs.DeleteKey(THIRDWEB_AUTO_CONNECT_OPTIONS_KEY);
                }
            }
        }
    }
}
