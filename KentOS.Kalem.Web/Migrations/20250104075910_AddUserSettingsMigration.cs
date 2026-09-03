using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    hide_old_agendas = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_created = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_organized = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_status_change = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_image_upload = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_note_added = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_postponed = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_flower_sent = table.Column<bool>(type: "boolean", nullable: false),
                    agenda_on_flower_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_created = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_organized = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_file_attached = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_status_change = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_note_added = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_remittance = table.Column<bool>(type: "boolean", nullable: false),
                    request_on_added_to_agenda = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
