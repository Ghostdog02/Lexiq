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
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "What does 'Ciao' mean?",
                Instructions = "Select the correct English translation.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation =
                    "'Ciao' is an informal greeting used for both hello and goodbye in Italian.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Hello / Goodbye",
                        IsCorrect = true,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Thank you",
                        IsCorrect = false,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Good evening",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Please",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Complete the sentence",
                Instructions = "Fill in the blank: Mi _______ Marco. (My name is Marco.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Mi chiamo' means 'My name is' or literally 'I call myself'.",
                Text = "Mi _______ Marco.",
                CorrectAnswer = "chiamo",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate to Italian",
                Instructions = "How do you say 'Good evening' in Italian?",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Buonasera' is used from late afternoon onwards.",
                SourceText = "Good evening",
                TargetText = "Buonasera",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.85,
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Listen and write",
                Instructions = "Listen to the greeting and write what you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 3,
                Explanation = "'Buongiorno' is used from morning until early afternoon.",
                AudioUrl = AudioPlaceholder,
                CorrectAnswer = "Buongiorno",
                AcceptedAnswers = "buon giorno",
                CaseSensitive = false,
                MaxReplays = 3,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 1: Numbers 1 to 20
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildNumbersExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "Which number is 'cinque'?",
                Instructions = "Select the correct number.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Cinque' is the Italian word for 5.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "3",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "5",
                        IsCorrect = true,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "7",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "9",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Write the number",
                Instructions = "The number 10 in Italian is _______.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Dieci' is 10 in Italian.",
                Text = "The number 10 in Italian is _______.",
                CorrectAnswer = "dieci",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate the number",
                Instructions = "Translate 'tre' to English.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 2,
                Explanation = "'Tre' means three in Italian.",
                SourceText = "tre",
                TargetText = "three",
                SourceLanguageCode = "it",
                TargetLanguageCode = "en",
                MatchingThreshold = 0.9,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 2: Colors and Descriptions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildColorsExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "What color is 'verde'?",
                Instructions = "Pick the correct color.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Verde' means green in Italian.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Red",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Blue",
                        IsCorrect = false,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Green",
                        IsCorrect = true,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Yellow",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Black in Italian",
                Instructions = "The Italian word for black is _______.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Nero' is black in Italian (masculine form).",
                Text = "The Italian word for black is _______.",
                CorrectAnswer = "nero",
                AcceptedAnswers = "nera",
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate the color",
                Instructions = "Translate 'bianco' to English.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 2,
                Explanation = "'Bianco' means white in Italian.",
                SourceText = "bianco",
                TargetText = "white",
                SourceLanguageCode = "it",
                TargetLanguageCode = "en",
                MatchingThreshold = 0.9,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 3: Food and Ordering
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildFoodExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "What does 'il pane' mean?",
                Instructions = "Select the correct translation.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Il pane' is Italian for bread.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Cheese",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Bread",
                        IsCorrect = true,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Coffee",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Water",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Order politely",
                Instructions = "Complete: Un caffe, _______. (A coffee, please.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Per favore' means 'please' and is essential for polite ordering.",
                Text = "Un caffe, _______.",
                CorrectAnswer = "per favore",
                AcceptedAnswers = "perfavore",
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate the food word",
                Instructions = "What is 'l'acqua' in English?",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 2,
                Explanation = "'L'acqua' means water in Italian.",
                SourceText = "l'acqua",
                TargetText = "water",
                SourceLanguageCode = "it",
                TargetLanguageCode = "en",
                MatchingThreshold = 0.85,
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Listen and identify",
                Instructions = "Listen and type the food word you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 3,
                Explanation = "The word spoken is 'formaggio' (cheese).",
                AudioUrl = AudioPlaceholder,
                CorrectAnswer = "formaggio",
                AcceptedAnswers = null,
                CaseSensitive = false,
                MaxReplays = 3,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 4: Travel and Directions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTravelExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "What does 'destra' mean?",
                Instructions = "Choose the correct direction.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Destra' means right. 'Sinistra' means left.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Left",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Right",
                        IsCorrect = true,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Straight",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Behind",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Give directions",
                Instructions = "Complete: La stazione e a _______. (The station is on the left.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'A sinistra' means 'on the left'.",
                Text = "La stazione e a _______.",
                CorrectAnswer = "sinistra",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate the question",
                Instructions = "Translate 'Where is the hotel?' to Italian.",
                EstimatedDurationMinutes = 7,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Dov'e l'hotel?' or 'Dove e l'hotel?' both work.",
                SourceText = "Where is the hotel?",
                TargetText = "Dov'e l'hotel?",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.75,
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Listen and write",
                Instructions = "Write down the direction you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 3,
                Explanation = "'Arrivederci' is the formal goodbye.",
                AudioUrl = AudioPlaceholder,
                CorrectAnswer = "Arrivederci",
                AcceptedAnswers = null,
                CaseSensitive = false,
                MaxReplays = 3,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 5: Present Tense Verbs
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildVerbsExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "'Io parlo' - who is speaking?",
                Instructions = "Which pronoun does 'io' refer to?",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Io' is the first-person singular pronoun: I.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "You",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "I",
                        IsCorrect = true,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "We",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "They",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Conjugate for 'noi'",
                Instructions = "Complete: Noi parl_______. (We speak.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                OrderIndex = 1,
                Explanation = "The 'noi' form of 'parlare' is 'parliamo'.",
                Text = "Noi parl_______.",
                CorrectAnswer = "iamo",
                AcceptedAnswers = "parliamo",
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate the sentence",
                Instructions = "Translate 'She speaks Italian' to Italian.",
                EstimatedDurationMinutes = 7,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Lei parla italiano' - 'lei' can mean both 'she' and formal 'you'.",
                SourceText = "She speaks Italian",
                TargetText = "Lei parla italiano",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.75,
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Hear the conjugation",
                Instructions = "Listen and type the verb form you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                OrderIndex = 3,
                Explanation = "The spoken word is 'parlano' (they speak).",
                AudioUrl = AudioPlaceholder,
                CorrectAnswer = "parlano",
                AcceptedAnswers = null,
                CaseSensitive = false,
                MaxReplays = 3,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 6: Days and Time
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTimeExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "Which day is 'venerdi'?",
                Instructions = "Select the day of the week.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Venerdi' is Friday.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "Wednesday",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Thursday",
                        IsCorrect = false,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Friday",
                        IsCorrect = true,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Saturday",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Sunday in Italian",
                Instructions = "Sunday in Italian is _______.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Domenica' is Sunday - the only day that's feminine!",
                Text = "Sunday in Italian is _______.",
                CorrectAnswer = "domenica",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "What time is it?",
                Instructions = "Translate 'It is three o'clock' to Italian.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 2,
                Explanation = "'Sono le tre' - Note: Use 'Sono le' for 2+, but 'E l'una' for 1.",
                SourceText = "It is three o'clock",
                TargetText = "Sono le tre",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.75,
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 7: First Conversations
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildConversationExercises(string lessonId) =>
        [
            new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = "What does 'Dove' mean?",
                Instructions = "Select the correct question word.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // First exercise is unlocked
                Explanation = "'Dove' means 'Where' in Italian.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "What",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new ExerciseOption
                    {
                        OptionText = "When",
                        IsCorrect = false,
                        OrderIndex = 1,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Where",
                        IsCorrect = true,
                        OrderIndex = 2,
                    },
                    new ExerciseOption
                    {
                        OptionText = "Why",
                        IsCorrect = false,
                        OrderIndex = 3,
                    },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "I'm fine",
                Instructions = "Complete: _______ bene, grazie! (I'm fine, thank you!)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Sto bene' means 'I'm fine'. 'Sto' is from the verb 'stare'.",
                Text = "_______ bene, grazie!",
                CorrectAnswer = "Sto",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
            },
            new TranslationExercise
            {
                LessonId = lessonId,
                Title = "Translate the question",
                Instructions = "Translate 'Where are you from?' to Italian.",
                EstimatedDurationMinutes = 7,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Di dove sei?' (informal) or 'Di dove e?' (formal).",
                SourceText = "Where are you from?",
                TargetText = "Di dove sei?",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.75,
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Listen to the response",
                Instructions = "Listen and type the response you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 3,
                Explanation = "'Sto bene, grazie' is the standard response to 'Come stai?'",
                AudioUrl = AudioPlaceholder,
                CorrectAnswer = "Sto bene, grazie",
                AcceptedAnswers = "sto bene",
                CaseSensitive = false,
                MaxReplays = 3,
            },
        ];
}
