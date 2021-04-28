using Assertions = UnityEngine.Assertions;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;

namespace InterventionPoint
{
    public class TestCase
    {
        #region Parameters
        private const float waitingTime = 0.1f, startingPointHeight = 1.3f;
        private const int zero = 0, one = 1;

        private PlayerController player = null;

        Vector3 playerStartPoint;
        #endregion

        [SetUp]
        public void SetUp()
        {
            playerStartPoint = new Vector3(zero, startingPointHeight, zero);
            player = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Player"), playerStartPoint, Quaternion.identity).GetComponent<PlayerController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player.gameObject);
        }

        #region Tests

        #region Player movement tests
        [UnityTest]
        public IEnumerator PlayerMoveForward()
        {
            player.Walk(new Vector3(zero, zero, one).normalized);

            yield return new WaitForSeconds(waitingTime);

            Assert.Greater(player.transform.position.z, playerStartPoint.z);
        }

        [UnityTest]
        public IEnumerator PlayerMoveBack()
        {
            player.Walk(new Vector3(zero, zero, -one).normalized);

            yield return new WaitForSeconds(waitingTime);

            Assert.Less(player.transform.position.z, playerStartPoint.z);
        }

        [UnityTest]
        public IEnumerator PlayerMoveOnLeft()
        {
            player.Walk(new Vector3(-one, zero, zero).normalized);

            yield return new WaitForSeconds(waitingTime);

            Assert.Less(player.transform.position.x, playerStartPoint.x);
        }

        [UnityTest]
        public IEnumerator PlayerMoveOnRight()
        {
            player.Walk(new Vector3(one, zero, zero).normalized);

            yield return new WaitForSeconds(waitingTime);

            Assert.Greater(player.transform.position.x, playerStartPoint.x);
        }
        #endregion

        #endregion
    }
}
