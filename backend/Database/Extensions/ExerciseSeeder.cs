using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

public static class ExerciseSeeder
{
    private const string AudioPlaceholder = "/static/uploads/audio/placeholder.mp3";

    public static async Task SeedAsync(BackendDbContext context, List<string> lessonIds, string createdById)
    {
        if (await context.Exercises.AnyAsync(e => lessonIds.Contains(e.LessonId)))
            return;

        var exercises = new List<Exercise>();

        for (int i = 0; i < lessonIds.Count; i++)
        {
            var lessonExercises = BuildExercisesForLesson(lessonIds[i], i, createdById);
            exercises.AddRange(lessonExercises);
        }

        context.Exercises.AddRange(exercises);
        await context.SaveChangesAsync();
    }

    private static List<Exercise> BuildExercisesForLesson(string lessonId, int lessonIndex, string createdById) =>
        lessonIndex switch
        {
            0 => BuildGreetingsExercises(lessonId, createdById),
            1 => BuildNumbersExercises(lessonId, createdById),
            2 => BuildColorsExercises(lessonId, createdById),
            3 => BuildFoodExercises(lessonId, createdById),
            4 => BuildTravelExercises(lessonId, createdById),
            5 => BuildVerbsExercises(lessonId, createdById),
            6 => BuildTimeExercises(lessonId, createdById),
            7 => BuildConversationExercises(lessonId, createdById),
            _ => [],
        };

    // ══════════════════════════════════════════════════════════════════
    // Lesson 0: Greetings and Introductions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildGreetingsExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "What does 'Ciao' mean? Select the correct English translation.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Hello / Goodbye", IsCorrect = true, Explanation = "'Ciao' is an informal greeting used for both hello and goodbye in Italian." },
                    new ExerciseOption { OptionText = "Thank you", IsCorrect = false, Explanation = "'Thank you' in Italian is 'Grazie'." },
                    new ExerciseOption { OptionText = "Good evening", IsCorrect = false, Explanation = "'Good evening' in Italian is 'Buonasera'." },
                    new ExerciseOption { OptionText = "Please", IsCorrect = false, Explanation = "'Please' in Italian is 'Per favore'." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Fill in the blank: Mi ____ Marco. (My name is Marco.)",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Mi ____ Marco.",
                Options =
                [
                    new ExerciseOption { OptionText = "chiamo", IsCorrect = true, Explanation = "'Mi chiamo' means 'My name is' or literally 'I call myself'." },
                    new ExerciseOption { OptionText = "sono", IsCorrect = false, Explanation = "'Sono' means 'I am' but is not used for introducing names." },
                    new ExerciseOption { OptionText = "ho", IsCorrect = false, Explanation = "'Ho' means 'I have'." },
                    new ExerciseOption { OptionText = "parlo", IsCorrect = false, Explanation = "'Parlo' means 'I speak'." },
                    new ExerciseOption { OptionText = "vengo", IsCorrect = false, Explanation = "'Vengo' means 'I come'." },
                ],
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Listen and select the greeting you hear.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Buongiorno", IsCorrect = true, Explanation = "'Buongiorno' is used from morning until early afternoon." },
                    new ExerciseOption { OptionText = "Buonasera", IsCorrect = false, Explanation = "'Buonasera' is used in the evening." },
                    new ExerciseOption { OptionText = "Buonanotte", IsCorrect = false, Explanation = "'Buonanotte' means 'Good night' and is used before sleeping." },
                    new ExerciseOption { OptionText = "Ciao", IsCorrect = false, Explanation = "'Ciao' is an informal greeting." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 1: Numbers 1 to 20
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildNumbersExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Which number is 'cinque'? Select the correct number.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "3", IsCorrect = false, Explanation = "'Tre' is the Italian word for 3." },
                    new ExerciseOption { OptionText = "5", IsCorrect = true, Explanation = "'Cinque' is the Italian word for 5." },
                    new ExerciseOption { OptionText = "7", IsCorrect = false, Explanation = "'Sette' is the Italian word for 7." },
                    new ExerciseOption { OptionText = "9", IsCorrect = false, Explanation = "'Nove' is the Italian word for 9." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "The number 10 in Italian is ____.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "The number 10 in Italian is ____.",
                Options =
                [
                    new ExerciseOption { OptionText = "dieci", IsCorrect = true, Explanation = "'Dieci' is 10 in Italian." },
                    new ExerciseOption { OptionText = "sette", IsCorrect = false, Explanation = "'Sette' is 7 in Italian." },
                    new ExerciseOption { OptionText = "otto", IsCorrect = false, Explanation = "'Otto' is 8 in Italian." },
                    new ExerciseOption { OptionText = "nove", IsCorrect = false, Explanation = "'Nove' is 9 in Italian." },
                    new ExerciseOption { OptionText = "undici", IsCorrect = false, Explanation = "'Undici' is 11 in Italian." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 2: Colors and Descriptions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildColorsExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "What color is 'verde'? Pick the correct color.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Red", IsCorrect = false, Explanation = "'Rosso' means red in Italian." },
                    new ExerciseOption { OptionText = "Blue", IsCorrect = false, Explanation = "'Blu' means blue in Italian." },
                    new ExerciseOption { OptionText = "Green", IsCorrect = true, Explanation = "'Verde' means green in Italian." },
                    new ExerciseOption { OptionText = "Yellow", IsCorrect = false, Explanation = "'Giallo' means yellow in Italian." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "The Italian word for black is ____.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "The Italian word for black is ____.",
                Options =
                [
                    new ExerciseOption { OptionText = "nero", IsCorrect = true, Explanation = "'Nero' is black in Italian (masculine form)." },
                    new ExerciseOption { OptionText = "nera", IsCorrect = true, Explanation = "'Nera' is black in Italian (feminine form)." },
                    new ExerciseOption { OptionText = "bianco", IsCorrect = false, Explanation = "'Bianco' means white in Italian." },
                    new ExerciseOption { OptionText = "rosso", IsCorrect = false, Explanation = "'Rosso' means red in Italian." },
                    new ExerciseOption { OptionText = "blu", IsCorrect = false, Explanation = "'Blu' means blue in Italian." },
                    new ExerciseOption { OptionText = "giallo", IsCorrect = false, Explanation = "'Giallo' means yellow in Italian." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 3: Food and Ordering
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildFoodExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "What does 'il pane' mean? Select the correct translation.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Cheese", IsCorrect = false, Explanation = "'Formaggio' is Italian for cheese." },
                    new ExerciseOption { OptionText = "Bread", IsCorrect = true, Explanation = "'Il pane' is Italian for bread." },
                    new ExerciseOption { OptionText = "Coffee", IsCorrect = false, Explanation = "'Caffe' is Italian for coffee." },
                    new ExerciseOption { OptionText = "Water", IsCorrect = false, Explanation = "'Acqua' is Italian for water." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Complete: Un caffe, ____. (A coffee, please.)",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Un caffe, ____.",
                Options =
                [
                    new ExerciseOption { OptionText = "per favore", IsCorrect = true, Explanation = "'Per favore' means 'please' and is essential for polite ordering." },
                    new ExerciseOption { OptionText = "perfavore", IsCorrect = true, Explanation = "Alternative spelling of 'per favore' (please)." },
                    new ExerciseOption { OptionText = "grazie", IsCorrect = false, Explanation = "'Grazie' means 'thank you', not 'please'." },
                    new ExerciseOption { OptionText = "prego", IsCorrect = false, Explanation = "'Prego' means 'you're welcome' or 'go ahead'." },
                    new ExerciseOption { OptionText = "scusi", IsCorrect = false, Explanation = "'Scusi' means 'excuse me'." },
                    new ExerciseOption { OptionText = "arrivederci", IsCorrect = false, Explanation = "'Arrivederci' means 'goodbye'." },
                ],
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Listen and select the food word you hear.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "formaggio", IsCorrect = true, Explanation = "The word spoken is 'formaggio' (cheese)." },
                    new ExerciseOption { OptionText = "pane", IsCorrect = false, Explanation = "'Pane' means bread." },
                    new ExerciseOption { OptionText = "acqua", IsCorrect = false, Explanation = "'Acqua' means water." },
                    new ExerciseOption { OptionText = "caffe", IsCorrect = false, Explanation = "'Caffe' means coffee." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 4: Travel and Directions
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTravelExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "What does 'destra' mean? Choose the correct direction.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Left", IsCorrect = false, Explanation = "'Sinistra' means left in Italian." },
                    new ExerciseOption { OptionText = "Right", IsCorrect = true, Explanation = "'Destra' means right. 'Sinistra' means left." },
                    new ExerciseOption { OptionText = "Straight", IsCorrect = false, Explanation = "'Dritto' means straight in Italian." },
                    new ExerciseOption { OptionText = "Behind", IsCorrect = false, Explanation = "'Dietro' means behind in Italian." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Complete: La stazione e a ____. (The station is on the left.)",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "La stazione e a ____.",
                Options =
                [
                    new ExerciseOption { OptionText = "sinistra", IsCorrect = true, Explanation = "'A sinistra' means 'on the left'." },
                    new ExerciseOption { OptionText = "destra", IsCorrect = false, Explanation = "'A destra' means 'on the right'." },
                    new ExerciseOption { OptionText = "dritto", IsCorrect = false, Explanation = "'Dritto' means 'straight'." },
                    new ExerciseOption { OptionText = "dietro", IsCorrect = false, Explanation = "'Dietro' means 'behind'." },
                    new ExerciseOption { OptionText = "davanti", IsCorrect = false, Explanation = "'Davanti' means 'in front'." },
                ],
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Listen and select what you hear.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Arrivederci", IsCorrect = true, Explanation = "'Arrivederci' is the formal goodbye." },
                    new ExerciseOption { OptionText = "Ciao", IsCorrect = false, Explanation = "'Ciao' is an informal greeting/goodbye." },
                    new ExerciseOption { OptionText = "Buonasera", IsCorrect = false, Explanation = "'Buonasera' means 'Good evening'." },
                    new ExerciseOption { OptionText = "Prego", IsCorrect = false, Explanation = "'Prego' means 'you're welcome' or 'go ahead'." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 5: Present Tense Verbs
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildVerbsExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "'Io parlo' - which pronoun does 'io' refer to?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "You", IsCorrect = false, Explanation = "'Tu' is the pronoun for 'you' (informal)." },
                    new ExerciseOption { OptionText = "I", IsCorrect = true, Explanation = "'Io' is the first-person singular pronoun: I." },
                    new ExerciseOption { OptionText = "We", IsCorrect = false, Explanation = "'Noi' is the pronoun for 'we'." },
                    new ExerciseOption { OptionText = "They", IsCorrect = false, Explanation = "'Loro' is the pronoun for 'they'." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Complete: Noi parl____. (We speak.)",
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                Text = "Noi parl____.",
                Options =
                [
                    new ExerciseOption { OptionText = "iamo", IsCorrect = true, Explanation = "The 'noi' form of 'parlare' is 'parliamo'." },
                    new ExerciseOption { OptionText = "parliamo", IsCorrect = true, Explanation = "The full conjugation for 'we speak' is 'parliamo'." },
                    new ExerciseOption { OptionText = "o", IsCorrect = false, Explanation = "'O' is the ending for 'io' (I), not 'noi' (we)." },
                    new ExerciseOption { OptionText = "i", IsCorrect = false, Explanation = "'I' is the ending for 'tu' (you), not 'noi' (we)." },
                    new ExerciseOption { OptionText = "ano", IsCorrect = false, Explanation = "'Ano' is the ending for 'loro' (they), not 'noi' (we)." },
                    new ExerciseOption { OptionText = "ete", IsCorrect = false, Explanation = "'Ete' is the ending for 'voi' (you plural), not 'noi' (we)." },
                ],
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Listen and select the verb form you hear.",
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "parlano", IsCorrect = true, Explanation = "The spoken word is 'parlano' (they speak)." },
                    new ExerciseOption { OptionText = "parliamo", IsCorrect = false, Explanation = "'Parliamo' means 'we speak'." },
                    new ExerciseOption { OptionText = "parli", IsCorrect = false, Explanation = "'Parli' means 'you speak' (informal)." },
                    new ExerciseOption { OptionText = "parlo", IsCorrect = false, Explanation = "'Parlo' means 'I speak'." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 6: Days and Time
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildTimeExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Which day is 'venerdi'? Select the day of the week.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Wednesday", IsCorrect = false, Explanation = "'Mercoledi' is Wednesday in Italian." },
                    new ExerciseOption { OptionText = "Thursday", IsCorrect = false, Explanation = "'Giovedi' is Thursday in Italian." },
                    new ExerciseOption { OptionText = "Friday", IsCorrect = true, Explanation = "'Venerdi' is Friday." },
                    new ExerciseOption { OptionText = "Saturday", IsCorrect = false, Explanation = "'Sabato' is Saturday in Italian." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Sunday in Italian is ____.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Sunday in Italian is ____.",
                Options =
                [
                    new ExerciseOption { OptionText = "domenica", IsCorrect = true, Explanation = "'Domenica' is Sunday - the only day that's feminine!" },
                    new ExerciseOption { OptionText = "lunedi", IsCorrect = false, Explanation = "'Lunedi' is Monday in Italian." },
                    new ExerciseOption { OptionText = "martedi", IsCorrect = false, Explanation = "'Martedi' is Tuesday in Italian." },
                    new ExerciseOption { OptionText = "sabato", IsCorrect = false, Explanation = "'Sabato' is Saturday in Italian." },
                    new ExerciseOption { OptionText = "venerdi", IsCorrect = false, Explanation = "'Venerdi' is Friday in Italian." },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Lesson 7: First Conversations
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildConversationExercises(string lessonId, string createdById) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "What does 'Dove' mean? Select the correct question word.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "What", IsCorrect = false, Explanation = "'Cosa' or 'Che cosa' means 'What' in Italian." },
                    new ExerciseOption { OptionText = "When", IsCorrect = false, Explanation = "'Quando' means 'When' in Italian." },
                    new ExerciseOption { OptionText = "Where", IsCorrect = true, Explanation = "'Dove' means 'Where' in Italian." },
                    new ExerciseOption { OptionText = "Why", IsCorrect = false, Explanation = "'Perche' means 'Why' in Italian." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Complete: ____ bene, grazie! (I'm fine, thank you!)",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "____ bene, grazie!",
                Options =
                [
                    new ExerciseOption { OptionText = "Sto", IsCorrect = true, Explanation = "'Sto bene' means 'I'm fine'. 'Sto' is from the verb 'stare'." },
                    new ExerciseOption { OptionText = "Sono", IsCorrect = false, Explanation = "'Sono' means 'I am' but 'stare' is used for temporary states like feeling well." },
                    new ExerciseOption { OptionText = "Ho", IsCorrect = false, Explanation = "'Ho' means 'I have'." },
                    new ExerciseOption { OptionText = "Vado", IsCorrect = false, Explanation = "'Vado' means 'I go'." },
                    new ExerciseOption { OptionText = "Vengo", IsCorrect = false, Explanation = "'Vengo' means 'I come'." },
                ],
            },
            new ListeningExercise
            {
                LessonId = lessonId,
                CreatedById = createdById,
                Instructions = "Listen and select what you hear.",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                AudioUrl = AudioPlaceholder,
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Sto bene, grazie", IsCorrect = true, Explanation = "'Sto bene, grazie' is the standard response to 'Come stai?'" },
                    new ExerciseOption { OptionText = "Mi chiamo Marco", IsCorrect = false, Explanation = "'Mi chiamo Marco' means 'My name is Marco'." },
                    new ExerciseOption { OptionText = "Sono di Roma", IsCorrect = false, Explanation = "'Sono di Roma' means 'I'm from Rome'." },
                    new ExerciseOption { OptionText = "Non capisco", IsCorrect = false, Explanation = "'Non capisco' means 'I don't understand'." },
                ],
            },
        ];
}
