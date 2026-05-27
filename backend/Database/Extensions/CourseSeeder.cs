using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds multiple Italian courses into the database.
/// Returns the list of course IDs for use by downstream seeders.
/// </summary>
public static class CourseSeeder
{
    private static readonly List<CourseData> Courses = new()
    {
        new(
            Title: "Italian for Beginners",
            Description: "A structured introduction to Italian covering greetings, essential vocabulary, present tense verbs, and everyday conversation. Designed for Bulgarian speakers learning Italian from scratch.",
            EstimatedDurationHours: 12,
            OrderIndex: 0
        ),
        new(
            Title: "Everyday Conversations",
            Description: "Master practical Italian for daily situations: shopping, dining, transportation, and social interactions. Build confidence in real-world scenarios.",
            EstimatedDurationHours: 10,
            OrderIndex: 1
        ),
        new(
            Title: "Italian Grammar Essentials",
            Description: "Deep dive into Italian grammar: verb conjugations, articles, prepositions, and sentence structure. Strengthen your grammatical foundation.",
            EstimatedDurationHours: 15,
            OrderIndex: 2
        ),
        new(
            Title: "Italian Culture & Idioms",
            Description: "Explore Italian culture through idiomatic expressions, traditions, and regional variations. Understand the context behind the language.",
            EstimatedDurationHours: 8,
            OrderIndex: 3
        )
    };

    public static async Task<List<string>> SeedAsync(
        BackendDbContext context,
        string languageId
    )
    {
        var courseIds = new List<string>();

        foreach (var courseData in Courses)
        {
            var existing = await context.Courses.FirstOrDefaultAsync(c =>
                c.LanguageId == languageId && c.Title == courseData.Title
            );

            if (existing != null)
            {
                courseIds.Add(existing.CourseId);
                continue;
            }

            var course = new Course
            {
                LanguageId = languageId,
                Title = courseData.Title,
                Description = courseData.Description,
                EstimatedDurationHours = courseData.EstimatedDurationHours,
                OrderIndex = courseData.OrderIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            context.Courses.Add(course);
            await context.SaveChangesAsync();
            courseIds.Add(course.CourseId);
        }

        return courseIds;
    }

    private record CourseData(
        string Title,
        string Description,
        int EstimatedDurationHours,
        int OrderIndex
    );
}
