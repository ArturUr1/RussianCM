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

            -- Forum post starter messages normally share the thread id. Seed that value so the
            -- synchronizer edits the original AHelp card instead of posting a second status card.
            -- For text-channel threads the lookup simply misses and the synchronizer replaces the
            -- seed with the id of the dedicated status message it creates.
            INSERT INTO governance.ahelp_discord_sync(ticket_id, status_message_id)
            SELECT ticket.id, ticket.discord_thread_id
            FROM governance.ahelp_tickets AS ticket
            WHERE ticket.discord_thread_id IS NOT NULL
            ON CONFLICT (ticket_id) DO NOTHING;

            CREATE OR REPLACE FUNCTION governance.seed_ahelp_discord_sync()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $governance$
            BEGIN
                IF NEW.discord_thread_id IS NOT NULL THEN
                    INSERT INTO governance.ahelp_discord_sync(ticket_id, status_message_id)
                    VALUES (NEW.id, NEW.discord_thread_id)
                    ON CONFLICT (ticket_id) DO NOTHING;
                END IF;
                RETURN NEW;
            END;
            $governance$;

            DROP TRIGGER IF EXISTS governance_ahelp_seed_discord_sync ON governance.ahelp_tickets;
            CREATE TRIGGER governance_ahelp_seed_discord_sync
            AFTER INSERT OR UPDATE OF discord_thread_id ON governance.ahelp_tickets
            FOR EACH ROW
            EXECUTE FUNCTION governance.seed_ahelp_discord_sync();

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
            DROP TRIGGER IF EXISTS governance_ahelp_seed_discord_sync ON governance.ahelp_tickets;
            DROP FUNCTION IF EXISTS governance.seed_ahelp_discord_sync();
            DROP TABLE IF EXISTS governance.court_defense_confirmations;
            DROP TABLE IF EXISTS governance.ahelp_discord_sync;
            """);
    }
}
