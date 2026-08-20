using System;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Governance;

/// <summary>
/// Executes Event Governance actions that were already approved against a reviewed manifest.
/// Discord only records the request; this system is the authoritative in-game execution plane.
/// </summary>
public sealed class GovernanceEventActionSystem : EntitySystem
{
    private const float PollIntervalSeconds = 1f;
    private const int MaxActionsPerPoll = 10;
    private const int MaxAnnouncementLength = 1000;

    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _database = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private float _pollAccumulator;
    private bool _processing;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _pollAccumulator += frameTime;
        if (_pollAccumulator < PollIntervalSeconds || _processing || _ticker.RoundId <= 0)
            return;

        _pollAccumulator = 0f;
        _ = ProcessPendingAsync();
    }

    private async Task ProcessPendingAsync()
    {
        _processing = true;
        try
        {
            await _database.FailUnexecutableGovernanceEventActionsAsync(_ticker.RoundId);

            for (var index = 0; index < MaxActionsPerPoll; index++)
            {
                var action = await _database.ClaimGovernanceEventActionAsync(_ticker.RoundId);
                if (action == null)
                    break;

                string? error;
                try
                {
                    error = ExecuteAction(action);
                }
                catch (Exception exception)
                {
                    Log.Error($"Governance event action {action.Id} failed during game execution: {exception}");
                    error = "Внутренняя ошибка игрового исполнителя события.";
                }

                if (!await _database.CompleteGovernanceEventActionAsync(action.Id, error == null, error))
                    Log.Error($"Governance event action {action.Id} could not persist its execution result.");
            }
        }
        catch (Exception exception)
        {
            Log.Error($"Governance event execution polling failed: {exception}");
        }
        finally
        {
            _processing = false;
        }
    }

    private string? ExecuteAction(GovernanceEventExecutionAction action)
    {
        return action.Capability switch
        {
            "event.spawn" => ExecuteSpawn(action),
            "event.announce" => ExecuteAnnouncement(action),
            _ => $"Полномочие «{action.Capability}» не имеет игрового исполнителя.",
        };
    }

    private string? ExecuteSpawn(GovernanceEventExecutionAction action)
    {
        if (!_prototypes.TryIndex<EntityPrototype>(action.Resource, out _))
            return $"Прототип «{action.Resource}» не найден.";

        if (!_players.TryGetSessionById(action.ActorUserId, out var director) ||
            director.AttachedEntity is not { } directorEntity || Deleted(directorEntity))
        {
            return "Директор события должен находиться в игре для event.spawn.";
        }

        if (!TryReadObject(action.Payload, out var payload, out var payloadError))
            return payloadError;

        if (payload.TryGetProperty("count", out var countElement) &&
            (!countElement.TryGetInt32(out var count) || count != 1))
        {
            return "Одно применение event.spawn создаёт ровно одну сущность; количество ограничивается max_uses манифеста.";
        }

        EntityManager.SpawnEntity(action.Resource, Transform(directorEntity).Coordinates);
        return null;
    }

    private string? ExecuteAnnouncement(GovernanceEventExecutionAction action)
    {
        if (!action.Resource.Equals("server", StringComparison.OrdinalIgnoreCase))
            return "Для event.announce разрешён только ресурс «server».";

        if (!TryReadObject(action.Payload, out var payload, out var payloadError))
            return payloadError;

        if (!payload.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.String)
            return "Для event.announce требуется payload с полем text.";

        var text = textElement.GetString()?.Trim() ?? string.Empty;
        if (text.Length is < 1 or > MaxAnnouncementLength)
            return $"Текст объявления должен содержать от 1 до {MaxAnnouncementLength} символов.";

        _chat.DispatchServerAnnouncement($"[Событие] {text}");
        return null;
    }

    private static bool TryReadObject(string json, out JsonElement payload, out string? error)
    {
        payload = default;
        error = null;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Payload действия события должен быть JSON-объектом.";
                return false;
            }

            payload = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            error = "Payload действия события содержит некорректный JSON.";
            return false;
        }
    }
}
