using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds exercises for all lessons in the Italian Beginners course.
/// Each lesson gets 3-4 exercises covering different exercise types.
/// </summary>
public static class ExerciseSeeder
{
    private const string AudioPlaceholder = "/static/uploads/audio/placeholder.mp3";
    private const string AdminUserId = "system-admin";

    public static async Task SeedAsync(BackendDbContext context, List<string> lessonIds)
    {
        // Idempotency: if exercises exist for any lesson, assume full seed completed
        if (await context.Exercises.AnyAsync(e => lessonIds.Contains(e.LessonId)))
        {
            return;
        }

        var exercises = new List<Exercise>();

        for (int i = 0; i < lessonIds.Count; i++)
        {
            var lessonExercises = BuildExercisesForLesson(lessonIds[i], i);
            exercises.AddRange(lessonExercises);
        }

        context.Exercises.AddRange(exercises);
        await context.SaveChangesAsync();
    }

    private static List<Exercise> BuildExercisesForLesson(string lessonId, int lessonIndex) =>
        lessonIndex switch
        {
            0 => BuildGreetingsExercises(lessonId),
            1 => BuildNumbersExercises(lessonId),
            2 => BuildColorsExercises(lessonId),
            3 => BuildFoodExercises(lessonId),
            4 => BuildTravelExercises(lessonId),
            5 => BuildVerbsExercises(lessonId),
            6 => BuildTimeExercises(lessonId),
            7 => BuildConversationExercises(lessonId),
            _ => [],
        };

    // ══════════════════════════════════════════════════════════════════
    // Lesson 0: Greetings and Introductions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildGreetingsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Ciao' mean?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Hello / Goodbye",
                        IsCorrect = true,
                        Explanation = "'Ciao' is an informal greeting used for both hello and goodbye in Italian.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Thank you",
                        IsCorrect = false,
                        Explanation = "'Grazie' means thank you in Italian.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Good evening",
                        IsCorrect = false,
                        Explanation = "'Buonasera' means good evening.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Please",
                        IsCorrect = false,
                        Explanation = "'Per favore' or 'Prego' means please.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Mi ____ Marco. (My name is Marco.)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "chiamo",
                        IsCorrect = true,
                        Explanation = "'Mi chiamo' means 'My name is' or literally 'I call myself'.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "sono",
                        IsCorrect = false,
                        Explanation = "'Sono' means 'I am', which is not the standard way to introduce yourself in Italian.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "ho",
                        IsCorrect = false,
                        Explanation = "'Ho' means 'I have', which doesn't fit this context.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Buonasera' is used from late afternoon onwards.",
                CorrectAnswer = true,
                Explanation = "'Buonasera' (good evening) is typically used from around 4-5 PM until late night.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 1: Numbers 1 to 20
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildNumbersExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What number is this?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "cinque (5)",
                        IsCorrect = true,
                        Explanation = "The Italian word 'cinque' means five.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "tre (3)",
                        IsCorrect = false,
                        Explanation = "'Tre' means three.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "sette (7)",
                        IsCorrect = false,
                        Explanation = "'Sette' means seven.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "nove (9)",
                        IsCorrect = false,
                        Explanation = "'Nove' means nine.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Count in Italian",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "uno, due, ____, quattro (one, two, ____, four)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "tre",
                        IsCorrect = true,
                        Explanation = "'Tre' is the Italian word for three.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "cinque",
                        IsCorrect = false,
                        Explanation = "'Cinque' means five, which comes after four.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "sei",
                        IsCorrect = false,
                        Explanation = "'Sei' means six.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "The Italian word for 'ten' is 'dieci'.",
                CorrectAnswer = true,
                Explanation = "'Dieci' is indeed the correct Italian word for ten.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 2: Colors
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildColorsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What color is 'rosso'?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Red",
                        IsCorrect = true,
                        Explanation = "'Rosso' is the Italian word for red.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Blue",
                        IsCorrect = false,
                        Explanation = "'Blu' means blue in Italian.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Yellow",
                        IsCorrect = false,
                        Explanation = "'Giallo' means yellow.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Green",
                        IsCorrect = false,
                        Explanation = "'Verde' means green.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the phrase",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Il cielo è ____. (The sky is blue.)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "blu",
                        IsCorrect = true,
                        Explanation = "'Blu' is the Italian word for blue.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "verde",
                        IsCorrect = false,
                        Explanation = "'Verde' means green.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "giallo",
                        IsCorrect = false,
                        Explanation = "'Giallo' means yellow.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Nero' means white in Italian.",
                CorrectAnswer = false,
                Explanation = "'Nero' means black. 'Bianco' means white.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 3: Food and Drink
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildFoodExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'acqua' mean?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Water",
                        IsCorrect = true,
                        Explanation = "'Acqua' is the Italian word for water.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Wine",
                        IsCorrect = false,
                        Explanation = "'Vino' means wine.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Coffee",
                        IsCorrect = false,
                        Explanation = "'Caffè' means coffee.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Milk",
                        IsCorrect = false,
                        Explanation = "'Latte' means milk.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Vorrei un ____ di caffè. (I would like a cup of coffee.)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "tazza",
                        IsCorrect = true,
                        Explanation = "'Tazza' means cup in Italian.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "bicchiere",
                        IsCorrect = false,
                        Explanation = "'Bicchiere' means glass, typically used for water or wine.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "piatto",
                        IsCorrect = false,
                        Explanation = "'Piatto' means plate.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Pizza' and 'pasta' are Italian words used worldwide.",
                CorrectAnswer = true,
                Explanation = "These are indeed Italian words that have been adopted into many languages globally.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 4: Travel and Directions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTravelExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'stazione' mean?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Station",
                        IsCorrect = true,
                        Explanation = "'Stazione' means station (train station, bus station, etc.).",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Airport",
                        IsCorrect = false,
                        Explanation = "'Aeroporto' means airport.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Hotel",
                        IsCorrect = false,
                        Explanation = "'Albergo' or 'hotel' means hotel.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Street",
                        IsCorrect = false,
                        Explanation = "'Strada' or 'via' means street.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the question",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Dov'è la ____? (Where is the station?)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "stazione",
                        IsCorrect = true,
                        Explanation = "'Stazione' means station.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "piazza",
                        IsCorrect = false,
                        Explanation = "'Piazza' means square or plaza.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "strada",
                        IsCorrect = false,
                        Explanation = "'Strada' means street or road.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Sinistra' means left and 'destra' means right.",
                CorrectAnswer = true,
                Explanation = "These are the correct Italian words for left and right directions.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 5: Common Verbs
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildVerbsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'mangiare' mean?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "To eat",
                        IsCorrect = true,
                        Explanation = "'Mangiare' is the Italian verb meaning 'to eat'.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "To drink",
                        IsCorrect = false,
                        Explanation = "'Bere' means 'to drink'.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "To sleep",
                        IsCorrect = false,
                        Explanation = "'Dormire' means 'to sleep'.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "To speak",
                        IsCorrect = false,
                        Explanation = "'Parlare' means 'to speak'.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Conjugate the verb",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Text = "Io ____ italiano. (I speak Italian.)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "parlo",
                        IsCorrect = true,
                        Explanation = "'Parlo' is the first-person singular form of 'parlare' (to speak).",
                    },
                    new ExerciseOption
                    {
                        OptionText = "parli",
                        IsCorrect = false,
                        Explanation = "'Parli' is the second-person singular (you speak).",
                    },
                    new ExerciseOption
                    {
                        OptionText = "parla",
                        IsCorrect = false,
                        Explanation = "'Parla' is the third-person singular (he/she speaks).",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "Italian verbs are conjugated based on the subject pronoun.",
                CorrectAnswer = true,
                Explanation = "Yes, Italian verbs change their ending based on who is performing the action.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 6: Time and Days
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTimeExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'oggi' mean?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Today",
                        IsCorrect = true,
                        Explanation = "'Oggi' means today in Italian.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Yesterday",
                        IsCorrect = false,
                        Explanation = "'Ieri' means yesterday.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Tomorrow",
                        IsCorrect = false,
                        Explanation = "'Domani' means tomorrow.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Now",
                        IsCorrect = false,
                        Explanation = "'Adesso' or 'ora' means now.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Che ore ____? (What time is it?)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "sono",
                        IsCorrect = true,
                        Explanation = "'Che ore sono?' is the standard way to ask 'What time is it?'",
                    },
                    new ExerciseOption
                    {
                        OptionText = "è",
                        IsCorrect = false,
                        Explanation = "'È' is used for singular (one o'clock), but 'sono' is standard for the question.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "hai",
                        IsCorrect = false,
                        Explanation = "'Hai' means 'you have', which doesn't fit here.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "The Italian week starts with Monday (lunedì).",
                CorrectAnswer = true,
                Explanation = "In Italy, the week traditionally starts on Monday, not Sunday.",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 7: Basic Conversation
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildConversationExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Come stai?' mean?",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "How are you?",
                        IsCorrect = true,
                        Explanation = "'Come stai?' is an informal way to ask 'How are you?'",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Where are you?",
                        IsCorrect = false,
                        Explanation = "'Dove sei?' means 'Where are you?'",
                    },
                    new ExerciseOption
                    {
                        OptionText = "What's your name?",
                        IsCorrect = false,
                        Explanation = "'Come ti chiami?' means 'What's your name?'",
                    },
                    new ExerciseOption
                    {
                        OptionText = "Nice to meet you",
                        IsCorrect = false,
                        Explanation = "'Piacere' means 'Nice to meet you'.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the response",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Come stai? - Bene, ____! (How are you? - Well, thanks!)",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "grazie",
                        IsCorrect = true,
                        Explanation = "'Grazie' means 'thank you' or 'thanks'.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "prego",
                        IsCorrect = false,
                        Explanation = "'Prego' means 'you're welcome'.",
                    },
                    new ExerciseOption
                    {
                        OptionText = "scusa",
                        IsCorrect = false,
                        Explanation = "'Scusa' means 'excuse me' or 'sorry'.",
                    },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                CreatedById = AdminUserId,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Arrivederci' is a formal way to say goodbye.",
                CorrectAnswer = true,
                Explanation = "'Arrivederci' is more formal than 'ciao' and literally means 'until we see each other again'.",
            },
        ];
}
