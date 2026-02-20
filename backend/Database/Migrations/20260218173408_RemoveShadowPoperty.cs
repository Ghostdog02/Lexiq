using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowPoperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserExerciseProgress_Exercises_ExerciseId1",
                table: "UserExerciseProgress"
            );

            migrationBuilder.DropIndex(
                name: "IX_UserExerciseProgress_ExerciseId1",
                table: "UserExerciseProgress"
            );

            migrationBuilder.DropColumn(name: "ExerciseId1", table: "UserExerciseProgress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExerciseId1",
                table: "UserExerciseProgress",
                type: "nvarchar(450)",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserExerciseProgress_ExerciseId1",
                table: "UserExerciseProgress",
                column: "ExerciseId1"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserExerciseProgress_Exercises_ExerciseId1",
                table: "UserExerciseProgress",
                column: "ExerciseId1",
                principalTable: "Exercises",
                principalColumn: "Id"
            );
        }
    }
}
