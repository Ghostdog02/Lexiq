using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds 8 lessons into the Italian Beginners course.
/// Returns an ordered list of lesson IDs for use by ExerciseSeeder.
/// </summary>
public static class LessonSeeder
{
    public static async Task<List<string>> SeedAsync(BackendDbContext context, string courseId)
    {
        var existingLessons = await context
            .Lessons.Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();

        if (existingLessons.Count > 0)
        {
            return existingLessons.Select(l => l.Id).ToList();
        }

        var definitions = GetLessonDefinitions();
        var lessonIds = new List<string>();

        for (int i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = def.Title,
                Description = def.Description,
                EstimatedDurationMinutes = def.DurationMinutes,
                OrderIndex = i,
                LessonContent = def.EditorContent,
                IsLocked = i != 0, // Only the first lesson is unlocked
                CreatedAt = DateTime.UtcNow,
            };

            context.Lessons.Add(lesson);
            lessonIds.Add(lesson.Id);
        }

        await context.SaveChangesAsync();
        return lessonIds;
    }

    private static List<LessonDefinition> GetLessonDefinitions() =>
        [
            new(
                "Greetings and Introductions",
                "Learn basic Italian greetings, how to introduce yourself, and common pleasantries for everyday conversations.",
                15,
                BuildEditorJson(
                    "Buongiorno! Welcome to Italian",
                    """
                    In this lesson you will learn the most common Italian greetings:

                    <b>Ciao</b> - Hello / Goodbye (informal)
                    <b>Buongiorno</b> - Good morning / Good day
                    <b>Buonasera</b> - Good evening
                    <b>Buonanotte</b> - Good night
                    <b>Arrivederci</b> - Goodbye (formal)

                    You'll also learn how to introduce yourself:
                    <b>Mi chiamo...</b> - My name is...
                    <b>Sono...</b> - I am...
                    <b>Piacere!</b> - Nice to meet you!
                    <b>Come stai?</b> - How are you? (informal)
                    <b>Come sta?</b> - How are you? (formal)
                    """
                )
            ),
            new(
                "Numbers 1 to 20",
                "Master counting in Italian from uno to venti and use numbers in simple everyday contexts.",
                20,
                BuildEditorJson(
                    "I Numeri - Counting in Italian",
                    """
                    Numbers are essential for everyday life. Let's learn 1-20:

                    <b>1-10:</b> uno, due, tre, quattro, cinque, sei, sette, otto, nove, dieci

                    <b>11-20:</b> undici, dodici, tredici, quattordici, quindici, sedici, diciassette, diciotto, diciannove, venti

                    Notice the pattern for 11-16: they end in "-dici".
                    For 17-19: the structure is "dici" + unit (diciassette = dici + sette).

                    Practice: <i>Ho due fratelli</i> (I have two brothers)
                    """
                )
            ),
            new(
                "Colors and Descriptions",
                "Describe objects using Italian color words and learn basic adjective agreement.",
                18,
                BuildEditorJson(
                    "I Colori - Colors in Italian",
                    """
                    Colors help you describe the world around you:

                    <b>rosso/rossa</b> - red
                    <b>blu</b> - blue (invariable)
                    <b>verde</b> - green
                    <b>giallo/gialla</b> - yellow
                    <b>nero/nera</b> - black
                    <b>bianco/bianca</b> - white
                    <b>arancione</b> - orange
                    <b>azzurro/azzurra</b> - light blue

                    <i>Important:</i> Most adjectives change endings to match the noun's gender!
                    <b>Il libro rosso</b> (the red book - masculine)
                    <b>La macchina rossa</b> (the red car - feminine)
                    """
                )
            ),
            new(
                "Food and Ordering",
                "Navigate an Italian cafe or restaurant - order food and drinks with confidence.",
                22,
                BuildEditorJson(
                    "Mangiare e Bere - Food and Drink",
                    """
                    Italian food culture is central to the language. Essential words:

                    <b>il pane</b> - bread
                    <b>il formaggio</b> - cheese
                    <b>la pasta</b> - pasta
                    <b>la pizza</b> - pizza
                    <b>il caffe</b> - coffee
                    <b>l'acqua</b> - water
                    <b>il vino</b> - wine
                    <b>il gelato</b> - ice cream

                    <b>Useful phrases:</b>
                    <i>Vorrei...</i> - I would like...
                    <i>Un caffe, per favore</i> - A coffee, please
                    <i>Il conto, per favore</i> - The bill, please
                    <i>Grazie mille!</i> - Thank you very much!
                    """
                )
            ),
            new(
                "Travel and Directions",
                "Ask for directions and find your way around an Italian city.",
                20,
                BuildEditorJson(
                    "Viaggiare - Getting Around",
                    """
                    Finding your way in Italy:

                    <b>Directions:</b>
                    <b>a destra</b> - to the right
                    <b>a sinistra</b> - to the left
                    <b>dritto / diritto</b> - straight ahead
                    <b>qui / qua</b> - here
                    <b>la / li</b> - there

                    <b>Places:</b>
                    <b>la stazione</b> - the station
                    <b>l'hotel / l'albergo</b> - the hotel
                    <b>il ristorante</b> - the restaurant
                    <b>il museo</b> - the museum

                    <b>Key question:</b> <i>Dov'e...?</i> - Where is...?
                    Example: <i>Dov'e la stazione?</i> - Where is the station?
                    """
                )
            ),
            new(
                "Present Tense Verbs",
                "Conjugate the most common Italian verbs in the present tense.",
                25,
                BuildEditorJson(
                    "I Verbi - Present Tense",
                    """
                    Italian verbs change based on who is doing the action. Let's learn <b>parlare</b> (to speak):

                    <b>io parlo</b> - I speak
                    <b>tu parli</b> - you speak (informal)
                    <b>lui/lei parla</b> - he/she speaks
                    <b>noi parliamo</b> - we speak
                    <b>voi parlate</b> - you speak (plural)
                    <b>loro parlano</b> - they speak

                    Regular -ARE verbs follow this pattern!
                    Try: <b>mangiare</b> (to eat), <b>guardare</b> (to watch), <b>ascoltare</b> (to listen)

                    Example: <i>Io parlo italiano.</i> - I speak Italian.
                    """
                )
            ),
            new(
                "Days and Time",
                "Tell time, name the days of the week, and discuss daily schedules.",
                18,
                BuildEditorJson(
                    "Il Tempo - Days and Time",
                    """
                    <b>Days of the week (i giorni della settimana):</b>
                    lunedi, martedi, mercoledi, giovedi, venerdi, sabato, domenica

                    <b>Telling time:</b>
                    <i>Che ore sono?</i> - What time is it?
                    <i>Sono le tre.</i> - It's three o'clock.
                    <i>E l'una.</i> - It's one o'clock.
                    <i>Sono le dieci e mezza.</i> - It's 10:30.
                    <i>Sono le due e un quarto.</i> - It's 2:15.

                    <b>Time expressions:</b>
                    <b>oggi</b> - today
                    <b>domani</b> - tomorrow
                    <b>ieri</b> - yesterday
                    """
                )
            ),
            new(
                "First Conversations",
                "Put everything together in guided conversation practice - your first real Italian dialogue!",
                25,
                BuildEditorJson(
                    "La Conversazione - Putting It Together",
                    """
                    Let's practice a complete conversation:

                    <b>A:</b> Buongiorno! Come sta?
                    <b>B:</b> Sto bene, grazie. E Lei?
                    <b>A:</b> Molto bene! Mi chiamo Marco. E Lei?
                    <b>B:</b> Piacere, mi chiamo Sofia.
                    <b>A:</b> Piacere mio! Di dove e?
                    <b>B:</b> Sono di Roma. E Lei?
                    <b>A:</b> Sono di Milano.

                    <b>Key phrases:</b>
                    <i>Sto bene</i> - I'm fine
                    <i>Di dove e?</i> - Where are you from?
                    <i>Sono di...</i> - I'm from...
                    <i>Piacere mio!</i> - The pleasure is mine!
                    """
                )
            ),
        ];

    private static string BuildEditorJson(string heading, string content)
    {
        var escapedHeading = EscapeForJson(heading);
        var escapedContent = EscapeForJson(content.Trim());

        return $$"""
            {
                "time": {{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}},
                "blocks": [
                    {
                        "id": "{{Guid.NewGuid().ToString("N")[..8]}}",
                        "type": "header",
                        "data": { "text": "{{escapedHeading}}", "level": 2 }
                    },
                    {
                        "id": "{{Guid.NewGuid().ToString("N")[..8]}}",
                        "type": "paragraph",
                        "data": { "text": "{{escapedContent}}" }
                    }
                ],
                "version": "2.28.0"
            }
            """;
    }

    private static string EscapeForJson(string text) =>
        text.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "")
            .Replace("\t", "\\t");

    private record LessonDefinition(
        string Title,
        string Description,
        int DurationMinutes,
        string EditorContent
    );
}
