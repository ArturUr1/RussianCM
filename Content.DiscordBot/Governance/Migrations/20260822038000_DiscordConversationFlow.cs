using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.DiscordBot.Governance.Migrations;

[DbContext(typeof(GovernanceDbContext))]
[Migration("20260822038000_DiscordConversationFlow")]
public sealed class DiscordConversationFlow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS governance.ahelp_discord_sync (
                ticket_id bigint PRIMARY KEY REFERENCES governance.ahelp_tickets(id) ON DELETE CASCADE,
                status_message_id bigint,
                last_message_id bigint NOT NULL DEFAULT 0,
                last_status text,
                updated_at timestamptz NOT NULL DEFAULT now()
            );

            CREATE TABLE IF NOT EXISTS governance.court_defense_confirmations (
                case_id bigint NOT NULL REFERENCES governance.court_cases(id) ON DELETE CASCADE,
                user_id uuid NOT NULL REFERENCES governance.users(id) ON DELETE CASCADE,
                confirmed_at timestamptz NOT NULL DEFAULT now(),
                PRIMARY KEY (case_id, user_id)
            );

            CREATE INDEX IF NOT EXISTS court_defense_confirmations_case_idx
                ON governance.court_defense_confirmations(case_id, confirmed_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS governance.court_defense_confirmations;
            DROP TABLE IF EXISTS governance.ahelp_discord_sync;
            """);
    }
}
