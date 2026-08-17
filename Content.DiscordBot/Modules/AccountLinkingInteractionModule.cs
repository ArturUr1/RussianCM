using Discord;
using Discord.Interactions;

namespace Content.DiscordBot.Modules;

[Group("аккаунт", "Привязка аккаунта SS14")]
public sealed class AccountLinkingInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("панель", "Создать панель привязки аккаунта")]
    [RequireOwner]
    public Task CreatePanelAsync()
    {
        var component = new ComponentBuilder()
            .WithButton("Привязать аккаунт SS14", "link-ss14-account")
            .Build();
        return RespondAsync("Привяжите аккаунт SS14 с помощью кнопки ниже.", components: component);
    }
}
