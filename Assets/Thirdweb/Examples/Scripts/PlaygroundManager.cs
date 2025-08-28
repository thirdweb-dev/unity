using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thirdweb.Unity.Examples
{
    [System.Serializable]
    public class WalletPanelUI
    {
        public string Identifier;
        public GameObject Panel;
        public Button Action1Button;
        public Button Action2Button;
        public Button Action3Button;
        public Button BackButton;
        public Button NextButton;
        public TMP_Text LogText;
        public TMP_InputField InputField;
        public Button InputFieldSubmitButton;
    }

    public class PlaygroundManager : MonoBehaviour
    {
        [field: SerializeField, Header("Wallet Options")]
        private ulong ActiveChainId = 84532;

        [field: SerializeField]
        private bool WebglForceMetamaskExtension = false;

        [field: SerializeField, Header("Connect Wallet")]
        private GameObject ConnectWalletPanel;

        [field: SerializeField]
        private Button PrivateKeyWalletButton;

        [field: SerializeField]
        private Button EcosystemWalletButton;

        [field: SerializeField]
        private Button WalletConnectButton;

        [field: SerializeField, Header("Wallet Panels")]
        private List<WalletPanelUI> WalletPanels;

        private ThirdwebChainData _chainDetails;

        private void Awake()
        {
            InitializePanels();
        }

        private async void Start()
        {
            try
            {
                _chainDetails = await Utils.GetChainMetadata(client: ThirdwebManager.Instance.Client, chainId: ActiveChainId);
            }
            catch
            {
                _chainDetails = new ThirdwebChainData()
                {
                    NativeCurrency = new ThirdwebChainNativeCurrency()
                    {
                        Decimals = 18,
                        Name = "ETH",
                        Symbol = "ETH",
                    },
                };
            }
        }

        private void InitializePanels()
        {
            CloseAllPanels();

            ConnectWalletPanel.SetActive(true);

            if (ThirdwebManager.Instance != null && ThirdwebManager.Instance.ActiveWallet != null)
            {
                ThirdwebManager.Instance.ActiveWallet.Disconnect();
            }

            PrivateKeyWalletButton.onClick.RemoveAllListeners();
            PrivateKeyWalletButton.onClick.AddListener(() =>
            {
                var options = GetWalletOptions(WalletProvider.PrivateKeyWallet);
                ConnectWallet(options);
            });

            EcosystemWalletButton.onClick.RemoveAllListeners();
            EcosystemWalletButton.onClick.AddListener(() => InitializeEcosystemWalletPanel());

            WalletConnectButton.onClick.RemoveAllListeners();
            WalletConnectButton.onClick.AddListener(() =>
            {
                var options = GetWalletOptions(WalletProvider.ReownWallet);
                ConnectWallet(options);
            });
        }

        private async void ConnectWallet(WalletOptions options)
        {
            // Connect the wallet

            var internalWalletProvider = options.Provider == WalletProvider.MetaMaskWallet ? WalletProvider.ReownWallet : options.Provider;
            var currentPanel = WalletPanels.Find(panel => panel.Identifier == internalWalletProvider.ToString());

            Log(currentPanel.LogText, $"Connecting...");

            var wallet = await ThirdwebManager.Instance.ConnectWallet(options);

            // Initialize the wallet panel

            CloseAllPanels();

            // Setup actions

            ClearLog(currentPanel.LogText);
            currentPanel.Panel.SetActive(true);

            currentPanel.BackButton.onClick.RemoveAllListeners();
            currentPanel.BackButton.onClick.AddListener(InitializePanels);

            currentPanel.NextButton.onClick.RemoveAllListeners();
            currentPanel.NextButton.onClick.AddListener(InitializeContractsPanel);

            currentPanel.Action1Button.onClick.RemoveAllListeners();
            currentPanel.Action1Button.onClick.AddListener(async () =>
            {
                var address = await wallet.GetAddress();
                address.CopyToClipboard();
                Log(currentPanel.LogText, $"Address: {address}");
            });

            currentPanel.Action2Button.onClick.RemoveAllListeners();
            currentPanel.Action2Button.onClick.AddListener(async () =>
            {
                var message = "Hello World!";
                var signature = await wallet.PersonalSign(message);
                Log(currentPanel.LogText, $"Signature: {signature}");
            });

            currentPanel.Action3Button.onClick.RemoveAllListeners();
            currentPanel.Action3Button.onClick.AddListener(async () =>
            {
                LoadingLog(currentPanel.LogText);
                var balance = await wallet.GetBalance(chainId: ActiveChainId);
                var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 4, addCommas: true);
                Log(currentPanel.LogText, $"Balance: {balanceEth} {_chainDetails.NativeCurrency.Symbol}");
            });
        }

        private WalletOptions GetWalletOptions(WalletProvider provider)
        {
            switch (provider)
            {
                case WalletProvider.PrivateKeyWallet:
                    return new WalletOptions(provider: WalletProvider.PrivateKeyWallet, chainId: ActiveChainId);
                case WalletProvider.EcosystemWallet:
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Google);
                    return new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                case WalletProvider.ReownWallet:
                    var externalWalletProvider = Application.platform == RuntimePlatform.WebGLPlayer && WebglForceMetamaskExtension ? WalletProvider.MetaMaskWallet : WalletProvider.ReownWallet;
                    return new WalletOptions(provider: externalWalletProvider, chainId: ActiveChainId);
                default:
                    throw new System.NotImplementedException("Wallet provider not implemented for this example.");
            }
        }

        private void InitializeEcosystemWalletPanel()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "EcosystemWallet_Authentication");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            // Email
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(() =>
            {
                InitializeEcosystemWalletPanel_Email();
            });

            // Phone
            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(() =>
            {
                InitializeEcosystemWalletPanel_Phone();
            });

            // Socials
            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(() =>
            {
                InitializeEcosystemWalletPanel_Socials();
            });
        }

        private void InitializeEcosystemWalletPanel_Email()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "EcosystemWallet_Email");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializeEcosystemWalletPanel);

            panel.InputFieldSubmitButton.onClick.RemoveAllListeners();
            panel.InputFieldSubmitButton.onClick.AddListener(() =>
            {
                try
                {
                    var email = panel.InputField.text;
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", email: email);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private void InitializeEcosystemWalletPanel_Phone()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "EcosystemWallet_Phone");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializeEcosystemWalletPanel);

            panel.InputFieldSubmitButton.onClick.RemoveAllListeners();
            panel.InputFieldSubmitButton.onClick.AddListener(() =>
            {
                try
                {
                    var phone = panel.InputField.text;
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", phoneNumber: phone);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private void InitializeEcosystemWalletPanel_Socials()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "EcosystemWallet_Socials");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializeEcosystemWalletPanel);

            // socials action 1 is google, 2 is apple 3 is discord

            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(() =>
            {
                try
                {
                    Log(panel.LogText, "Authenticating...");
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Google);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(() =>
            {
                try
                {
                    Log(panel.LogText, "Authenticating...");
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Apple);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(() =>
            {
                try
                {
                    Log(panel.LogText, "Authenticating...");
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Discord);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private void InitializeContractsPanel()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "Contracts");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            panel.NextButton.onClick.RemoveAllListeners();
            panel.NextButton.onClick.AddListener(InitializeAccountAbstractionPanel);

            // Get NFT
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var dropErc1155Contract = await ThirdwebManager.Instance.GetContract(address: "0x8F0a4dde7791fa9B6C62E0B099a1a3ff6dd1cF29", chainId: ActiveChainId);
                    var nft = await dropErc1155Contract.ERC1155_GetNFT(tokenId: 1);
                    Log(panel.LogText, $"NFT: {JsonConvert.SerializeObject(nft.Metadata)}");
                    var sprite = await nft.GetNFTSprite(client: ThirdwebManager.Instance.Client);
                    // spawn image for 3s
                    var image = new GameObject("NFT Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    image.transform.SetParent(panel.Panel.transform, false);
                    image.GetComponent<Image>().sprite = sprite;
                    Destroy(image, 3f);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Call contract
            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var contract = await ThirdwebManager.Instance.GetContract(address: "0x8F0a4dde7791fa9B6C62E0B099a1a3ff6dd1cF29", chainId: ActiveChainId);
                    var result = await contract.ERC1155_URI(tokenId: 1);
                    Log(panel.LogText, $"Result (uri): {result}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Get ERC20 Balance
            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var dropErc20Contract = await ThirdwebManager.Instance.GetContract(address: "0x28C1209fa6e7f1B258Ef65527C94129c6F82995f", chainId: ActiveChainId);
                    var symbol = await dropErc20Contract.ERC20_Symbol();
                    var balance = await dropErc20Contract.ERC20_BalanceOf(ownerAddress: await ThirdwebManager.Instance.GetActiveWallet().GetAddress());
                    var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 0, addCommas: false);
                    Log(panel.LogText, $"Balance: {balanceEth} {symbol}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private async void InitializeAccountAbstractionPanel()
        {
            var currentWallet = ThirdwebManager.Instance.GetActiveWallet();
            var smartWallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(personalWallet: currentWallet, chainId: ActiveChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));

            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "AccountAbstraction");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            // Personal Sign (1271)
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(async () =>
            {
                try
                {
                    var message = "Hello, World!";
                    var signature = await smartWallet.PersonalSign(message);
                    Log(panel.LogText, $"Signature: {signature}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Create Session Key
            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(async () =>
            {
                try
                {
                    Log(panel.LogText, "Granting Session Key...");
                    var randomWallet = await PrivateKeyWallet.Generate(ThirdwebManager.Instance.Client);
                    var randomWalletAddress = await randomWallet.GetAddress();
                    var timeTomorrow = Utils.GetUnixTimeStampNow() + 60 * 60 * 24;
                    var sessionKey = await smartWallet.CreateSessionKey(
                        signerAddress: randomWalletAddress,
                        approvedTargets: new List<string> { Constants.ADDRESS_ZERO },
                        nativeTokenLimitPerTransactionInWei: "0",
                        permissionStartTimestamp: "0",
                        permissionEndTimestamp: timeTomorrow.ToString(),
                        reqValidityStartTimestamp: "0",
                        reqValidityEndTimestamp: timeTomorrow.ToString()
                    );
                    Log(panel.LogText, $"Session Key Created for {randomWalletAddress}: {sessionKey.TransactionHash}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Get Active Signers
            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var activeSigners = await smartWallet.GetAllActiveSigners();
                    Log(panel.LogText, $"Active Signers: {JsonConvert.SerializeObject(activeSigners)}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private void CloseAllPanels()
        {
            ConnectWalletPanel.SetActive(false);
            foreach (var walletPanel in WalletPanels)
            {
                walletPanel.Panel.SetActive(false);
            }
        }

        private void ClearLog(TMP_Text logText)
        {
            logText.text = string.Empty;
        }

        private void Log(TMP_Text logText, string message)
        {
            logText.text = message;
            ThirdwebDebug.Log(message);
        }

        private void LoadingLog(TMP_Text logText)
        {
            logText.text = "Loading...";
        }
    }
}
