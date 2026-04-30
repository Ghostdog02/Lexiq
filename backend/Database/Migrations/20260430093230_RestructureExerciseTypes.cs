using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class RestructureExerciseTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Languages_LanguageId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseOption_Exercises_ExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Lessons_LessonId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Courses_CourseId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_UserExerciseProgress_Exercises_ExerciseId",
                table: "UserExerciseProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLanguages_Languages_LanguageId",
                table: "UserLanguages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lessons",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Languages",
                table: "Languages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExerciseOption",
                table: "ExerciseOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Courses",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_LanguageId",
                table: "Courses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Achievements",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "AcceptedAnswers",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CaseSensitive",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ListeningExercise_AcceptedAnswers",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ListeningExercise_CaseSensitive",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MatchingThreshold",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Question",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SourceLanguageCode",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SourceText",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "TargetLanguageCode",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "TrimWhitespace",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ExerciseOption");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "ExerciseOption");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Achievements");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Languages",
                newName: "LanguageName");

            migrationBuilder.RenameColumn(
                name: "TargetText",
                table: "Exercises",
                newName: "Statement");

            migrationBuilder.RenameColumn(
                name: "ListeningExercise_CorrectAnswer",
                table: "Exercises",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Achievements",
                newName: "AchievementName");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageId",
                table: "UserLanguages",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseId",
                table: "UserExerciseProgress",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<double>(
                name: "EaseFactor",
                table: "UserExerciseProgress",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "UserExerciseProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewedAt",
                table: "UserExerciseProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "UserExerciseProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Repetitions",
                table: "UserExerciseProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "AchievementId",
                table: "UserAchievements",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "Lessons",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "LessonId",
                table: "Lessons",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FlagIconUrl",
                table: "Languages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageId",
                table: "Languages",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LessonId",
                table: "Exercises",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Exercises",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(34)",
                oldMaxLength: 34);

            migrationBuilder.AlterColumn<bool>(
                name: "CorrectAnswer",
                table: "Exercises",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExerciseId",
                table: "Exercises",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Exercises",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Exercises",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseId",
                table: "ExerciseOption",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ExerciseOptionId",
                table: "ExerciseOption",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "ExerciseOption",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FillInBlankExerciseExerciseId",
                table: "ExerciseOption",
                type: "nvarchar(36)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListeningExerciseExerciseId",
                table: "ExerciseOption",
                type: "nvarchar(36)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanguageId",
                table: "Courses",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "EstimatedDurationHours",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseId",
                table: "Courses",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AchievementId",
                table: "Achievements",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lessons",
                table: "Lessons",
                column: "LessonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Languages",
                table: "Languages",
                column: "LanguageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises",
                column: "ExerciseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExerciseOption",
                table: "ExerciseOption",
                column: "ExerciseOptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Courses",
                table: "Courses",
                column: "CourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Achievements",
                table: "Achievements",
                column: "AchievementId");

            migrationBuilder.CreateTable(
                name: "AudioMatchPair",
                columns: table => new
                {
                    AudioMatchPairId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    AudioMatchingExerciseId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    AudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioMatchPair", x => x.AudioMatchPairId);
                    table.ForeignKey(
                        name: "FK_AudioMatchPair_Exercises_AudioMatchingExerciseId",
                        column: x => x.AudioMatchingExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageOption",
                columns: table => new
                {
                    ImageOptionId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ImageChoiceExerciseId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageOption", x => x.ImageOptionId);
                    table.ForeignKey(
                        name: "FK_ImageOption_Exercises_ImageChoiceExerciseId",
                        column: x => x.ImageChoiceExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLanguages_UserId",
                table: "UserLanguages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExerciseProgress_UserId",
                table: "UserExerciseProgress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId",
                table: "UserAchievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId_OrderIndex",
                table: "Lessons",
                columns: new[] { "CourseId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Languages_LanguageName",
                table: "Languages",
                column: "LanguageName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CreatedById",
                table: "Exercises",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseOption_FillInBlankExerciseExerciseId",
                table: "ExerciseOption",
                column: "FillInBlankExerciseExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseOption_ListeningExerciseExerciseId",
                table: "ExerciseOption",
                column: "ListeningExerciseExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_LanguageId_OrderIndex",
                table: "Courses",
                columns: new[] { "LanguageId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_AudioMatchPair_AudioMatchingExerciseId",
                table: "AudioMatchPair",
                column: "AudioMatchingExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageOption_ImageChoiceExerciseId",
                table: "ImageOption",
                column: "ImageChoiceExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Languages_LanguageId",
                table: "Courses",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseOption_Exercises_ExerciseId",
                table: "ExerciseOption",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseOption_Exercises_FillInBlankExerciseExerciseId",
                table: "ExerciseOption",
                column: "FillInBlankExerciseExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseOption_Exercises_ListeningExerciseExerciseId",
                table: "ExerciseOption",
                column: "ListeningExerciseExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Lessons_LessonId",
                table: "Exercises",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "LessonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_CreatedById",
                table: "Exercises",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Courses_CourseId",
                table: "Lessons",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId",
                principalTable: "Achievements",
                principalColumn: "AchievementId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserExerciseProgress_Exercises_ExerciseId",
                table: "UserExerciseProgress",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLanguages_Languages_LanguageId",
                table: "UserLanguages",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Languages_LanguageId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseOption_Exercises_ExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseOption_Exercises_FillInBlankExerciseExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseOption_Exercises_ListeningExerciseExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Lessons_LessonId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_CreatedById",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Courses_CourseId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_UserExerciseProgress_Exercises_ExerciseId",
                table: "UserExerciseProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLanguages_Languages_LanguageId",
                table: "UserLanguages");

            migrationBuilder.DropTable(
                name: "AudioMatchPair");

            migrationBuilder.DropTable(
                name: "ImageOption");

            migrationBuilder.DropIndex(
                name: "IX_UserLanguages_UserId",
                table: "UserLanguages");

            migrationBuilder.DropIndex(
                name: "IX_UserExerciseProgress_UserId",
                table: "UserExerciseProgress");

            migrationBuilder.DropIndex(
                name: "IX_UserAchievements_UserId",
                table: "UserAchievements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lessons",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CourseId_OrderIndex",
                table: "Lessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Languages",
                table: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_Languages_LanguageName",
                table: "Languages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_CreatedById",
                table: "Exercises");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExerciseOption",
                table: "ExerciseOption");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseOption_FillInBlankExerciseExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseOption_ListeningExerciseExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Courses",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_LanguageId_OrderIndex",
                table: "Courses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Achievements",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "EaseFactor",
                table: "UserExerciseProgress");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "UserExerciseProgress");

            migrationBuilder.DropColumn(
                name: "LastReviewedAt",
                table: "UserExerciseProgress");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "UserExerciseProgress");

            migrationBuilder.DropColumn(
                name: "Repetitions",
                table: "UserExerciseProgress");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseOptionId",
                table: "ExerciseOption");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "ExerciseOption");

            migrationBuilder.DropColumn(
                name: "FillInBlankExerciseExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropColumn(
                name: "ListeningExerciseExerciseId",
                table: "ExerciseOption");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AchievementId",
                table: "Achievements");

            migrationBuilder.RenameColumn(
                name: "LanguageName",
                table: "Languages",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Statement",
                table: "Exercises",
                newName: "TargetText");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Exercises",
                newName: "ListeningExercise_CorrectAnswer");

            migrationBuilder.RenameColumn(
                name: "AchievementName",
                table: "Achievements",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageId",
                table: "UserLanguages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseId",
                table: "UserExerciseProgress",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "AchievementId",
                table: "UserAchievements",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Lessons",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "Lessons",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Lessons",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Lessons",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FlagIconUrl",
                table: "Languages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Languages",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LessonId",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Exercises",
                type: "nvarchar(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AlterColumn<string>(
                name: "CorrectAnswer",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AcceptedAnswers",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CaseSensitive",
                table: "Exercises",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Exercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListeningExercise_AcceptedAnswers",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ListeningExercise_CaseSensitive",
                table: "Exercises",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MatchingThreshold",
                table: "Exercises",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Exercises",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Question",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceLanguageCode",
                table: "Exercises",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceText",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetLanguageCode",
                table: "Exercises",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Exercises",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TrimWhitespace",
                table: "Exercises",
                type: "bit",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExerciseId",
                table: "ExerciseOption",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "ExerciseOption",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "ExerciseOption",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "LanguageId",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<int>(
                name: "EstimatedDurationHours",
                table: "Courses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Achievements",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lessons",
                table: "Lessons",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Languages",
                table: "Languages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExerciseOption",
                table: "ExerciseOption",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Courses",
                table: "Courses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Achievements",
                table: "Achievements",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_LanguageId",
                table: "Courses",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Languages_LanguageId",
                table: "Courses",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseOption_Exercises_ExerciseId",
                table: "ExerciseOption",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Lessons_LessonId",
                table: "Exercises",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Courses_CourseId",
                table: "Lessons",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId",
                principalTable: "Achievements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserExerciseProgress_Exercises_ExerciseId",
                table: "UserExerciseProgress",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLanguages_Languages_LanguageId",
                table: "UserLanguages",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
