using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedRoleProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("Role", "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("Role", "Users");
        }
    }
}
