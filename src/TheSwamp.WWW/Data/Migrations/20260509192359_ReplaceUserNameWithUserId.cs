using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSwamp.WWW.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserNameWithUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RenameTable (ChatMessages → ChatMessage) and RenameColumn (SentAt → SentOnUtc)
            // were applied manually to the database and are intentionally omitted here.

            // Drop the PK dynamically so this works regardless of whether the PK constraint
            // was renamed when the table was renamed (sp_rename leaves it as PK_ChatMessages;
            // SSMS designer may have changed it to PK_ChatMessage).
            migrationBuilder.Sql(@"
                DECLARE @pkName NVARCHAR(256) = (
                    SELECT name FROM sys.key_constraints
                    WHERE parent_object_id = OBJECT_ID('ChatMessage') AND type = 'PK'
                );
                EXEC('ALTER TABLE ChatMessage DROP CONSTRAINT [' + @pkName + ']');
            ");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "ChatMessage");

            // Rename the index if it still has the pre-rename name.
            // If your index is already named IX_ChatMessage_SentOnUtc, comment this out.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ChatMessages_SentAt'
                      AND object_id = OBJECT_ID('ChatMessage')
                )
                EXEC sp_rename N'ChatMessage.IX_ChatMessages_SentAt',
                               N'IX_ChatMessage_SentOnUtc', N'INDEX';
            ");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "ChatMessage",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ChatMessage",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChatMessage");

            // Restore index name.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ChatMessage_SentOnUtc'
                      AND object_id = OBJECT_ID('ChatMessage')
                )
                EXEC sp_rename N'ChatMessage.IX_ChatMessage_SentOnUtc',
                               N'IX_ChatMessages_SentAt', N'INDEX';
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatMessage",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "ChatMessage",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                DECLARE @pkName NVARCHAR(256) = (
                    SELECT name FROM sys.key_constraints
                    WHERE parent_object_id = OBJECT_ID('ChatMessage') AND type = 'PK'
                );
                EXEC('ALTER TABLE ChatMessage DROP CONSTRAINT [' + @pkName + ']');
            ");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessage",
                column: "Id");

            // NOTE: Down does NOT rename the table/column back since those were applied
            // manually. If rolling back this migration, restore those manually.
        }
    }
}
