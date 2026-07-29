using Content.Shared.Chemistry;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaChemistryVisualTest
{
    [Test]
    public async Task ShipGlasswareFillLayersUseDedicatedRsiStates()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var spriteSystem = client.System<SpriteSystem>();

        EntityUid beaker = default;
        EntityUid vial = default;
        NetEntity beakerNet = default;
        NetEntity vialNet = default;

        await server.WaitPost(() =>
        {
            var entMan = server.EntMan;
            beaker = entMan.SpawnEntity("CMUHunterShipSilverCatalystBeaker", map.GridCoords);
            vial = entMan.SpawnEntity("CMUHunterShipPlacedBaseChemistryEmptyVialVialSouthOffset1x7", map.GridCoords.Offset(new(1, 0)));
            beakerNet = entMan.GetNetEntity(beaker);
            vialNet = entMan.GetNetEntity(vial);
        });

        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            Assert.That(client.EntMan.TryGetEntity(beakerNet, out var clientBeaker), Is.True);
            Assert.That(client.EntMan.TryGetEntity(vialNet, out var clientVial), Is.True);

            Assert.Multiple(() =>
            {
                AssertShipGlasswareFillRsi(
                    client.EntMan,
                    spriteSystem,
                    clientBeaker.Value,
                    "CMUHunterShipSilverCatalystBeaker",
                    new ResPath("/Textures/_CMU14/HunterShip/obj/items/chemistry.rsi"),
                    "beakerlarge",
                    5);

                AssertShipGlasswareFillRsi(
                    client.EntMan,
                    spriteSystem,
                    clientVial.Value,
                    "CMUHunterShipPlacedBaseChemistryEmptyVialVialSouthOffset1x7",
                    new ResPath("/Textures/_CMU14/HunterShip/obj/items/chemistry.rsi"),
                    "vial",
                    6);
            });
        });

        await pair.CleanReturnAsync();
    }

    private static void AssertShipGlasswareFillRsi(
        IEntityManager entMan,
        SpriteSystem spriteSystem,
        EntityUid uid,
        string id,
        ResPath expectedBaseRsi,
        string fillBaseName,
        int maxFillLevels)
    {
        var sprite = entMan.GetComponent<SpriteComponent>(uid);

        Assert.That(spriteSystem.TryGetLayer((uid, sprite), 0, out var baseLayer, false), Is.True, $"{id} base layer missing");
        Assert.That(spriteSystem.TryGetLayer((uid, sprite), SolutionContainerLayers.Fill, out var fillLayer, false), Is.True, $"{id} fill layer missing");

        Assert.That(baseLayer!.ActualRsi?.Path, Is.EqualTo(expectedBaseRsi), $"{id} base layer RSI");
        Assert.That(fillLayer!.ActualRsi, Is.Not.Null, $"{id} fill layer RSI");

        for (var i = 1; i <= maxFillLevels; i++)
        {
            var state = $"{fillBaseName}{i}";
            Assert.That(fillLayer.ActualRsi!.TryGetState(state, out _), Is.True,
                $"{id} fill layer RSI {fillLayer.ActualRsi.Path} should contain state {state}");
        }
    }
}
