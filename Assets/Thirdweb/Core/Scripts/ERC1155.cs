using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Thirdweb.Contracts.TokenERC1155;
using Thirdweb.Contracts.DropERC1155;

namespace Thirdweb
{
    /// <summary>
    /// Interact with any ERC1155 compatible contract.
    /// </summary>
    public class ERC1155 : Routable
    {
        /// <summary>
        /// Handle signature minting functionality
        /// /// </summary>
        public ERC1155Signature signature;

        /// <summary>
        /// Query claim conditions
        /// </summary>
        public ERC1155ClaimConditions claimConditions;

        TokenERC1155Service tokenERC1155Service;
        DropERC1155Service dropERC1155Service;

        /// <summary>
        /// Interact with any ERC1155 compatible contract.
        /// </summary>
        public ERC1155(string parentRoute, string address)
            : base(Routable.append(parentRoute, "erc1155"))
        {
            if (!Utils.IsWebGLBuild())
            {
                this.tokenERC1155Service = new TokenERC1155Service(ThirdwebManager.Instance.SDK.web3, address);
                this.dropERC1155Service = new DropERC1155Service(ThirdwebManager.Instance.SDK.web3, address);
            }

            this.signature = new ERC1155Signature(baseRoute);
            this.claimConditions = new ERC1155ClaimConditions(baseRoute);
        }

        // READ FUNCTIONS

        /// <summary>
        /// Get a NFT in this contract by its ID
        /// </summary>
        public async Task<NFT> Get(string tokenId)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<NFT>(getRoute("get"), Utils.ToJsonStringArray(tokenId));
            }
            else
            {
                NFT nft = new NFT();
                nft.owner = "";
                nft.type = "ERC1155";
                nft.supply = await TotalSupply(tokenId);
                nft.quantityOwned = 404;
                string tokenURI = await tokenERC1155Service.UriQueryAsync(BigInteger.Parse(tokenId));
                nft.metadata = await tokenURI.DownloadText<NFTMetadata>();
                nft.metadata.image = nft.metadata.image.ReplaceIPFS();
                nft.metadata.id = tokenId;
                nft.metadata.uri = tokenURI.ReplaceIPFS();
                return nft;
            }
        }

        /// <summary>
        /// Get a all NFTs in this contract
        /// </summary>
        public async Task<List<NFT>> GetAll(QueryAllParams queryParams = null)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<List<NFT>>(getRoute("getAll"), Utils.ToJsonStringArray(queryParams));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get a all NFTs owned by the connected wallet
        /// </summary>
        /// <param name="address">Optional wallet address to query NFTs of</param>
        public async Task<List<NFT>> GetOwned(string address = null)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<List<NFT>>(getRoute("getOwned"), Utils.ToJsonStringArray(address));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get the balance of the given NFT for the connected wallet
        /// </summary>
        public async Task<string> Balance(string tokenId)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<string>(getRoute("balance"), new string[] { });
            }
            else
            {
                return await BalanceOf(await ThirdwebManager.Instance.SDK.wallet.GetAddress(), tokenId);
            }
        }

        /// <summary>
        /// Get the balance of the given NFT for the given wallet address
        /// </summary>
        public async Task<string> BalanceOf(string address, string tokenId)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<string>(getRoute("balanceOf"), Utils.ToJsonStringArray(address, tokenId));
            }
            else
            {
                return (await tokenERC1155Service.BalanceOfQueryAsync(address, BigInteger.Parse(tokenId))).ToString();
            }
        }

        /// <summary>
        /// Check whether the given contract address has been approved to transfer NFTs on behalf of the given wallet address
        /// </summary>
        /// <param name="address">The wallet address</param>
        /// <param name="contractAddress">The contract address to check approval for</param>
        public async Task<string> IsApprovedForAll(string address, string approvedContract)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<string>(getRoute("isApproved"), Utils.ToJsonStringArray(address, approvedContract));
            }
            else
            {
                return (await tokenERC1155Service.IsApprovedForAllQueryAsync(address, approvedContract)).ToString();
            }
        }

        public async Task<int> TotalCount()
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<int>(getRoute("totalCount"), new string[] { });
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get the total suppply in circulation for thge given NFT
        /// </summary>
        public async Task<int> TotalSupply(string tokenId)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<int>(getRoute("totalSupply"), Utils.ToJsonStringArray(tokenId));
            }
            else
            {
                return (int)(await tokenERC1155Service.TotalSupplyQueryAsync(BigInteger.Parse(tokenId)));
            }
        }

        // WRITE FUNCTIONS

        /// <summary>
        /// Set approval to the given contract to transfer NFTs on behalf of the connected wallet
        /// </summary>
        public async Task<TransactionResult> SetApprovalForAll(string contractToApprove, bool approved)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(
                    getRoute("setApprovalForAll"),
                    Utils.ToJsonStringArray(contractToApprove, approved)
                );
            }
            else
            {
                var receipt = await tokenERC1155Service.SetApprovalForAllRequestAndWaitForReceiptAsync(contractToApprove, approved);
                return receipt.ToTransactionResult();
            }
        }

        /// <summary>
        /// Transfer NFTs to the given address
        /// </summary>
        public async Task<TransactionResult> Transfer(string to, string tokenId, int amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("transfer"), Utils.ToJsonStringArray(to, tokenId, amount));
            }
            else
            {
                var receipt = await tokenERC1155Service.SafeTransferFromRequestAndWaitForReceiptAsync(
                    await ThirdwebManager.Instance.SDK.wallet.GetAddress(),
                    to,
                    BigInteger.Parse(tokenId),
                    amount,
                    new byte[0]
                );
                return receipt.ToTransactionResult();
            }
        }

        /// <summary>
        /// Burn NFTs
        /// </summary>
        public async Task<TransactionResult> Burn(string tokenId, int amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("burn"), Utils.ToJsonStringArray(tokenId, amount));
            }
            else
            {
                var receipt = await tokenERC1155Service.BurnRequestAndWaitForReceiptAsync(
                    await ThirdwebManager.Instance.SDK.wallet.GetAddress(),
                    BigInteger.Parse(tokenId),
                    amount
                );
                return receipt.ToTransactionResult();
            }
        }

        /// <summary>
        /// Claim NFTs from a Drop contract
        /// </summary>
        public async Task<TransactionResult> Claim(string tokenId, int amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("claim"), Utils.ToJsonStringArray(tokenId, amount));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Claim NFTs from a Drop contract and send them to the given address
        /// </summary>
        public async Task<TransactionResult> ClaimTo(string address, string tokenId, int amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("claimTo"), Utils.ToJsonStringArray(address, tokenId, amount));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Mint an NFT (requires minting permission)
        /// </summary>
        public async Task<TransactionResult> Mint(NFTMetadataWithSupply nft)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("mint"), Utils.ToJsonStringArray(nft));
            }
            else
            {
                return await MintTo(await ThirdwebManager.Instance.SDK.wallet.GetAddress(), nft);
            }
        }

        /// <summary>
        /// Mint an NFT and send it to the given wallet (requires minting permission)
        /// </summary>
        public async Task<TransactionResult> MintTo(string address, NFTMetadataWithSupply nft)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("mintTo"), Utils.ToJsonStringArray(address, nft));
            }
            else
            {
                var receipt = await tokenERC1155Service.MintToRequestAndWaitForReceiptAsync(
                    await ThirdwebManager.Instance.SDK.wallet.GetAddress(),
                    BigInteger.Parse(nft.metadata.id),
                    nft.metadata.uri,
                    1
                );
                return receipt.ToTransactionResult();
            }
        }

        /// <summary>
        /// Mint additional supply of a given NFT (requires minting permission)
        /// </summary>
        public async Task<TransactionResult> MintAdditionalSupply(string tokenId, int additionalSupply)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(
                    getRoute("mintAdditionalSupply"),
                    Utils.ToJsonStringArray(tokenId, additionalSupply, additionalSupply)
                );
            }
            else
            {
                return await MintAdditionalSupplyTo(await ThirdwebManager.Instance.SDK.wallet.GetAddress(), tokenId, additionalSupply);
            }
        }

        /// <summary>
        /// Mint additional supply of a given NFT and send it to the given wallet (requires minting permission)
        /// </summary>
        public async Task<TransactionResult> MintAdditionalSupplyTo(string address, string tokenId, int additionalSupply)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(
                    getRoute("mintAdditionalSupplyTo"),
                    Utils.ToJsonStringArray(address, tokenId, additionalSupply)
                );
            }
            else
            {
                var uri = await tokenERC1155Service.UriQueryAsync(BigInteger.Parse(tokenId));
                var receipt = await tokenERC1155Service.MintToRequestAndWaitForReceiptAsync(
                    await ThirdwebManager.Instance.SDK.wallet.GetAddress(),
                    BigInteger.Parse(tokenId),
                    uri,
                    additionalSupply
                );
                return receipt.ToTransactionResult();
            }
        }
    }

    /// <summary>
    /// Fetch claim conditions for a given ERC1155 drop contract
    /// </summary>
    public class ERC1155ClaimConditions : Routable
    {
        public ERC1155ClaimConditions(string parentRoute)
            : base(Routable.append(parentRoute, "claimConditions")) { }

        /// <summary>
        /// Get the active claim condition
        /// </summary>
        public async Task<ClaimConditions> GetActive(string tokenId)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<ClaimConditions>(getRoute("getActive"), Utils.ToJsonStringArray(tokenId));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Check whether the connected wallet is eligible to claim
        /// </summary>
        public async Task<bool> CanClaim(string tokenId, int quantity, string addressToCheck = null)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<bool>(getRoute("canClaim"), Utils.ToJsonStringArray(tokenId, quantity, addressToCheck));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get the reasons why the connected wallet is not eligible to claim
        /// </summary>
        public async Task<string[]> GetIneligibilityReasons(string tokenId, int quantity, string addressToCheck = null)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<string[]>(
                    getRoute("getClaimIneligibilityReasons"),
                    Utils.ToJsonStringArray(tokenId, quantity, addressToCheck)
                );
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get the special values set in the allowlist for the given wallet
        /// </summary>
        public async Task<bool> GetClaimerProofs(string tokenId, string claimerAddress)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<bool>(getRoute("getClaimerProofs"), Utils.ToJsonStringArray(claimerAddress));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }
    }

    // TODO switch to another JSON serializer that supports polymorphism
    [System.Serializable]
#nullable enable
    public class ERC1155MintPayload
    {
        public string to;
        public string price;
        public string currencyAddress;
        public string primarySaleRecipient;
        public string royaltyRecipient;
        public int royaltyBps;
        public int quantity;
        public NFTMetadata metadata;
        public string uid;

        // TODO implement these, needs JS bridging support
        // public long mintStartTime;
        // public long mintEndTime;

        public ERC1155MintPayload(string receiverAddress, NFTMetadata metadata)
        {
            this.metadata = metadata;
            this.to = receiverAddress;
            this.price = "0";
            this.currencyAddress = Utils.AddressZero;
            this.primarySaleRecipient = Utils.AddressZero;
            this.royaltyRecipient = Utils.AddressZero;
            this.royaltyBps = 0;
            this.quantity = 1;
            this.uid = Utils.ToBytes32HexString(Guid.NewGuid().ToByteArray());
            // TODO temporary solution
            // this.mintStartTime = Utils.UnixTimeNowMs() * 1000L;
            // this.mintEndTime = this.mintStartTime + 1000L * 60L * 60L * 24L * 365L;
        }
    }

    [System.Serializable]
    public class ERC1155MintAdditionalPayload
    {
        public string tokenId;
        public string to;
        public string price;
        public string currencyAddress;
        public string primarySaleRecipient;
        public string royaltyRecipient;
        public int royaltyBps;
        public int quantity;
        public string uid;

        // TODO implement these, needs JS bridging support
        // public long mintStartTime;
        // public long mintEndTime;

        public ERC1155MintAdditionalPayload(string receiverAddress, string tokenId)
        {
            this.tokenId = tokenId;
            this.to = receiverAddress;
            this.price = "0";
            this.currencyAddress = Utils.AddressZero;
            this.primarySaleRecipient = Utils.AddressZero;
            this.royaltyRecipient = Utils.AddressZero;
            this.royaltyBps = 0;
            this.quantity = 1;
            this.uid = Utils.ToBytes32HexString(Guid.NewGuid().ToByteArray());
            // TODO temporary solution
            // this.mintStartTime = Utils.UnixTimeNowMs() * 1000L;
            // this.mintEndTime = this.mintStartTime + 1000L * 60L * 60L * 24L * 365L;
        }
    }

    [System.Serializable]
    public struct ERC1155SignedPayloadOutput
    {
        public string to;
        public string tokenId;
        public string price;
        public string currencyAddress;
        public string primarySaleRecipient;
        public string royaltyRecipient;
        public int royaltyBps;
        public int quantity;
        public string uri;
        public string uid;
        public long mintStartTime;
        public long mintEndTime;
    }

    [System.Serializable]
    public struct ERC1155SignedPayload
    {
        public string signature;
        public ERC1155SignedPayloadOutput payload;
    }

    /// <summary>
    /// Generate, verify and mint signed mintable payloads
    /// </summary>
    public class ERC1155Signature : Routable
    {
        /// <summary>
        /// Generate, verify and mint signed mintable payloads
        /// </summary>
        public ERC1155Signature(string parentRoute)
            : base(Routable.append(parentRoute, "signature")) { }

        /// <summary>
        /// Generate a signed mintable payload. Requires minting permission.
        /// </summary>
        public async Task<ERC1155SignedPayload> Generate(ERC1155MintPayload payloadToSign)
        {
            return await Bridge.InvokeRoute<ERC1155SignedPayload>(getRoute("generate"), Utils.ToJsonStringArray(payloadToSign));
        }

        public async Task<ERC1155SignedPayload> GenerateFromTokenId(ERC1155MintAdditionalPayload payloadToSign)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<ERC1155SignedPayload>(
                    getRoute("generateFromTokenId"),
                    Utils.ToJsonStringArray(payloadToSign)
                );
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Verify that a signed mintable payload is valid
        /// </summary>
        public async Task<bool> Verify(ERC1155SignedPayload signedPayload)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<bool>(getRoute("verify"), Utils.ToJsonStringArray(signedPayload));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Mint a signed mintable payload
        /// </summary>
        public async Task<TransactionResult> Mint(ERC1155SignedPayload signedPayload)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("mint"), Utils.ToJsonStringArray(signedPayload));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }
    }
}
