using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Numerics;

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
        private ulong ActiveChainId = 421614;

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
                        Symbol = "ETH"
                    }
                };
            }
        }

        private void InitializePanels()
        {
            CloseAllPanels();

            ConnectWalletPanel.SetActive(true);

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
                var options = GetWalletOptions(WalletProvider.WalletConnectWallet);
                ConnectWallet(options);
            });
        }

        private async void ConnectWallet(WalletOptions options)
        {
            // Connect the wallet

            var internalWalletProvider = options.Provider == WalletProvider.MetaMaskWallet ? WalletProvider.WalletConnectWallet : options.Provider;
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
                case WalletProvider.WalletConnectWallet:
                    var externalWalletProvider =
                        Application.platform == RuntimePlatform.WebGLPlayer && WebglForceMetamaskExtension ? WalletProvider.MetaMaskWallet : WalletProvider.WalletConnectWallet;
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
            panel.NextButton.onClick.AddListener(InitializeCustomContractsPanel);

            // Get NFT
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var dropErc1155Contract = await ThirdwebManager.Instance.GetContract(address: "0x94894F65d93eb124839C667Fc04F97723e5C4544", chainId: ActiveChainId);
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
                    var contract = await ThirdwebManager.Instance.GetContract(address: "0x6A7a26c9a595E6893C255C9dF0b593e77518e0c3", chainId: ActiveChainId);
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
                    var dropErc20Contract = await ThirdwebManager.Instance.GetContract(address: "0xEBB8a39D865465F289fa349A67B3391d8f910da9", chainId: ActiveChainId);
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

           private void InitializeCustomContractsPanel()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "CustomContracts");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            panel.NextButton.onClick.RemoveAllListeners();
            panel.NextButton.onClick.AddListener(InitializeAccountAbstractionPanel);

            //--CUSTOM READ CONTRACT BUTTON--//
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(async () =>
            {
              try
                {

                // Set your smart contract address "0x..."
                    string CustomContractAddress = ""; 
                // Set your smart contract method "myMethod"
                    string smartContractMethod = ""; 
                // Set parameter amount, value (in this case we are using string for an address type value). 
                // Check Learn About parameters in playground scene for more information.
                    string parameter1 = ""; 
                //  string parameter2 = ""; (optional)
                //  string parameter3 = ""; (optional)

                //Check if smartcontract is set.
                    if (string.IsNullOrEmpty(CustomContractAddress))
                    {
                        Log(panel.LogText, $"Error: You need to set 'CustomContractAddress' in PlaygroundManager.cs on line 423");
                        return;            
                    }        
                //Check if smartcontract is set. 
                    if (string.IsNullOrEmpty(smartContractMethod))
                    {
                        Log(panel.LogText, $"Error: You need to set the 'smartContractMethod' that you want to READ in PlaygroundManager.cs on line 425");
                        return; 
                    }       
                //Check if parameter1 is set. 
                    if (string.IsNullOrEmpty(parameter1))
                    {
                        Log(panel.LogText, $"Error: You need to set 'parameter1' in PlaygroundManager.cs on line 427.");
                        return; 
                    }

                // Initialize the contract
                    var contract = await ThirdwebManager.Instance.GetContract(address: CustomContractAddress, chainId: ActiveChainId);
                
                    Log(panel.LogText, $"Reading {smartContractMethod} Method for the contract address: {CustomContractAddress}");

                // Call the read method with desired type value 
                    string result = await contract.Read<string>(
                        smartContractMethod, // Your smartcontract Method
                        parameter1           // Optional depending on the method. Add or remove parameters if required.
                    // parameter2,
                    // parameter3,
                    // parameter4
                    );

                    Log(panel.LogText, $"Success! You made your first Read Method! Result: {result}");

                }
                catch (System.Exception ex)
                {
                                    Debug.LogError($"[PlaygroundManager] Error:{ex.Message}");
                    Log(panel.LogText, $"Error:\n"+
                                    $"Check that your smartcontract address is correct and the chain is correct in inspector\n"+
                                    $"Check that read Method name is correct\n"+
                                    $"Check the required amount of parameters and it's value type is correct"        
                        );
                }
            });


            //--CUSTOM WRITE CONTRACT BUTTON--//
            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(async () =>
            {
               try
                {
                // Set your smart contract address "0x..."
                    string CustomContractAddress = ""; 
                // Set your smart contract method "myMethod"
                    string smartContractMethod = ""; 
                // Set parameter amount, value type and it's value 
                    string parameter1 = ""; 
                //  string parameter2 = ""; (optional)
                //  string parameter3 = ""; (optional)

                //Check if smartcontract is set.
                    if (string.IsNullOrEmpty(CustomContractAddress))
                    {
                        Log(panel.LogText, $"Error: You need to set 'CustomContractAddress' in PlaygroundManager.cs on line 485");
                        return;            
                    }        
                //Check if smartcontract is set. 
                    if (string.IsNullOrEmpty(smartContractMethod))
                    {
                        Log(panel.LogText, $"Error: You need to set the 'smartContractMethod' that you want to WRITE in PlaygroundManager.cs on line 487");
                        return; 
                    }       
                //Check if parameter1 is set. 
                    if (string.IsNullOrEmpty(parameter1))
                    {
                        Log(panel.LogText, $"Error: You need to set 'parameter1' in PlaygroundManager.cs on line 489.");
                        return; 
                    }

                //Initialize contract, wallet and Weivalue
                    var contract = await ThirdwebManager.Instance.GetContract(address: CustomContractAddress, chainId: ActiveChainId);
                    var activeWallet = ThirdwebManager.Instance.GetActiveWallet();
                    BigInteger weiValue = BigInteger.Parse("0"); // 0 ETH. add the required eth value in WEI
                
                
                    Log(panel.LogText, $"Writing {smartContractMethod} Method for the contract address:{CustomContractAddress} ");
                
                //Write Method call
                    var receipt = await ThirdwebContract.Write(
                        activeWallet,       //Required Parameter in all write contract calls.
                        contract,           //Required Parameter in all write contract calls.
                        smartContractMethod,//Required Parameter in all write contract calls.
                        weiValue,           //Required Parameter in all write contract calls.
                        parameter1          //First Read Parameter 1. Add or remove depending on the method parameter amount
                    // parameter2,                 
                    // parameter3,
                    // parameter4  
                    );

                    Log(panel.LogText, $"[PlaygroundManager] Success! You made your first Write Method from unity!\n"+
                                       $"TX hash: {receipt.TransactionHash}"
                        );
                                
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlaygroundManager] Error in Write custom smartcontract: {ex.Message}");
                    Log(panel.LogText, $"Error:\n"+
                        $"Check that your smartcontract address is correct and the chain is correct in inspector\n"+
                        $"Check that read Method name is correct\n"+
                        $"Check the required amount of parameters and it's value type is correct"
                        );   
                }
            });

            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(() =>
            {
                InitializeLearnParameters();
            });
        }



        private void InitializeLearnParameters()
        {
            
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "LearnParameters");

            CloseAllPanels();

            
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializeCustomContractsPanel);

            // Personal Sign (1271)
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
            panel.BackButton.onClick.AddListener(InitializeCustomContractsPanel);

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
