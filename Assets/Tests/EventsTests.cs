using System.Collections;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NUnit.Framework;
using Thirdweb;
using UnityEngine;
using UnityEngine.TestTools;

public class EventsTests : ConfigManager
{
    [Event("Transfer")]
    public class TransferEventDTO : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "to", 2, true)]
        public string To { get; set; }

        [Parameter("uint256", "tokenId", 3, true)]
        public BigInteger TokenId { get; set; }
    }

    public struct TransferEvent
    {
        public string from;
        public string to;
        public string tokenId;
    }

    private GameObject _go;
    private string _dropErc721Address = "0xD811CB13169C175b64bf8897e2Fd6a69C6343f5C";

    [SetUp]
    public void SetUp()
    {
        var existingManager = GameObject.FindObjectOfType<ThirdwebManager>();
        if (existingManager != null)
            GameObject.DestroyImmediate(existingManager.gameObject);

        _go = new GameObject("ThirdwebManager");
        _go.AddComponent<ThirdwebManager>();

        ThirdwebManager.Instance.clientId = GetClientId();
        ThirdwebManager.Instance.Initialize("arbitrum-sepolia");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            GameObject.DestroyImmediate(_go);
            _go = null;
        }
    }

    [UnityTest]
    public IEnumerator GetEventLogs_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc721Address);
        if (Utils.IsWebGLBuild())
        {
            var transferEvents = contract.Events.Get<TransferEvent>("Transfer");
            yield return new WaitUntil(() => transferEvents.IsCompleted);
            if (transferEvents.IsFaulted)
                throw transferEvents.Exception;
            Assert.IsTrue(transferEvents.IsCompletedSuccessfully);
            Assert.IsNotNull(transferEvents.Result);
            Assert.Greater(transferEvents.Result.Count, 0);
            yield return null;
        }
        else
        {
            var transferEvents = contract.GetEventLogs<TransferEventDTO>();
            yield return new WaitUntil(() => transferEvents.IsCompleted);
            if (transferEvents.IsFaulted)
                throw transferEvents.Exception;
            Assert.IsTrue(transferEvents.IsCompletedSuccessfully);
            Assert.IsNotNull(transferEvents.Result);
            Assert.Greater(transferEvents.Result.Count, 0);
            yield return null;
        }
    }
}
