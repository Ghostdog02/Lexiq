using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations;

/// <inheritdoc />
public partial class ConfigureRelationships : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Languages",
            columns: table => new
            {
                Id = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false
                ),
                FlagIconUrl = table.Column<string>(
                    type: "nvarchar(255)",
                    maxLength: 255,
                    nullable: true
                ),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Languages", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "Courses",
            columns: table => new
            {
                Id = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LanguageId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false
                ),
                Description = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true
                ),
                EstimatedDurationHours = table.Column<int>(type: "int", nullable: true),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
                CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Courses_Languages_LanguageId",
                    column: x => x.LanguageId,
                    principalTable: "Languages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_Courses_Users_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "UserLanguages",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                LanguageId = table.Column<int>(type: "int", nullable: false),
                EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLanguages", x => new { x.UserId, x.LanguageId });
                table.ForeignKey(
                    name: "FK_UserLanguages_Languages_LanguageId",
                    column: x => x.LanguageId,
                    principalTable: "Languages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_UserLanguages_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Lessons",
            columns: table => new
            {
                Id = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CourseId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(
                    type: "nvarchar(200)",
                    maxLength: 200,
                    nullable: false
                ),
                Description = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true
                ),
                EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
                LessonMediaUrl = table.Column<string>(
                    type: "nvarchar(255)",
                    maxLength: 255,
                    nullable: true
                ),
                LessonTextUrl = table.Column<string>(
                    type: "nvarchar(255)",
                    maxLength: 255,
                    nullable: false
                ),
                IsLocked = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Lessons", x => x.Id);
                table.ForeignKey(
                    name: "FK_Lessons_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Exercises",
            columns: table => new
            {
                Id = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LessonId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(
                    type: "nvarchar(200)",
                    maxLength: 200,
                    nullable: false
                ),
                Instructions = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true
                ),
                EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                DifficultyLevel = table.Column<int>(type: "int", nullable: true),
                Points = table.Column<int>(type: "int", nullable: false),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Exercises", x => x.Id);
                table.ForeignKey(
                    name: "FK_Exercises_Lessons_LessonId",
                    column: x => x.LessonId,
                    principalTable: "Lessons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Questions",
            columns: table => new
            {
                Id = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ExerciseId = table.Column<int>(type: "int", nullable: false),
                QuestionText = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: false
                ),
                QuestionAudioUrl = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true
                ),
                QuestionImageUrl = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true
                ),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
                Points = table.Column<int>(type: "int", nullable: false),
                Explanation = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true
                ),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                QuestionType = table.Column<string>(
                    type: "nvarchar(21)",
                    maxLength: 21,
                    nullable: false
                ),
                FillInBlankQuestion_CorrectAnswer = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true
                ),
                FillInBlankQuestion_AcceptedAnswers = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true
                ),
                FillInBlankQuestion_CaseSensitive = table.Column<bool>(
                    type: "bit",
                    nullable: true
                ),
                TrimWhitespace = table.Column<bool>(type: "bit", nullable: true),
                AudioUrl = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true
                ),
                CorrectAnswer = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true
                ),
                AcceptedAnswers = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true
                ),
                CaseSensitive = table.Column<bool>(type: "bit", nullable: true),
                MaxReplays = table.Column<int>(type: "int", nullable: true),
                SourceLanguageCode = table.Column<string>(
                    type: "nvarchar(10)",
                    maxLength: 10,
                    nullable: true
                ),
                TargetLanguageCode = table.Column<string>(
                    type: "nvarchar(10)",
                    maxLength: 10,
                    nullable: true
                ),
                MatchingThreshold = table.Column<double>(type: "float", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Questions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Questions_Exercises_ExerciseId",
                    column: x => x.ExerciseId,
                    principalTable: "Exercises",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "QuestionOptions",
            columns: table => new
            {
                Id = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                QuestionId = table.Column<int>(type: "int", nullable: false),
                OptionText = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: false
                ),
                IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_QuestionOptions_Questions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "Questions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_Courses_CreatedById",
            table: "Courses",
            column: "CreatedById"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Courses_LanguageId",
            table: "Courses",
            column: "LanguageId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Exercises_LessonId",
            table: "Exercises",
            column: "LessonId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_CourseId",
            table: "Lessons",
            column: "CourseId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_QuestionOptions_QuestionId",
            table: "QuestionOptions",
            column: "QuestionId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Questions_ExerciseId",
            table: "Questions",
            column: "ExerciseId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_UserLanguages_LanguageId",
            table: "UserLanguages",
            column: "LanguageId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QuestionOptions");

        migrationBuilder.DropTable(name: "UserLanguages");

        migrationBuilder.DropTable(name: "Questions");

        migrationBuilder.DropTable(name: "Exercises");

        migrationBuilder.DropTable(name: "Lessons");

        migrationBuilder.DropTable(name: "Courses");

        migrationBuilder.DropTable(name: "Languages");
    }
}
