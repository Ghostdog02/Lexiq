using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

public static class ExerciseSeeder
{
    private const string AudioPlaceholder = "/static/uploads/audio/placeholder.mp3";

    public static async Task SeedAsync(BackendDbContext context, List<string> lessonIds)
    {
        if (await context.Exercises.AnyAsync(e => lessonIds.Contains(e.LessonId)))
            return;

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
                Title = "What does 'Ciao' mean?",
                Question = "Select the correct English translation.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Ciao' is an informal greeting used for both hello and goodbye in Italian.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Hello / Goodbye", IsCorrect = true, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Thank you", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Good evening", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Please", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Complete the sentence",
                Question = "Fill in the blank: Mi ____ Marco. (My name is Marco.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Mi chiamo' means 'My name is' or literally 'I call myself'.",
                Text = "Mi ____ Marco.",
                CorrectAnswer = "chiamo",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "chiamo,sono,ho,parlo,vengo",
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Hear the greeting",
                Question = "Listen and select the greeting you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Buongiorno' is used from morning until early afternoon.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Buongiorno", IsCorrect = true, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Buonasera", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Buonanotte", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Ciao", IsCorrect = false, OrderIndex = 3 },
                ],
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
                Title = "Which number is 'cinque'?",
                Question = "Select the correct number.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Cinque' is the Italian word for 5.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "3", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "5", IsCorrect = true, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "7", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "9", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Write the number",
                Question = "The number 10 in Italian is ____.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Dieci' is 10 in Italian.",
                Text = "The number 10 in Italian is ____.",
                CorrectAnswer = "dieci",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "dieci,sette,otto,nove,undici",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 2: Colors and Descriptions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildColorsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "What color is 'verde'?",
                Question = "Pick the correct color.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Verde' means green in Italian.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Red", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Blue", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Green", IsCorrect = true, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Yellow", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Black in Italian",
                Question = "The Italian word for black is ____.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Nero' is black in Italian (masculine form).",
                Text = "The Italian word for black is ____.",
                CorrectAnswer = "nero",
                AcceptedAnswers = "nera",
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "nero,bianco,rosso,blu,giallo",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 3: Food and Ordering
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildFoodExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "What does 'il pane' mean?",
                Question = "Select the correct translation.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Il pane' is Italian for bread.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Cheese", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Bread", IsCorrect = true, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Coffee", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Water", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Order politely",
                Question = "Complete: Un caffe, ____. (A coffee, please.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Per favore' means 'please' and is essential for polite ordering.",
                Text = "Un caffe, ____.",
                CorrectAnswer = "per favore",
                AcceptedAnswers = "perfavore",
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "per favore,grazie,prego,scusi,arrivederci",
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Hear the food word",
                Question = "Listen and select the food word you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "The word spoken is 'formaggio' (cheese).",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "formaggio", IsCorrect = true, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "pane", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "acqua", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "caffe", IsCorrect = false, OrderIndex = 3 },
                ],
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
                Title = "What does 'destra' mean?",
                Question = "Choose the correct direction.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Destra' means right. 'Sinistra' means left.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Left", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Right", IsCorrect = true, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Straight", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Behind", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Give directions",
                Question = "Complete: La stazione e a ____. (The station is on the left.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'A sinistra' means 'on the left'.",
                Text = "La stazione e a ____.",
                CorrectAnswer = "sinistra",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "sinistra,destra,dritto,dietro,davanti",
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Listen for the farewell",
                Question = "Listen and select what you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Arrivederci' is the formal goodbye.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Arrivederci", IsCorrect = true, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Ciao", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Buonasera", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Prego", IsCorrect = false, OrderIndex = 3 },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 5: Present Tense Verbs
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildVerbsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "'Io parlo' - who is speaking?",
                Question = "Which pronoun does 'io' refer to?",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Io' is the first-person singular pronoun: I.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "You", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "I", IsCorrect = true, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "We", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "They", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Conjugate for 'noi'",
                Question = "Complete: Noi parl____. (We speak.)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                OrderIndex = 1,
                Explanation = "The 'noi' form of 'parlare' is 'parliamo'.",
                Text = "Noi parl____.",
                CorrectAnswer = "iamo",
                AcceptedAnswers = "parliamo",
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "iamo,o,i,ano,ete",
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Hear the conjugation",
                Question = "Listen and select the verb form you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                OrderIndex = 2,
                Explanation = "The spoken word is 'parlano' (they speak).",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "parlano", IsCorrect = true, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "parliamo", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "parli", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "parlo", IsCorrect = false, OrderIndex = 3 },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 6: Days and Time
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTimeExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Which day is 'venerdi'?",
                Question = "Select the day of the week.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Venerdi' is Friday.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Wednesday", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Thursday", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Friday", IsCorrect = true, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Saturday", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "Sunday in Italian",
                Question = "Sunday in Italian is ____.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Domenica' is Sunday - the only day that's feminine!",
                Text = "Sunday in Italian is ____.",
                CorrectAnswer = "domenica",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "domenica,lunedi,martedi,sabato,venerdi",
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 7: First Conversations
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildConversationExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "What does 'Dove' mean?",
                Question = "Select the correct question word.",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false,
                Explanation = "'Dove' means 'Where' in Italian.",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "What", IsCorrect = false, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "When", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Where", IsCorrect = true, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Why", IsCorrect = false, OrderIndex = 3 },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = "I'm fine",
                Question = "Complete: ____ bene, grazie! (I'm fine, thank you!)",
                EstimatedDurationMinutes = 5,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                Explanation = "'Sto bene' means 'I'm fine'. 'Sto' is from the verb 'stare'.",
                Text = "____ bene, grazie!",
                CorrectAnswer = "Sto",
                AcceptedAnswers = null,
                CaseSensitive = false,
                TrimWhitespace = true,
                WordBank = "Sto,Sono,Ho,Vado,Vengo",
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                Title = "Listen to the response",
                Question = "Listen and select what you hear.",
                EstimatedDurationMinutes = 8,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                OrderIndex = 2,
                Explanation = "'Sto bene, grazie' is the standard response to 'Come stai?'",
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Sto bene, grazie", IsCorrect = true, OrderIndex = 0 },
                    new ExerciseOption { OptionText = "Mi chiamo Marco", IsCorrect = false, OrderIndex = 1 },
                    new ExerciseOption { OptionText = "Sono di Roma", IsCorrect = false, OrderIndex = 2 },
                    new ExerciseOption { OptionText = "Non capisco", IsCorrect = false, OrderIndex = 3 },
                ],
            },
        ];
}
