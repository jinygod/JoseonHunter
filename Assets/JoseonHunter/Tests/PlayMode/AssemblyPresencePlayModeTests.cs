using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class AssemblyPresencePlayModeTests
    {
        [UnityTest]
        public IEnumerator ProductionAssembliesAreResolvable()
        {
            Assert.That(JoseonHunter.Domain.ProjectIdentity.ProductName,
                Is.EqualTo("JoseonHunter"));
            Assert.That(typeof(JoseonHunter.Runtime.AssemblyMarker), Is.Not.Null);
            yield return null;
        }
    }
}
