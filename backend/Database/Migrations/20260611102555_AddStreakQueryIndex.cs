using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStreakQueryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserExerciseProgress_UserId_IsCompleted_CompletedAt",
                table: "UserExerciseProgress",
                columns: new[] { "UserId", "IsCompleted", "CompletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserExerciseProgress_UserId_IsCompleted_CompletedAt",
                table: "UserExerciseProgress");
        }
    }
}
