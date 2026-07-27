using JoseonHunter.Domain.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class AttackInstanceTests
    {
        [Test]
        public void FlyingBladeAllowsOneHitPerOutboundAndInboundPhase()
        {
            var attack = new AttackInstance(31, RepeatHitPolicy.OncePerPhase, 0.5f);

            Assert.That(attack.TryRecordHit(7, ContactPhase.Outbound, 0f), Is.True);
            Assert.That(attack.TryRecordHit(7, ContactPhase.Outbound, 0.01f), Is.False);
            Assert.That(attack.TryRecordHit(7, ContactPhase.Inbound, 0.2f), Is.True);
        }

        [Test]
        public void OncePerInstanceRejectsEveryRepeatedContact()
        {
            var attack = new AttackInstance(31, RepeatHitPolicy.OncePerInstance, 0.5f);

            Assert.That(attack.TryRecordHit(7, ContactPhase.Outbound, 0f), Is.True);
            Assert.That(attack.TryRecordHit(7, ContactPhase.Inbound, 1f), Is.False);
        }

        [Test]
        public void TimedTicksRequireTheConfiguredInterval()
        {
            var attack = new AttackInstance(31, RepeatHitPolicy.TimedTicks, 0.5f);

            Assert.That(attack.TryRecordHit(7, ContactPhase.Tick, 0f), Is.True);
            Assert.That(attack.TryRecordHit(7, ContactPhase.Tick, 0.49f), Is.False);
            Assert.That(attack.TryRecordHit(7, ContactPhase.Tick, 0.5f), Is.True);
        }

        [Test]
        public void BoundaryReentryUsesTheConfiguredReentryWindow()
        {
            var attack = new AttackInstance(31, RepeatHitPolicy.BoundaryReentry, 0.5f);

            Assert.That(attack.TryRecordHit(7, ContactPhase.BoundaryCrossing, 0f), Is.True);
            Assert.That(attack.TryRecordHit(7, ContactPhase.BoundaryCrossing, 0.2f), Is.False);
            Assert.That(attack.TryRecordHit(7, ContactPhase.BoundaryCrossing, 0.5f), Is.True);
        }

        [Test]
        public void ResetClearsHitMemory()
        {
            var attack = new AttackInstance(31, RepeatHitPolicy.OncePerInstance, 0.5f);
            attack.TryRecordHit(7, ContactPhase.Direct, 0f);

            attack.Reset();

            Assert.That(attack.TryRecordHit(7, ContactPhase.Direct, 0.1f), Is.True);
        }
    }
}
