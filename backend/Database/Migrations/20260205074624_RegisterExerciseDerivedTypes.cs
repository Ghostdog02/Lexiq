using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class RegisterExerciseDerivedTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FillInBlankExercise columns
            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

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

            migrationBuilder.AddColumn<bool>(
                name: "TrimWhitespace",
                table: "Exercises",
                type: "bit",
                nullable: true);

            // ListeningExercise columns (prefixed where name clashes with FillInBlank)
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListeningExercise_CorrectAnswer",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
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

            migrationBuilder.AddColumn<int>(
                name: "MaxReplays",
                table: "Exercises",
                type: "int",
                nullable: true);

            // TranslationExercise columns
            migrationBuilder.AddColumn<string>(
                name: "SourceText",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetText",
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
                name: "TargetLanguageCode",
                table: "Exercises",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MatchingThreshold",
                table: "Exercises",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Text",                          table: "Exercises");
            migrationBuilder.DropColumn(name: "CorrectAnswer",                 table: "Exercises");
            migrationBuilder.DropColumn(name: "AcceptedAnswers",               table: "Exercises");
            migrationBuilder.DropColumn(name: "CaseSensitive",                 table: "Exercises");
            migrationBuilder.DropColumn(name: "TrimWhitespace",                table: "Exercises");
            migrationBuilder.DropColumn(name: "AudioUrl",                      table: "Exercises");
            migrationBuilder.DropColumn(name: "ListeningExercise_CorrectAnswer",    table: "Exercises");
            migrationBuilder.DropColumn(name: "ListeningExercise_AcceptedAnswers",  table: "Exercises");
            migrationBuilder.DropColumn(name: "ListeningExercise_CaseSensitive",    table: "Exercises");
            migrationBuilder.DropColumn(name: "MaxReplays",                    table: "Exercises");
            migrationBuilder.DropColumn(name: "SourceText",                    table: "Exercises");
            migrationBuilder.DropColumn(name: "TargetText",                    table: "Exercises");
            migrationBuilder.DropColumn(name: "SourceLanguageCode",            table: "Exercises");
            migrationBuilder.DropColumn(name: "TargetLanguageCode",            table: "Exercises");
            migrationBuilder.DropColumn(name: "MatchingThreshold",             table: "Exercises");
        }
    }
}
