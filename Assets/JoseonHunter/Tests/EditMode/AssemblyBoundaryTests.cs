using System.Linq;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void RequiredFirstPartyAssembliesAreLoaded()
        {
            var names = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName().Name)
                .ToHashSet();

            Assert.That(names, Does.Contain("JoseonHunter.Domain"));
            Assert.That(names, Does.Contain("JoseonHunter.Content"));
            Assert.That(names, Does.Contain("JoseonHunter.Runtime"));
            Assert.That(names, Does.Contain("JoseonHunter.Presentation"));
            Assert.That(names, Does.Contain("JoseonHunter.Infrastructure"));
            Assert.That(names, Does.Contain("JoseonHunter.Editor"));
        }
    }
}
