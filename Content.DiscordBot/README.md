# RussianCM Discord bot and Community Court

`Content.DiscordBot` is the only Discord process used by RussianCM. It handles both
the existing SS14 account linking flow and Community Court. The game server and the
bot are separate processes from this repository and share the game PostgreSQL.

Community Court uses `GovernanceDbContext`; PostgreSQL is authoritative for cases,
statements, invitations, jurors, votes, deadlines, ratings, audits, Discord thread
IDs, and verdict publication. The bot applies the idempotent EF migration on startup.
It also keeps a session-level PostgreSQL advisory lock, so a second Court process
exits before logging into Discord. Never run two processes with the same bot token.

## Configuration

Copy `config.example.json` to `config.json` next to the process, set environment
variables, or pass `--env-file <path>`. Both the C# names (`DATABASE_STRING`,
`DISCORD_GUILD`, `COURT_CHANNEL`) and the former prototype names
(`GAME_DATABASE_URL`, `DISCORD_GUILD_ID`, `COURT_FORUM_CHANNEL_ID`) are accepted.
PostgreSQL URLs are converted to Npgsql connection strings. `CourtChannel` may be a
Discord Forum or a text channel where the bot can create, write, lock, and archive
public threads.

The bot needs only the Guilds and Guild Messages gateway intents; no privileged
intents are required. Invite it to the configured guild with application-command,
thread, message, and member-view permissions before enabling the scheduler. Use the
owner-only `/аккаунт панель` command to create the existing linking button.

```powershell
dotnet run --project Content.DiscordBot/Content.DiscordBot.csproj
```

`rmc.discord_token` is unrelated: it belongs to the in-game admin/mentor chat bridge
and is not read by Community Court. The former Python `rucm_court` process is not a
runtime dependency and must not be started after this bot is deployed.

## Discord commands

- `/суд жалоба` creates a PostgreSQL case and its public Discord thread.
- `/суд защита` records and publishes the defendant statement.
- `/суд присяжный` mirrors the in-game invitation response transactionally.
- `/суд голос` records a secret guilt-phase vote.
- `/суд наказание` records a secret sentencing-phase vote.
- `/суд статус` displays the current PostgreSQL state.

The scheduler advances defense deadlines, expires invitations, synchronizes in-game
responses, selects conflict-free above-average jurors, replaces timed-out nonvoters,
sends DMs, and publishes and archives final decisions.
