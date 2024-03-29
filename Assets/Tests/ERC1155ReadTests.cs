using System.Collections;
using System.Numerics;
using NUnit.Framework;
using Thirdweb;
using UnityEngine;
using UnityEngine.TestTools;

public class ERC1155ReadTests : ConfigManager
{
    private GameObject _go;
    private string _dropErc1155Address = "0x6A7a26c9a595E6893C255C9dF0b593e77518e0c3";

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
    public IEnumerator GetContract_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        Assert.IsNotNull(contract);
        Assert.AreEqual(_dropErc1155Address, contract.Address);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_Get_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var currencyInfoTask = contract.ERC1155.Get("1");
        yield return new WaitUntil(() => currencyInfoTask.IsCompleted);
        if (currencyInfoTask.IsFaulted)
            throw currencyInfoTask.Exception;
        Assert.IsTrue(currencyInfoTask.IsCompletedSuccessfully);
        Assert.IsNotNull(currencyInfoTask.Result);
        Assert.AreEqual("1", currencyInfoTask.Result.metadata.id);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_GetAll_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var currencyInfoTask = contract.ERC1155.GetAll();
        yield return new WaitUntil(() => currencyInfoTask.IsCompleted);
        if (currencyInfoTask.IsFaulted)
            throw currencyInfoTask.Exception;
        Assert.IsTrue(currencyInfoTask.IsCompletedSuccessfully);
        Assert.IsNotNull(currencyInfoTask.Result);
        Assert.GreaterOrEqual(currencyInfoTask.Result.Count, 0);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_GetOwned_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var currencyInfoTask = contract.ERC1155.GetOwned(_dropErc1155Address);
        yield return new WaitUntil(() => currencyInfoTask.IsCompleted);
        if (currencyInfoTask.IsFaulted)
            throw currencyInfoTask.Exception;
        Assert.IsTrue(currencyInfoTask.IsCompletedSuccessfully);
        Assert.IsNotNull(currencyInfoTask.Result);
        Assert.GreaterOrEqual(currencyInfoTask.Result.Count, 0);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_BalanceOf_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var balanceTask = contract.ERC1155.BalanceOf(_dropErc1155Address, "1");
        yield return new WaitUntil(() => balanceTask.IsCompleted);
        if (balanceTask.IsFaulted)
            throw balanceTask.Exception;
        Assert.IsTrue(balanceTask.IsCompletedSuccessfully);
        Assert.IsNotNull(balanceTask.Result);
        Assert.GreaterOrEqual(balanceTask.Result, BigInteger.Zero);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_IsApprovedForAll_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var allowanceTask = contract.ERC1155.IsApprovedForAll(_dropErc1155Address, _dropErc1155Address);
        yield return new WaitUntil(() => allowanceTask.IsCompleted);
        if (allowanceTask.IsFaulted)
            throw allowanceTask.Exception;
        Assert.IsTrue(allowanceTask.IsCompletedSuccessfully);
        Assert.IsNotNull(allowanceTask.Result);
        Assert.IsTrue(allowanceTask.Result == true || allowanceTask.Result == false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_TotalCount_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var totalSupplyTask = contract.ERC1155.TotalCount();
        yield return new WaitUntil(() => totalSupplyTask.IsCompleted);
        if (totalSupplyTask.IsFaulted)
            throw totalSupplyTask.Exception;
        Assert.IsTrue(totalSupplyTask.IsCompletedSuccessfully);
        Assert.IsNotNull(totalSupplyTask.Result);
        Assert.GreaterOrEqual(totalSupplyTask.Result, BigInteger.Zero);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ERC1155_TotalSupply_Success()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(_dropErc1155Address);
        var totalSupplyTask = contract.ERC1155.TotalSupply("1");
        yield return new WaitUntil(() => totalSupplyTask.IsCompleted);
        if (totalSupplyTask.IsFaulted)
            throw totalSupplyTask.Exception;
        Assert.IsTrue(totalSupplyTask.IsCompletedSuccessfully);
        Assert.IsNotNull(totalSupplyTask.Result);
        Assert.GreaterOrEqual(totalSupplyTask.Result, BigInteger.Zero);
        yield return null;
    }
}
