using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds exercises for all lessons in the Italian Beginners course.
/// Each lesson gets 3-4 exercises covering different exercise types.
/// </summary>
public static class ExerciseSeeder
{
    private static string Audio(string filename) => $"/static/uploads/audio/{filename}";

    public static async Task SeedAsync(
        BackendDbContext context,
        List<string> lessonIds,
        int courseIndex = 0
    )
    {
        // Idempotency: if exercises exist for any lesson, assume full seed completed
        if (await context.Exercises.AnyAsync(e => lessonIds.Contains(e.LessonId)))
        {
            return;
        }

        var exercises = new List<Exercise>();

        for (int i = 0; i < lessonIds.Count; i++)
        {
            var lessonExercises = BuildExercisesForLesson(lessonIds[i], i, courseIndex);
            exercises.AddRange(lessonExercises);
        }

        context.Exercises.AddRange(exercises);
        await context.SaveChangesAsync();
    }

    private static List<Exercise> BuildExercisesForLesson(
        string lessonId,
        int lessonIndex,
        int courseIndex
    ) =>
        courseIndex switch
        {
            0 => lessonIndex switch
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
            },
            1 => BuildConversationsExercises(lessonId, lessonIndex),
            2 => BuildGrammarExercises(lessonId, lessonIndex),
            3 => BuildCultureExercises(lessonId, lessonIndex),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_ciao.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Buonasera' is used from late afternoon onwards.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "'Buonasera' (good evening) is typically used from around 4-5 PM until late night.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
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
                Instructions = "What number is this?",

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_numbers.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "The Italian word for 'ten' is 'dieci'.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "'Dieci' is indeed the correct Italian word for ten.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_colors.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Nero' means white in Italian.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = false, Explanation = "" },
                    new ExerciseOption
                    {
                        OptionText = "False",
                        IsCorrect = true,
                        Explanation = "'Nero' means black. 'Bianco' means white.",
                    },
                ],
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_food.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Pizza' and 'pasta' are Italian words used worldwide.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "These are indeed Italian words that have been adopted into many languages globally.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
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
                Instructions = "What does 'stazione' mean?",

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_travel.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Sinistra' means left and 'destra' means right.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "These are the correct Italian words for left and right directions.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_verbs.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "Italian verbs are conjugated based on the subject pronoun.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "Yes, Italian verbs change their ending based on who is performing the action.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_time.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "The Italian week starts with Monday (lunedì).",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "In Italy, the week traditionally starts on Monday, not Sunday.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Course 1: Everyday Conversations
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildConversationsExercises(string lessonId, int lessonIndex) =>
        lessonIndex switch
        {
            0 => BuildRestaurantExercises(lessonId),
            1 => BuildShoppingExercises(lessonId),
            2 => BuildGettingAroundExercises(lessonId),
            3 => BuildMeetingPeopleExercises(lessonId),
            _ => [],
        };

    private static List<Exercise> BuildRestaurantExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Vorrei una pizza' mean?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("conv_restaurant.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "I would like a pizza", IsCorrect = true, Explanation = "'Vorrei' is the conditional of 'volere' (to want), used politely to order food." },
                    new ExerciseOption { OptionText = "I have a pizza", IsCorrect = false, Explanation = "'Ho una pizza' means I have a pizza." },
                    new ExerciseOption { OptionText = "Where is the pizza?", IsCorrect = false, Explanation = "'Dov'è la pizza?' means where is the pizza?" },
                    new ExerciseOption { OptionText = "Pizza please", IsCorrect = false, Explanation = "A simpler way would be 'Una pizza, per favore'." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Il conto, per ____, per favore. (The bill, please.)",
                Options =
                [
                    new ExerciseOption { OptionText = "favore", IsCorrect = true, Explanation = "'Per favore' means please — a polite way to make any request." },
                    new ExerciseOption { OptionText = "piacere", IsCorrect = false, Explanation = "'Piacere' means nice to meet you or pleasure, not please in this context." },
                    new ExerciseOption { OptionText = "mangiare", IsCorrect = false, Explanation = "'Mangiare' means to eat, which does not fit here." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Buon appetito' is said before eating.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "It's a wish for a good meal, said when food is served." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildShoppingExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Quanto costa?' mean?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("conv_shopping.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "How much does it cost?", IsCorrect = true, Explanation = "'Quanto costa?' is the standard Italian question for asking the price of something." },
                    new ExerciseOption { OptionText = "Do you have this?", IsCorrect = false, Explanation = "'Ce l'ha?' or 'Avete questo?' means do you have this?" },
                    new ExerciseOption { OptionText = "Can I pay by card?", IsCorrect = false, Explanation = "'Posso pagare con carta?' means can I pay by card?" },
                    new ExerciseOption { OptionText = "It's expensive", IsCorrect = false, Explanation = "'È caro' means it's expensive." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Mi ____ questa borsa. (I like this bag.)",
                Options =
                [
                    new ExerciseOption { OptionText = "piace", IsCorrect = true, Explanation = "'Mi piace' means I like — it is used with singular nouns." },
                    new ExerciseOption { OptionText = "vuole", IsCorrect = false, Explanation = "'Vuole' means he/she wants, not I like." },
                    new ExerciseOption { OptionText = "ho", IsCorrect = false, Explanation = "'Ho' means I have, which does not fit this context." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Caro' means cheap in Italian.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = false, Explanation = "" },
                    new ExerciseOption { OptionText = "False", IsCorrect = true, Explanation = "'Caro' means expensive. 'Economico' means cheap." },
                ],
            },
        ];

    private static List<Exercise> BuildGettingAroundExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Dov'è la stazione?' mean?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("conv_directions.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Where is the station?", IsCorrect = true, Explanation = "'Dov'è' means where is, and 'la stazione' means the station." },
                    new ExerciseOption { OptionText = "How far is the station?", IsCorrect = false, Explanation = "'Quanto è lontana la stazione?' means how far is the station?" },
                    new ExerciseOption { OptionText = "Is there a station?", IsCorrect = false, Explanation = "'C'è una stazione?' means is there a station?" },
                    new ExerciseOption { OptionText = "The station is here", IsCorrect = false, Explanation = "'La stazione è qui' means the station is here." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Gira a ____, poi vai dritto. (Turn right, then go straight.)",
                Options =
                [
                    new ExerciseOption { OptionText = "destra", IsCorrect = true, Explanation = "'A destra' means to the right in Italian." },
                    new ExerciseOption { OptionText = "sinistra", IsCorrect = false, Explanation = "'A sinistra' means to the left." },
                    new ExerciseOption { OptionText = "nord", IsCorrect = false, Explanation = "'Nord' means north, which is not used for turning directions this way." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Autobus' and 'bus' refer to the same vehicle in Italian.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "Both words are used in Italian — 'autobus' is the standard term, 'bus' is informal." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildMeetingPeopleExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Di dove sei?' mean?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("conv_meeting.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Where are you from?", IsCorrect = true, Explanation = "'Di dove sei?' literally means 'of where are you?' and is used to ask someone's origin." },
                    new ExerciseOption { OptionText = "Where are you going?", IsCorrect = false, Explanation = "'Dove vai?' means where are you going?" },
                    new ExerciseOption { OptionText = "Who are you?", IsCorrect = false, Explanation = "'Chi sei?' means who are you?" },
                    new ExerciseOption { OptionText = "What do you do?", IsCorrect = false, Explanation = "'Che lavoro fai?' means what do you do for work?" },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Sono ____ Milano. (I am from Milan.)",
                Options =
                [
                    new ExerciseOption { OptionText = "di", IsCorrect = true, Explanation = "'Sono di + city' is the standard way to say you are from a place in Italian." },
                    new ExerciseOption { OptionText = "a", IsCorrect = false, Explanation = "'Sono a Milano' means I am in Milan (location), not from Milan." },
                    new ExerciseOption { OptionText = "in", IsCorrect = false, Explanation = "'In' is used with countries and regions, not cities for origin." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Piacere' can be used when meeting someone for the first time.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "'Piacere' means nice to meet you and is the standard greeting when introduced." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Course 2: Grammar Essentials
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildGrammarExercises(string lessonId, int lessonIndex) =>
        lessonIndex switch
        {
            0 => BuildArticlesExercises(lessonId),
            1 => BuildVerbConjugationExercises(lessonId),
            2 => BuildQuestionWordsExercises(lessonId),
            3 => BuildPrepositionsExercises(lessonId),
            _ => [],
        };

    private static List<Exercise> BuildArticlesExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "Which definite article goes with 'libro' (book)?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("gram_articles.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Il", IsCorrect = true, Explanation = "'Libro' is a masculine noun that starts with a regular consonant, so it takes 'il'." },
                    new ExerciseOption { OptionText = "La", IsCorrect = false, Explanation = "'La' is used for feminine singular nouns." },
                    new ExerciseOption { OptionText = "Lo", IsCorrect = false, Explanation = "'Lo' is used for masculine nouns starting with z or s+consonant." },
                    new ExerciseOption { OptionText = "L'", IsCorrect = false, Explanation = "L' is used before nouns starting with a vowel." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "____ ragazza parla italiano. (The girl speaks Italian.)",
                Options =
                [
                    new ExerciseOption { OptionText = "La", IsCorrect = true, Explanation = "'Ragazza' is a feminine noun, so it takes the feminine article 'la'." },
                    new ExerciseOption { OptionText = "Il", IsCorrect = false, Explanation = "'Il' is used for masculine singular nouns." },
                    new ExerciseOption { OptionText = "Lo", IsCorrect = false, Explanation = "'Lo' is used for masculine nouns starting with z or s+consonant." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Lo' is used before nouns starting with 'z' or 's+consonant'.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "For example: lo zaino (backpack), lo studente (student)." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildVerbConjugationExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "How do you say 'We speak' using 'parlare'?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("gram_verbs.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Parliamo", IsCorrect = true, Explanation = "'Noi parliamo' is the first-person plural form of 'parlare'." },
                    new ExerciseOption { OptionText = "Parlano", IsCorrect = false, Explanation = "'Parlano' is the third-person plural form — they speak." },
                    new ExerciseOption { OptionText = "Parlate", IsCorrect = false, Explanation = "'Parlate' is the second-person plural — you (plural) speak." },
                    new ExerciseOption { OptionText = "Parla", IsCorrect = false, Explanation = "'Parla' is the third-person singular — he/she speaks." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Io ____ la pasta ogni giorno. (I eat pasta every day.)",
                Options =
                [
                    new ExerciseOption { OptionText = "mangio", IsCorrect = true, Explanation = "'Mangio' is the first-person singular form of 'mangiare' (to eat)." },
                    new ExerciseOption { OptionText = "mangia", IsCorrect = false, Explanation = "'Mangia' is the third-person singular — he/she eats." },
                    new ExerciseOption { OptionText = "mangiamo", IsCorrect = false, Explanation = "'Mangiamo' is the first-person plural — we eat." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "The -ARE verb ending for 'tu' (you, informal) is '-i'.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "Tu parli, tu mangi, tu guardi — all regular -ARE verbs use '-i' for 'tu'." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildQuestionWordsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'Quando' mean?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("gram_questions.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "When", IsCorrect = true, Explanation = "'Quando' is the Italian word for 'when'." },
                    new ExerciseOption { OptionText = "Where", IsCorrect = false, Explanation = "'Dove' means where." },
                    new ExerciseOption { OptionText = "Why", IsCorrect = false, Explanation = "'Perché' means why." },
                    new ExerciseOption { OptionText = "How", IsCorrect = false, Explanation = "'Come' means how." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "____ ore sono? (What time is it?)",
                Options =
                [
                    new ExerciseOption { OptionText = "Che", IsCorrect = true, Explanation = "'Che ore sono?' is the standard Italian phrase for asking the time." },
                    new ExerciseOption { OptionText = "Quale", IsCorrect = false, Explanation = "'Quale' means which and is not used in this fixed expression." },
                    new ExerciseOption { OptionText = "Quando", IsCorrect = false, Explanation = "'Quando' means when, not what in this context." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Perché' means both 'why' and 'because' depending on context.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "As a question word it means 'why?'; in an answer it means 'because'." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildPrepositionsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "Which preposition is used for cities — 'Vivo ___ Roma'?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("gram_prepositions.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "a", IsCorrect = true, Explanation = "The preposition 'a' is used before city names to indicate location or residence." },
                    new ExerciseOption { OptionText = "in", IsCorrect = false, Explanation = "'In' is used before countries, regions, and large islands, not cities." },
                    new ExerciseOption { OptionText = "di", IsCorrect = false, Explanation = "'Di' expresses origin, not current residence." },
                    new ExerciseOption { OptionText = "da", IsCorrect = false, Explanation = "'Da' expresses origin or duration, not current residence in a city." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Vivo ____ Italia. (I live in Italy.)",
                Options =
                [
                    new ExerciseOption { OptionText = "in", IsCorrect = true, Explanation = "'In' is used before countries and regions to indicate residence or location." },
                    new ExerciseOption { OptionText = "a", IsCorrect = false, Explanation = "'A' is used before cities, not countries." },
                    new ExerciseOption { OptionText = "di", IsCorrect = false, Explanation = "'Di' expresses possession or origin, not location." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Da' can express both origin and duration in Italian.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "Examples: Sono da Roma (I'm from Rome). Studio da tre anni (I've been studying for three years)." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    // ══════════════════════════════════════════════════════════════════
    // Course 3: Culture and Idioms
    // ══════════════════════════════════════════════════════════════════
    private static List<Exercise> BuildCultureExercises(string lessonId, int lessonIndex) =>
        lessonIndex switch
        {
            0 => BuildExpressionsExercises(lessonId),
            1 => BuildFoodCultureExercises(lessonId),
            2 => BuildFamilyExercises(lessonId),
            3 => BuildCelebrationsExercises(lessonId),
            _ => [],
        };

    private static List<Exercise> BuildExpressionsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What is the correct response to 'In bocca al lupo!'?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("cult_expressions.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Crepi!", IsCorrect = true, Explanation = "'Crepi!' (may it die!) is the traditional response to 'In bocca al lupo!' (good luck!)." },
                    new ExerciseOption { OptionText = "Grazie", IsCorrect = false, Explanation = "While 'Grazie' is polite, the traditional idiomatic response is 'Crepi!'." },
                    new ExerciseOption { OptionText = "Prego", IsCorrect = false, Explanation = "'Prego' means you're welcome and is not the correct response here." },
                    new ExerciseOption { OptionText = "Ciao", IsCorrect = false, Explanation = "'Ciao' is a greeting and not the expected reply to this expression." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the expression",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "____! (Wow / Come on!) — used to express surprise or encouragement.",
                Options =
                [
                    new ExerciseOption { OptionText = "Dai", IsCorrect = true, Explanation = "'Dai!' is a versatile Italian exclamation used for encouragement, surprise, or urging someone on." },
                    new ExerciseOption { OptionText = "Boh", IsCorrect = false, Explanation = "'Boh' expresses uncertainty or indifference — similar to 'dunno' in English." },
                    new ExerciseOption { OptionText = "Mah", IsCorrect = false, Explanation = "'Mah' expresses doubt or resignation." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Mamma mia' can express both surprise and admiration.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "It's a versatile exclamation used for positive and negative reactions alike." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildFoodCultureExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "When do Italians traditionally have 'colazione'?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("cult_food.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "In the morning", IsCorrect = true, Explanation = "'Colazione' is breakfast, typically a light meal eaten in the morning." },
                    new ExerciseOption { OptionText = "At noon", IsCorrect = false, Explanation = "The midday meal is 'pranzo' (lunch)." },
                    new ExerciseOption { OptionText = "In the evening", IsCorrect = false, Explanation = "The evening meal is 'cena' (dinner)." },
                    new ExerciseOption { OptionText = "At midnight", IsCorrect = false, Explanation = "Italians do not traditionally eat a meal at midnight." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "L'____ è un momento sociale prima di cena. (Aperitivo is a social moment before dinner.)",
                Options =
                [
                    new ExerciseOption { OptionText = "aperitivo", IsCorrect = true, Explanation = "'Aperitivo' is a pre-dinner tradition involving drinks and light snacks with friends." },
                    new ExerciseOption { OptionText = "antipasto", IsCorrect = false, Explanation = "'Antipasto' is a starter course served at the table, not a social pre-dinner ritual." },
                    new ExerciseOption { OptionText = "dolce", IsCorrect = false, Explanation = "'Dolce' means dessert and comes at the end of a meal." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "It's considered rude to order a cappuccino after lunch in Italy.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "Cappuccino is a morning drink in Italy. After meals, Italians drink espresso." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildFamilyExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What does 'nonno' mean?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("cult_family.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Grandfather", IsCorrect = true, Explanation = "'Nonno' means grandfather. 'Nonna' means grandmother." },
                    new ExerciseOption { OptionText = "Grandmother", IsCorrect = false, Explanation = "'Nonna' means grandmother. 'Nonno' means grandfather." },
                    new ExerciseOption { OptionText = "Uncle", IsCorrect = false, Explanation = "'Zio' means uncle." },
                    new ExerciseOption { OptionText = "Father", IsCorrect = false, Explanation = "'Padre' or 'papà' means father." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the sentence",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Mia ____ si chiama Sofia. (My sister is called Sofia.)",
                Options =
                [
                    new ExerciseOption { OptionText = "sorella", IsCorrect = true, Explanation = "'Sorella' means sister. 'Fratello' means brother." },
                    new ExerciseOption { OptionText = "madre", IsCorrect = false, Explanation = "'Madre' means mother." },
                    new ExerciseOption { OptionText = "nonna", IsCorrect = false, Explanation = "'Nonna' means grandmother." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "'Fidanzato' can mean both boyfriend and fiancé in Italian.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "Context and additional words (like 'ufficiale') usually clarify whether it means boyfriend or fiancé." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];

    private static List<Exercise> BuildCelebrationsExercises(string lessonId) =>
        [
            new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = "What do Italians say on Christmas?",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("cult_celebrations.mp3"),
                MaxReplays = 3,
                Options =
                [
                    new ExerciseOption { OptionText = "Buon Natale!", IsCorrect = true, Explanation = "'Buon Natale!' means Merry Christmas in Italian." },
                    new ExerciseOption { OptionText = "Buona Pasqua!", IsCorrect = false, Explanation = "'Buona Pasqua!' means Happy Easter." },
                    new ExerciseOption { OptionText = "Buon Anno!", IsCorrect = false, Explanation = "'Buon Anno!' means Happy New Year." },
                    new ExerciseOption { OptionText = "Auguri!", IsCorrect = false, Explanation = "'Auguri!' is a general best wishes greeting used for many occasions." },
                ],
            },
            new FillInBlankExercise
            {
                LessonId = lessonId,
                Instructions = "Complete the greeting",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Text = "Buon ____! (Happy Birthday!)",
                Options =
                [
                    new ExerciseOption { OptionText = "compleanno", IsCorrect = true, Explanation = "'Buon compleanno!' is the Italian way to wish someone a happy birthday." },
                    new ExerciseOption { OptionText = "Natale", IsCorrect = false, Explanation = "'Buon Natale!' means Merry Christmas." },
                    new ExerciseOption { OptionText = "anno", IsCorrect = false, Explanation = "'Buon anno!' means Happy New Year." },
                ],
            },
            new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = "True or False",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 15,
                Statement = "Ferragosto is a major Italian holiday celebrated on August 15th.",
                Options =
                [
                    new ExerciseOption { OptionText = "True", IsCorrect = true, Explanation = "Ferragosto marks the height of summer. Most businesses close and Italians go on holiday." },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                AudioUrl = Audio("greetings_conversation.mp3"),
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

                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Statement = "'Arrivederci' is a formal way to say goodbye.",
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "True",
                        IsCorrect = true,
                        Explanation = "'Arrivederci' is more formal than 'ciao' and literally means 'until we see each other again'.",
                    },
                    new ExerciseOption { OptionText = "False", IsCorrect = false, Explanation = "" },
                ],
            },
        ];
}
