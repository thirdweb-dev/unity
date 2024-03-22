using System.Collections;
using System.Numerics;
using NUnit.Framework;
using Thirdweb;
using UnityEngine;
using UnityEngine.TestTools;

public class PackReadTests : ConfigManager
{
    private GameObject _go;
    private string _packAddress = "0xE33653ce510Ee767d8824b5EcDeD27125D49889D";

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
    public IEnumerator Get_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_packAddress);
        var packTask = contract.Pack.Get("0");
        yield return new WaitUntil(() => packTask.IsCompleted);
        Assert.IsTrue(packTask.IsCompletedSuccessfully);
        Assert.IsNotNull(packTask.Result);
    }

    [UnityTest]
    public IEnumerator BalanceOf_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_packAddress);
        var packTask = contract.Pack.BalanceOf(_packAddress, "0");
        yield return new WaitUntil(() => packTask.IsCompleted);
        Assert.IsTrue(packTask.IsCompletedSuccessfully);
        Assert.IsNotNull(packTask.Result);
    }

    [UnityTest]
    public IEnumerator IsApprovedForAll_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_packAddress);
        var packTask = contract.Pack.IsApprovedForAll(_packAddress, _packAddress);
        yield return new WaitUntil(() => packTask.IsCompleted);
        Assert.IsTrue(packTask.IsCompletedSuccessfully);
        Assert.IsNotNull(packTask.Result);
    }

    [UnityTest]
    public IEnumerator TotalSupply_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_packAddress);
        var packTask = contract.Pack.TotalSupply("0");
        yield return new WaitUntil(() => packTask.IsCompleted);
        Assert.IsTrue(packTask.IsCompletedSuccessfully);
        Assert.IsNotNull(packTask.Result);
        Assert.GreaterOrEqual(packTask.Result, BigInteger.Zero);
    }

    [UnityTest]
    public IEnumerator GetPackContents_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_packAddress);
        var packTask = contract.Pack.GetPackContents("0");
        yield return new WaitUntil(() => packTask.IsCompleted);
        Assert.IsTrue(packTask.IsCompletedSuccessfully);
        Assert.IsNotNull(packTask.Result);
        Assert.IsNotNull(packTask.Result.erc20Rewards);
        Assert.IsNotNull(packTask.Result.erc721Rewards);
        Assert.IsNotNull(packTask.Result.erc1155Rewards);
        Assert.GreaterOrEqual(packTask.Result.erc20Rewards.Count, 0);
        Assert.GreaterOrEqual(packTask.Result.erc721Rewards.Count, 0);
        Assert.GreaterOrEqual(packTask.Result.erc1155Rewards.Count, 0);
    }
}
