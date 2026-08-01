using Content.Shared._CMU14.Yautja;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaHealingGunCmss13ParityTest
{
    [Test]
    public async Task HealingGunLoadsOneDiscreteCapsuleAndRefusesSecondLoad()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var gun = entMan.SpawnEntity("CMUYautjaHealingGun", MapCoordinates.Nullspace);
            var user = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var firstCapsule = entMan.SpawnEntity("CMUYautjaHealingCapsule", MapCoordinates.Nullspace);
            var secondCapsule = entMan.SpawnEntity("CMUYautjaHealingCapsule", MapCoordinates.Nullspace);

            try
            {
                entMan.GetComponent<YautjaHealingGunComponent>(gun).Loaded = false;

                var firstLoad = new AfterInteractUsingEvent(user, firstCapsule, gun, default, true);
                entMan.EventBus.RaiseLocalEvent(gun, firstLoad);

                Assert.Multiple(() =>
                {
                    Assert.That(firstLoad.Handled, Is.True,
                        "CMSS13 healing_gun accepts a discrete healing_gel capsule when empty.");
                    Assert.That(entMan.Deleted(firstCapsule), Is.True,
                        "Loading the CMSS13 healing_gel consumes the discrete capsule.");
                });

                var secondLoad = new AfterInteractUsingEvent(user, secondCapsule, gun, default, true);
                entMan.EventBus.RaiseLocalEvent(gun, secondLoad);

                Assert.Multiple(() =>
                {
                    Assert.That(secondLoad.Handled, Is.False,
                        "CMSS13 healing_gun refuses a second capsule while already loaded.");
                    Assert.That(entMan.Deleted(secondCapsule), Is.False,
                        "A capsule must not be consumed by a failed reload attempt.");
                });
            }
            finally
            {
                if (!entMan.Deleted(gun))
                    entMan.DeleteEntity(gun);
                if (!entMan.Deleted(user))
                    entMan.DeleteEntity(user);
                if (!entMan.Deleted(secondCapsule))
                    entMan.DeleteEntity(secondCapsule);
            }
        });

        await pair.CleanReturnAsync();
    }
}
