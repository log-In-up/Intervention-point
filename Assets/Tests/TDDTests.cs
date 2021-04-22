using Assertions = UnityEngine.Assertions;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;

public class TDDTests
{
    const float waitingTime = 0.1f;

    [SetUp]
    public void SetUp()
    {

    }

    [TearDown]
    public void TearDown()
    {

    }

    [UnityTest]
    public IEnumerator SimpleTest()
    {
        yield return new WaitForSeconds(waitingTime);
    }
}
