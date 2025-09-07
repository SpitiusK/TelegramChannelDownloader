using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TelegramChannelDownloader.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class InitialTelegramSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "download_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalMessages = table.Column<int>(type: "integer", nullable: false),
                    ProcessedMessages = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExportFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExportPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_download_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DownloadSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromId = table.Column<long>(type: "bigint", nullable: false),
                    FromUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FromDisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    HasMedia = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MediaType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MediaFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MediaFileSize = table.Column<long>(type: "bigint", nullable: true),
                    MediaMimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    Forwards = table.Column<int>(type: "integer", nullable: false),
                    Reactions = table.Column<string>(type: "jsonb", nullable: true),
                    IsForwarded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ForwardedFromId = table.Column<long>(type: "bigint", nullable: true),
                    ForwardedFromMessageId = table.Column<long>(type: "bigint", nullable: true),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RawData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_telegram_messages_download_sessions",
                        column: x => x.DownloadSessionId,
                        principalTable: "download_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_download_sessions_channel_id",
                table: "download_sessions",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_download_sessions_channel_status",
                table: "download_sessions",
                columns: new[] { "ChannelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_download_sessions_expires_at",
                table: "download_sessions",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_download_sessions_started_at",
                table: "download_sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_download_sessions_status",
                table: "download_sessions",
                column: "Status");

            // Note: GIN trigram index removed for initial setup
            // Will be added in a future migration when pg_trgm extension is available
            // migrationBuilder.CreateIndex(
            //     name: "IX_telegram_messages_content_gin",
            //     table: "telegram_messages",
            //     column: "Content")
            //     .Annotation("Npgsql:IndexMethod", "gin")
            //     .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_date",
                table: "telegram_messages",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_from_id",
                table: "telegram_messages",
                column: "FromId");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_reply_to",
                table: "telegram_messages",
                column: "ReplyToMessageId",
                filter: "\"ReplyToMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_session_date",
                table: "telegram_messages",
                columns: new[] { "DownloadSessionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_session_id",
                table: "telegram_messages",
                column: "DownloadSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_session_media",
                table: "telegram_messages",
                columns: new[] { "DownloadSessionId", "HasMedia" });

            migrationBuilder.CreateIndex(
                name: "IX_telegram_messages_session_type",
                table: "telegram_messages",
                columns: new[] { "DownloadSessionId", "MessageType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_messages");

            migrationBuilder.DropTable(
                name: "download_sessions");
        }
    }
}
