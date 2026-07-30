using Content.Server.Station.Systems;
using Content.Shared._CMU14.Yautja;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaMilitaryCasteRoleTest
{
    [Test]
    public async Task MilitaryCasteJobsAreHiddenEventOnlyRoles()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            var soldier = prototypes.Index<JobPrototype>("CMUYautjaMilitaryCasteSoldier");
            var enforcer = prototypes.Index<JobPrototype>("CMUYautjaMilitaryCasteEnforcer");

            Assert.Multiple(() =>
            {
                AssertMilitaryCasteJob(soldier,
                    "CMUMobYautjaMilitaryCasteSoldier",
                    "CMUYautjaMilitaryCasteSoldierGear");
                AssertMilitaryCasteJob(enforcer,
                    "CMUMobYautjaMilitaryCasteEnforcer",
                    "CMUYautjaMilitaryCasteEnforcerGear");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MilitaryCasteJobsSpawnWithFixedRoleGear()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var inventory = entMan.System<InventorySystem>();
            var stationSpawning = entMan.System<StationSpawningSystem>();
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithName("Military Caste Test")
                .WithYautjaProfile(YautjaCharacterProfile.Default.WithName("Military Caste Test"));

            var soldier = stationSpawning.SpawnPlayerMob(
                map.GridCoords,
                "CMUYautjaMilitaryCasteSoldier",
                profile,
                station: null);
            var enforcer = stationSpawning.SpawnPlayerMob(
                map.GridCoords.Offset(new System.Numerics.Vector2(1, 0)),
                "CMUYautjaMilitaryCasteEnforcer",
                profile,
                station: null);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<YautjaComponent>(soldier), Is.True);
                AssertEquippedPrototype(entMan, inventory, soldier, "ears", "CMUYautjaMilitaryCommunicator");
                AssertEquippedPrototype(entMan, inventory, soldier, "head", "CMUYautjaPoweredHelmet");
                AssertEquippedPrototype(entMan, inventory, soldier, "gloves", "CMUYautjaSoldierBracers");
                AssertEquippedPrototype(entMan, inventory, soldier, "outerClothing", "CMUYautjaPoweredArmor");
                AssertEquippedPrototype(entMan, inventory, soldier, "shoes", "CMUYautjaPoweredGreaves");

                Assert.That(entMan.HasComponent<YautjaComponent>(enforcer), Is.True);
                AssertEquippedPrototype(entMan, inventory, enforcer, "ears", "CMUYautjaMilitaryCommunicator");
                AssertEquippedPrototype(entMan, inventory, enforcer, "head", "CMUYautjaPoweredHelmet");
                AssertEquippedPrototype(entMan, inventory, enforcer, "gloves", "CMUYautjaSoldierBracers");
                AssertEquippedPrototype(entMan, inventory, enforcer, "outerClothing", "CMUYautjaPoweredArmorEnforcer");
                AssertEquippedPrototype(entMan, inventory, enforcer, "shoes", "CMUYautjaPoweredGreaves");
                AssertEquippedPrototype(entMan, inventory, enforcer, "back", "CMUYautjaCannonPack");
            });
        });

        await pair.CleanReturnAsync();
    }

    private static void AssertMilitaryCasteJob(
        JobPrototype job,
        string expectedEntity,
        string expectedStartingGear)
    {
        Assert.That(job.Hidden, Is.True);
        Assert.That(job.Whitelisted, Is.False);
        Assert.That(job.CanBeAntag, Is.False);
        Assert.That(job.JoinNotifyCrew, Is.False);
        Assert.That(job.UsePlayerProfile, Is.False);
        Assert.That(job.JobEntity, Is.EqualTo(expectedEntity));
        Assert.That(job.JobPreviewEntity?.ToString(), Is.EqualTo(expectedEntity));
        Assert.That(job.StartingGear?.ToString(), Is.EqualTo(expectedStartingGear));
    }

    private static void AssertEquippedPrototype(
        IEntityManager entMan,
        InventorySystem inventory,
        EntityUid wearer,
        string slot,
        string expectedPrototype)
    {
        Assert.That(inventory.TryGetSlotEntity(wearer, slot, out var equipped), Is.True, slot);
        var meta = entMan.GetComponent<MetaDataComponent>(equipped.Value);
        Assert.That(meta.EntityPrototype?.ID, Is.EqualTo(expectedPrototype), slot);
    }
}
