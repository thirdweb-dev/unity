using System.Collections.Generic;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using UnityEngine;

namespace Thirdweb
{
    /// <summary>
    /// The entry point for the thirdweb SDK.
    /// </summary>
    public class ThirdwebSDK
    {
        /// <summary>
        /// Options for the thirdweb SDK.
        /// </summary>
        [System.Serializable]
        public struct Options
        {
            public GaslessOptions? gasless;
            public StorageOptions? storage;
            public WalletOptions? wallet;
        }

        /// <summary>
        /// Wallet configuration options.
        /// </summary>
        [System.Serializable]
        public struct WalletOptions
        {
            public string appName; // the app name that will show in different wallet providers
            public Dictionary<string, object> extras; // extra data to pass to the wallet provider
        }

        /// <summary>
        /// Storage configuration options.
        /// </summary>
        [System.Serializable]
        public struct StorageOptions
        {
            public string ipfsGatewayUrl; // override the default ipfs gateway, should end in /ipfs/
        }

        /// <summary>
        /// Gasless configuration options.
        /// </summary>
        [System.Serializable]
        public struct GaslessOptions
        {
            public OZDefenderOptions? openzeppelin;
            public BiconomyOptions? biconomy;
            public bool experimentalChainlessSupport;
        }

        /// <summary>
        /// OpenZeppelin Defender Gasless configuration options.
        /// </summary>
        [System.Serializable]
        public struct OZDefenderOptions
        {
            public string relayerUrl;
            public string relayerForwarderAddress;
        }

        /// <summary>
        /// Biconomy Gasless configuration options.
        /// </summary>
        [System.Serializable]
        public struct BiconomyOptions
        {
            public string apiId;
            public string apiKey;
        }

        private string chainOrRPC;

        /// <summary>
        /// Connect and Interact with a user's wallet
        /// </summary>
        public Wallet wallet;

        /// <summary>
        /// Deploy new contracts
        /// </summary>
        public Deployer deployer;

        public Storage storage;

        [System.Serializable]
        public class NativeSession
        {
            public int lastChainId = -1;
            public string lastRPC = null;
            public Account account = null;
            public Web3 web3 = null;
        }

        public NativeSession nativeSession;

        /// <summary>
        /// Create an instance of the thirdweb SDK. Requires a webGL browser context.
        /// </summary>
        /// <param name="chainOrRPC">The chain name or RPC url to connect to</param>
        /// <param name="options">Configuration options</param>
        public ThirdwebSDK(string chainOrRPC, int chainId = -1, Options options = new Options())
        {
            this.chainOrRPC = chainOrRPC;
            this.wallet = new Wallet();
            this.deployer = new Deployer();
            this.storage = new Storage(options.storage);

            if (!Utils.IsWebGLBuild())
            {
                if (!chainOrRPC.StartsWith("https://"))
                    throw new UnityException("Invalid RPC URL!");
                if (chainId == -1)
                    throw new UnityException("Chain ID override required for native platforms!");

                nativeSession = new NativeSession();
                nativeSession.lastRPC = chainOrRPC;
                nativeSession.lastChainId = chainId;
                nativeSession.web3 = new Web3(nativeSession.lastRPC);
            }
            else
            {
                Bridge.Initialize(chainOrRPC, options);
            }
        }

        /// <summary>
        /// Get an instance of a deployed contract.
        /// </summary>
        /// <param name="address">The contract address</param>
        /// <param name="abi">Optionally pass the ABI for contracts that cannot be auto resolved. Expected format for the ABI is escaped JSON string</param>
        /// <returns>A contract instance</returns>
        public Contract GetContract(string address, string abi = null)
        {
            return new Contract(this.chainOrRPC, address, abi);
        }
    }
}
