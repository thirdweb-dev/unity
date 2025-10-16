using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Thirdweb.Unity.Examples
{
    public enum OAuthProvider
    {
        Google,
        Apple,
        Facebook,
        Discord,
        Twitch,
        Github,
        Coinbase,
        X,
        TikTok,
        Line,
        Steam,
    }

    /// <summary>
    /// A simple manager to demonstrate core functionality of the thirdweb SDK.
    /// This is not production-ready code. Do not use this in production.
    /// </summary>
    public class PlaygroundManager : MonoBehaviour
    {
        #region Inspector

        public bool AlwaysUpgradeToSmartWallet;
        public ulong ChainId;
        public string Email;
        public string Phone;
        public OAuthProvider Social = OAuthProvider.Google;
        public Transform ActionButtonParent;
        public GameObject ActionButtonPrefab;
        public GameObject LogPanel;

        #endregion

        #region Initialization

        private Dictionary<string, UnityAction> _actionMappings;

        private void Awake()
        {
            this.LogPanel.SetActive(false);
            this._actionMappings = new Dictionary<string, UnityAction>
            {
                // Wallet Connection
                { "Guest Wallet (Smart)", this.Wallet_Guest },
                { "Social Wallet", this.Wallet_Social },
                { "Email Wallet", this.Wallet_Email },
                { "Phone Wallet", this.Wallet_Phone },
                { "External Wallet", this.Wallet_External },
                // Wallet Actions
                { "Sign Message", this.WalletAction_SignMessage },
                { "Sign SIWE", this.WalletAction_SIWE },
                { "Sign Typed Data", this.WalletAction_SignTypedData },
                { "Get Balance", this.WalletAction_GetBalance },
                { "Send Assets", this.WalletAction_Transfer },
                // Contract Interaction
                { "Read Contract (Ext)", this.Contract_Read },
                { "Write Contract (Ext)", this.Contract_Write },
                { "Read Contract (Raw)", this.Contract_ReadCustom },
                { "Write Contract (Raw)", this.Contract_WriteCustom },
                { "Prepare Tx (Low Lvl)", this.Contract_PrepareTransaction },
            };

            foreach (Transform child in this.ActionButtonParent)
            {
                Destroy(child.gameObject);
            }

            foreach (var action in this._actionMappings)
            {
                var button = Instantiate(this.ActionButtonPrefab, this.ActionButtonParent);
                button.GetComponentInChildren<TMP_Text>().text = action.Key;
                button.GetComponent<Button>().onClick.AddListener(action.Value);
            }
        }

        private void LogPlayground(string message)
        {
            this.LogPanel.GetComponentInChildren<TMP_Text>().text = message;
            ThirdwebDebug.Log(message);
            this.LogPanel.SetActive(true);
        }

        private bool WalletConnected()
        {
            var isConnected = ThirdwebManager.Instance.ActiveWallet != null;
            if (!isConnected)
            {
                this.LogPlayground("Please authenticate to connect a wallet first.");
            }
            return isConnected;
        }

        #endregion

        #region Wallet Connection

        private async void Wallet_Guest()
        {
            var walletOptions = new WalletOptions(provider: WalletProvider.InAppWallet, chainId: this.ChainId, new InAppWalletOptions(authprovider: AuthProvider.Guest));
            var wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);

            var address = await wallet.GetAddress();
            ThirdwebDebug.Log($"[Guest] Smart wallet admin signer address:\n{address}");

            // --For the guest wallet to showcase ERC4337 we're gonna upgrade it to a smart wallet
            var smartWallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(
                ThirdwebManager.Instance.ActiveWallet,
                chainId: this.ChainId,
                smartWalletOptions: new SmartWalletOptions(
                    // sponsor gas for users, improve onboarding
                    sponsorGas: true,
                    // optional
                    factoryAddress: Constants.DEFAULT_FACTORY_ADDRESS_V07,
                    // optional
                    entryPoint: Constants.ENTRYPOINT_ADDRESS_V07
                )
            );

            var smartWalletAddress = await smartWallet.GetAddress();
            this.LogPlayground($"[Guest] Connected to wallet (sponsored gas):\n{smartWalletAddress}");

            // // --Smart wallets have special functionality other than just gas sponsorship
            // var sessionKeyReceipt = await smartWallet.CreateSessionKey(
            //     signerAddress: await Utils.GetAddressFromENS(ThirdwebManager.Instance.Client, "vitalik.eth"),
            //     approvedTargets: new List<string> { Constants.ADDRESS_ZERO },
            //     nativeTokenLimitPerTransactionInWei: "0",
            //     permissionStartTimestamp: "0",
            //     permissionEndTimestamp: (Utils.GetUnixTimeStampNow() + (60 * 60)).ToString(), // 1 hour from now
            //     reqValidityStartTimestamp: "0",
            //     reqValidityEndTimestamp: (Utils.GetUnixTimeStampNow() + (60 * 60)).ToString() // 1 hour from now
            // );

            // this.LogPlayground($"Session Key Creation Receipt:\n{sessionKeyReceipt}");
        }

        private async void Wallet_Social()
        {
            if (!System.Enum.TryParse<AuthProvider>(this.Social.ToString(), out var parsedOAuthProvider))
            {
                parsedOAuthProvider = AuthProvider.Google;
            }
            var walletOptions = new WalletOptions(provider: WalletProvider.InAppWallet, chainId: this.ChainId, new InAppWalletOptions(authprovider: parsedOAuthProvider));
            var wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);
            if (this.AlwaysUpgradeToSmartWallet)
            {
                wallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(wallet, chainId: this.ChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));
            }
            var address = await wallet.GetAddress();
            this.LogPlayground($"[Social] Connected to wallet:\n{address}");
        }

        private async void Wallet_Email()
        {
            if (string.IsNullOrEmpty(this.Email))
            {
                this.LogPlayground("Please enter a valid email address in the scene's PlaygroundManager.");
                return;
            }

            var walletOptions = new WalletOptions(provider: WalletProvider.InAppWallet, chainId: this.ChainId, new InAppWalletOptions(email: this.Email));
            var wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);
            if (this.AlwaysUpgradeToSmartWallet)
            {
                wallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(wallet, chainId: this.ChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));
            }
            var address = await wallet.GetAddress();
            this.LogPlayground($"[Email] Connected to wallet:\n{address}");
        }

        private async void Wallet_Phone()
        {
            if (string.IsNullOrEmpty(this.Phone))
            {
                this.LogPlayground("Please enter a valid phone number in the scene's PlaygroundManager.");
                return;
            }

            var walletOptions = new WalletOptions(provider: WalletProvider.InAppWallet, chainId: this.ChainId, new InAppWalletOptions(phoneNumber: this.Phone));
            var wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);
            if (this.AlwaysUpgradeToSmartWallet)
            {
                wallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(wallet, chainId: this.ChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));
            }
            var address = await wallet.GetAddress();
            this.LogPlayground($"[Phone] Connected to wallet:\n{address}");
        }

        private async void Wallet_External()
        {
            var walletOptions = new WalletOptions(
                provider: WalletProvider.ReownWallet,
                chainId: this.ChainId,
                reownOptions: new ReownOptions(
                    projectId: null,
                    name: null,
                    description: null,
                    url: null,
                    iconUrl: null,
                    includedWalletIds: null,
                    excludedWalletIds: null,
                    featuredWalletIds: new string[]
                    {
                        "c57ca95b47569778a828d19178114f4db188b89b763c899ba0be274e97267d96",
                        "18388be9ac2d02726dbac9777c96efaac06d744b2f6d580fccdd4127a6d01fd1",
                        "541d5dcd4ede02f3afaf75bf8e3e4c4f1fb09edb5fa6c4377ebf31c2785d9adf",
                    }
                )
            );
            var wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);
            if (this.AlwaysUpgradeToSmartWallet)
            {
                wallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(wallet, chainId: this.ChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));
            }
            var address = await wallet.GetAddress();
            this.LogPlayground($"[SIWE] Connected to wallet:\n{address}");
        }

#pragma warning disable IDE0051 // Remove unused private members: This is a showcase of an alternative way to use Reown
        private async void Wallet_External_Direct()
#pragma warning restore IDE0051 // Remove unused private members: This is a showcase of an alternative way to use Reown
        {
            var walletOptions = new WalletOptions(
                provider: WalletProvider.ReownWallet,
                chainId: this.ChainId,
                reownOptions: new ReownOptions(singleWalletId: "c57ca95b47569778a828d19178114f4db188b89b763c899ba0be274e97267d96")
            );
            var wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);
            if (this.AlwaysUpgradeToSmartWallet)
            {
                wallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(wallet, chainId: this.ChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));
            }
            var address = await wallet.GetAddress();
            this.LogPlayground($"[SIWE] Connected to wallet:\n{address}");
        }

        #endregion

        #region Wallet Actions

        private async void WalletAction_SignMessage()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            var message = "Hello from thirdweb!";
            var sig = await ThirdwebManager.Instance.ActiveWallet.PersonalSign(message);
            this.LogPlayground($"Message:\n{message}\n\nSignature:\n{sig}");
        }

        private async void WalletAction_SIWE()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            var payload = Utils.GenerateSIWE(
                new LoginPayloadData()
                {
                    Domain = "thirdweb.com",
                    Address = await ThirdwebManager.Instance.ActiveWallet.GetAddress(),
                    Statement = "Sign in with thirdweb to the Unity SDK playground.",
                    Version = "1",
                    ChainId = this.ChainId.ToString(),
                    Nonce = System.Guid.NewGuid().ToString(),
                    IssuedAt = System.DateTime.UtcNow.ToString("o"),
                }
            );
            var sig = await ThirdwebManager.Instance.ActiveWallet.PersonalSign(payload);
            this.LogPlayground($"SIWE Payload:\n{payload}\n\nSignature:\n{sig}");
        }

        private async void WalletAction_SignTypedData()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            var json =
                "{\"types\":{\"EIP712Domain\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"version\",\"type\":\"string\"},{\"name\":\"chainId\",\"type\":\"uint256\"},{\"name\":\"verifyingContract\",\"type\":\"address\"}],\"Person\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"wallet\",\"type\":\"address\"}],\"Mail\":[{\"name\":\"from\",\"type\":\"Person\"},{\"name\":\"to\",\"type\":\"Person\"},{\"name\":\"contents\",\"type\":\"string\"}]},\"primaryType\":\"Mail\",\"domain\":{\"name\":\"Ether Mail\",\"version\":\"1\",\"chainId\":1,\"verifyingContract\":\"0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC\"},\"message\":{\"from\":{\"name\":\"Cow\",\"wallet\":\"0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826\"},\"to\":{\"name\":\"Bob\",\"wallet\":\"0xbBbBBBBbbBBBbbbBbbBbbBBbBbbBbBbBbBbbBBbB\"},\"contents\":\"Hello, Bob!\"}}";
            var sig = await ThirdwebManager.Instance.ActiveWallet.SignTypedDataV4(json);
            this.LogPlayground($"Typed Data:\n{json}\n\nSignature:\n{sig}");
        }

        private async void WalletAction_GetBalance()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            var balance = await ThirdwebManager.Instance.ActiveWallet.GetBalance(this.ChainId);
            var chainMeta = await Utils.GetChainMetadata(ThirdwebManager.Instance.Client, this.ChainId);
            this.LogPlayground($"Wallet Balance:\n{Utils.ToEth(balance.ToString(), 6, true)} {chainMeta.NativeCurrency.Symbol}");
        }

        private async void WalletAction_Transfer()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            // ---Transfer native tokens
            var receipt = await ThirdwebManager.Instance.ActiveWallet.Transfer(chainId: this.ChainId, toAddress: await ThirdwebManager.Instance.ActiveWallet.GetAddress(), weiAmount: 0);

            // ---Transfer ERC20 tokens
            // var receipt = await ThirdwebManager.Instance.ActiveWallet.Transfer(chainId: this.ChainId, toAddress: await ThirdwebManager.Instance.ActiveWallet.GetAddress(), weiAmount: 0, tokenAddress: "0xERC20Addy");

            this.LogPlayground($"Transfer Receipt:\n{receipt}");
        }

        #endregion

        #region Contract Interaction

        private async void Contract_Read()
        {
            var usdcContractAddressArbitrum = "0xaf88d065e77c8cc2239327c5edb3a432268e5831";
            var contract = await ThirdwebManager.Instance.GetContract(address: usdcContractAddressArbitrum, chainId: 42161);
            var result = await contract.ERC20_BalanceOf(ownerAddress: await Utils.GetAddressFromENS(ThirdwebManager.Instance.Client, "vitalik.eth"));
            var tokenSymbol = await contract.ERC20_Symbol();
            var resultFormatted = Utils.ToEth(result.ToString(), 6, true) + " " + tokenSymbol;
            this.LogPlayground($"ThirdwebContract.ERC20_BalanceOf Result:\n{resultFormatted}");
        }

        private async void Contract_Write()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            var contract = await ThirdwebManager.Instance.GetContract(address: "0x3EE304A2cBf24F73510C6C590cFcd10bEd0E6F70", chainId: this.ChainId);
            var transactionReceipt = await contract.ERC20_Approve(
                wallet: ThirdwebManager.Instance.ActiveWallet,
                spenderAddress: await Utils.GetAddressFromENS(ThirdwebManager.Instance.Client, "vitalik.eth"),
                amount: 0
            );
            this.LogPlayground($"ThirdwebContract.ERC20_Approve Receipt:\n{transactionReceipt}");
        }

        private async void Contract_ReadCustom()
        {
            var contract = await ThirdwebManager.Instance.GetContract(address: "0xBD0334AC7FADA28CcD27Fa09838e9EA4c39117Db", chainId: this.ChainId);
            var method = "function getDeposit() view returns (uint256)";
            var result = await contract.Read<BigInteger>(method);
            this.LogPlayground($"ThirdwebContract.Read<T>\n({method}\nResult:\n{result}");
        }

        private async void Contract_WriteCustom()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            var contract = await ThirdwebManager.Instance.GetContract(address: "0xBD0334AC7FADA28CcD27Fa09838e9EA4c39117Db", chainId: this.ChainId);
            var method = "function transferOwnership(address newOwner) payable";
            // just in case someday this actually goes through for some unknown reason, we can count on vitalik
            var newOwner = await Utils.GetAddressFromENS(ThirdwebManager.Instance.Client, "vitalik.eth");
            var transactionReceipt = await contract.Write(wallet: ThirdwebManager.Instance.ActiveWallet, method: method, weiValue: 0, parameters: new object[] { newOwner });
            this.LogPlayground($"ThirdwebContract.Write\n({method}\nReceipt:\n{transactionReceipt}");
        }

        private async void Contract_PrepareTransaction()
        {
            if (!this.WalletConnected())
            {
                return;
            }

            // ---You can prepare a transaction instead of directly calling Thirdweb.Contract.Write
            // var contract = await ThirdwebManager.Instance.GetContract(address: "0xBD0334AC7FADA28CcD27Fa09838e9EA4c39117Db", chainId: this.ChainId);
            // var method = "function transferOwnership(address newOwner) payable";
            // var newOwner = await Utils.GetAddressFromENS(ThirdwebManager.Instance.Client, "vitalik.eth");
            // var transaction = await contract.Prepare(wallet: ThirdwebManager.Instance.ActiveWallet, method: method, weiValue: 0, parameters: new object[] { newOwner });

            // ---Or you can create a transaction from scratch
            var transaction = await ThirdwebTransaction.Create(
                ThirdwebManager.Instance.ActiveWallet,
                new ThirdwebTransactionInput(this.ChainId, to: await ThirdwebManager.Instance.ActiveWallet.GetAddress(), value: 0, data: "0x")
            );
            var costEstimates = await ThirdwebTransaction.EstimateTotalCosts(transaction);

            // ---If you wanted to send it
            // `var hash = transaction.Send();` or `var receipt = await transaction.SendAndWaitForTransactionReceipt();`

            // ---We're just gonna log it here
            this.LogPlayground($"ThirdwebContract.Prepare\nEstimated Cost:\n{costEstimates.Ether}\n\nTransaction:\n{JsonConvert.SerializeObject(transaction, Formatting.Indented)}");
        }

        #endregion
    }
}
