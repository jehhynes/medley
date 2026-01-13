using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationRunningTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if is_running column exists before adding
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name='chat_conversations' AND column_name='is_running') THEN
                        ALTER TABLE chat_conversations ADD COLUMN is_running boolean NOT NULL DEFAULT FALSE;
                    END IF;
                END $$;
            ");

            // Check if current_conversation_id column exists before adding
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name='articles' AND column_name='current_conversation_id') THEN
                        ALTER TABLE articles ADD COLUMN current_conversation_id uuid;
                    END IF;
                END $$;
            ");

            // Check if index exists before creating
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_articles_current_conversation_id') THEN
                        CREATE UNIQUE INDEX ix_articles_current_conversation_id ON articles(current_conversation_id);
                    END IF;
                END $$;
            ");

            // Check if foreign key exists before creating
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                                   WHERE constraint_name = 'fk_articles_chat_conversations_current_conversation_id') THEN
                        ALTER TABLE articles ADD CONSTRAINT fk_articles_chat_conversations_current_conversation_id 
                            FOREIGN KEY (current_conversation_id) REFERENCES chat_conversations(id) ON DELETE SET NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_articles_chat_conversations_current_conversation_id",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "ix_articles_current_conversation_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "is_running",
                table: "chat_conversations");

            migrationBuilder.DropColumn(
                name: "current_conversation_id",
                table: "articles");
        }
    }
}
