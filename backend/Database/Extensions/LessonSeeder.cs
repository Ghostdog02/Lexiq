using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds lessons into the Italian courses.
/// Returns an ordered list of lesson IDs for use by ExerciseSeeder.
/// </summary>
public static class LessonSeeder
{
    public static async Task<List<string>> SeedAsync(BackendDbContext context, string courseId, int courseIndex = 0)
    {
        var existingLessons = await context
            .Lessons.Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();

        if (existingLessons.Count > 0)
        {
            return existingLessons.Select(l => l.LessonId).ToList();
        }

        var definitions = GetLessonDefinitions(courseIndex);
        var lessonIds = new List<string>();

        for (int i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = def.Title,
                EstimatedDurationMinutes = def.DurationMinutes,
                OrderIndex = i,
                LessonContent = def.EditorContent,
                IsLocked = courseIndex != 0 || i != 0, // Only lesson 0 of course 0 is unlocked initially
                CreatedAt = DateTime.UtcNow,
            };

            context.Lessons.Add(lesson);
            lessonIds.Add(lesson.LessonId);
        }

        await context.SaveChangesAsync();
        return lessonIds;
    }

    private static List<LessonDefinition> GetLessonDefinitions(int courseIndex) => courseIndex switch
    {
        0 => GetBeginnersLessons(),
        1 => GetEverydayConversationsLessons(),
        2 => GetGrammarEssentialsLessons(),
        3 => GetCultureAndIdiomsLessons(),
        _ => [],
    };

    private static List<LessonDefinition> GetBeginnersLessons() =>
        [
            new(
                "Greetings and Introductions",
                10,
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
                10,
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
                10,
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
                10,
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
                10,
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
                10,
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
                10,
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
                10,
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

    private static List<LessonDefinition> GetEverydayConversationsLessons() =>
        [
            new(
                "At the Restaurant",
                7,
                BuildEditorJson(
                    "Al Ristorante",
                    """
                    Learn how to order food and navigate an Italian restaurant:

                    <b>Vorrei...</b> - I would like...
                    <b>Il conto, per favore</b> - The bill, please
                    <b>Buon appetito</b> - Enjoy your meal
                    <b>Un tavolo per due, per favore</b> - A table for two, please

                    Practice these phrases when dining out to sound like a local!
                    """
                )
            ),
            new(
                "Shopping in Italy",
                5,
                BuildEditorJson(
                    "Fare Shopping",
                    """
                    Essential phrases for shopping in Italy:

                    <b>Quanto costa?</b> - How much does it cost?
                    <b>È caro / È economico</b> - It's expensive / It's cheap
                    <b>Posso pagare con carta?</b> - Can I pay by card?
                    <b>Mi piace questo</b> - I like this

                    Useful when browsing markets, boutiques, or any shop!
                    """
                )
            ),
            new(
                "Getting Around",
                5,
                BuildEditorJson(
                    "In Giro per la Città",
                    """
                    Navigate Italian cities with confidence:

                    <b>Dov'è...?</b> - Where is...?
                    <b>a destra / a sinistra</b> - to the right / to the left
                    <b>il treno / l'autobus / il taxi</b> - train / bus / taxi
                    <b>Un biglietto per..., per favore</b> - A ticket to..., please

                    These phrases will help you find your way around any Italian city.
                    """
                )
            ),
            new(
                "Meeting People",
                5,
                BuildEditorJson(
                    "Fare Nuove Amicizie",
                    """
                    Small talk for making new friends in Italian:

                    <b>Di dove sei?</b> - Where are you from?
                    <b>Che lavoro fai?</b> - What do you do for work?
                    <b>Quanti anni hai?</b> - How old are you?
                    <b>Ti piace l'Italia?</b> - Do you like Italy?

                    Italians love to chat — these phrases will get the conversation started!
                    """
                )
            ),
        ];

    private static List<LessonDefinition> GetGrammarEssentialsLessons() =>
        [
            new(
                "Definite Articles",
                10,
                BuildEditorJson(
                    "Gli Articoli Determinativi",
                    """
                    Italian nouns require the correct definite article based on gender and starting sound:

                    <b>il</b> - masculine singular (il libro - the book)
                    <b>lo</b> - masculine singular before z or s+consonant (lo zaino - the backpack)
                    <b>la</b> - feminine singular (la casa - the house)
                    <b>i</b> - masculine plural (i libri - the books)
                    <b>gli</b> - masculine plural before z or s+consonant (gli studenti)
                    <b>le</b> - feminine plural (le case - the houses)

                    Gender agreement is essential in Italian grammar!
                    """
                )
            ),
            new(
                "Regular -ARE Verbs",
                10,
                BuildEditorJson(
                    "Verbi Regolari in -ARE",
                    """
                    Regular -ARE verbs follow a consistent conjugation pattern. Example: <b>parlare</b> (to speak):

                    <b>io parlo</b> - I speak
                    <b>tu parli</b> - you speak
                    <b>lui/lei parla</b> - he/she speaks
                    <b>noi parliamo</b> - we speak
                    <b>voi parlate</b> - you (plural) speak
                    <b>loro parlano</b> - they speak

                    Other common -ARE verbs: <b>mangiare</b> (to eat), <b>guardare</b> (to watch)
                    """
                )
            ),
            new(
                "Question Words",
                8,
                BuildEditorJson(
                    "Le Parole Interrogative",
                    """
                    Master Italian question words to ask about anything:

                    <b>chi</b> - who
                    <b>cosa</b> - what
                    <b>dove</b> - where
                    <b>quando</b> - when
                    <b>come</b> - how
                    <b>perché</b> - why / because
                    <b>quanto / quanta</b> - how much / how many

                    Example: <i>Come ti chiami?</i> (What is your name?)
                    """
                )
            ),
            new(
                "Prepositions",
                10,
                BuildEditorJson(
                    "Le Preposizioni",
                    """
                    Italian prepositions are small but essential words:

                    <b>di</b> - of, from (Sono di Roma - I'm from Rome)
                    <b>a</b> - at, to (Vado a Milano - I'm going to Milan)
                    <b>da</b> - from, since (Studio da tre anni - I've studied for three years)
                    <b>in</b> - in, to (Vivo in Italia - I live in Italy)
                    <b>con</b> - with (Vengo con te - I'm coming with you)
                    <b>su</b> - on (Il libro è sul tavolo - The book is on the table)
                    <b>per</b> - for (Un caffè per favore - A coffee please)
                    <b>tra / fra</b> - between, in (tra due ore - in two hours)
                    """
                )
            ),
        ];

    private static List<LessonDefinition> GetCultureAndIdiomsLessons() =>
        [
            new(
                "Common Expressions",
                5,
                BuildEditorJson(
                    "Espressioni Comuni",
                    """
                    Colourful Italian expressions you'll hear every day:

                    <b>In bocca al lupo!</b> - Good luck! (lit. "In the mouth of the wolf!")
                    <b>Crepi!</b> - The traditional response to "In bocca al lupo!"
                    <b>Mamma mia!</b> - Oh my! (surprise or admiration)
                    <b>Dai!</b> - Come on! / Wow! (encouragement or surprise)
                    <b>Figurati!</b> - Don't mention it! / Of course!
                    <b>Magari!</b> - I wish! / Maybe! (expresses hope or desire)
                    """
                )
            ),
            new(
                "Italian Food Culture",
                5,
                BuildEditorJson(
                    "La Cultura del Cibo",
                    """
                    Food is at the heart of Italian culture:

                    <b>la colazione</b> - breakfast (light — usually a cornetto and caffè)
                    <b>il pranzo</b> - lunch (the main meal of the day)
                    <b>la cena</b> - dinner (lighter than pranzo)
                    <b>il caffè</b> - espresso (never a cappuccino after noon!)
                    <b>l'aperitivo</b> - pre-dinner social drinks and snacks
                    <b>la domenica a tavola</b> - Sunday family lunch is a sacred tradition

                    Understanding these customs helps you connect with Italian culture.
                    """
                )
            ),
            new(
                "Family and Relationships",
                5,
                BuildEditorJson(
                    "La Famiglia",
                    """
                    Family vocabulary is central to Italian life:

                    <b>il nonno / la nonna</b> - grandfather / grandmother
                    <b>il fratello / la sorella</b> - brother / sister
                    <b>lo zio / la zia</b> - uncle / aunt
                    <b>il cugino / la cugina</b> - male cousin / female cousin
                    <b>il marito / la moglie</b> - husband / wife
                    <b>il figlio / la figlia</b> - son / daughter

                    Italians are famously close to their families — la famiglia è tutto!
                    """
                )
            ),
            new(
                "Celebrations",
                5,
                BuildEditorJson(
                    "Le Feste",
                    """
                    Important Italian celebrations and how to greet people:

                    <b>Natale</b> - Christmas (Buon Natale! - Merry Christmas!)
                    <b>Capodanno</b> - New Year's Day (Buon Anno! - Happy New Year!)
                    <b>Ferragosto</b> - August 15th national holiday (peak of summer)
                    <b>Carnevale</b> - Carnival season before Lent (famous in Venice)
                    <b>il compleanno</b> - birthday (Buon compleanno! - Happy Birthday!)

                    Knowing these helps you celebrate like a true Italian!
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
        int DurationMinutes,
        string EditorContent
    );
}
