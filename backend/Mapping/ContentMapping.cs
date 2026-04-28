using Backend.Api.Dtos;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Riok.Mapperly.Abstractions;

namespace Backend.Api.Mapping;

[Mapper]
public partial class ContentMapping
{
    // Language mappings
    [MapperIgnoreSource(nameof(Language.LanguageId))]
    [MapperIgnoreSource(nameof(Language.CreatedAt))]
    [MapperIgnoreSource(nameof(Language.UserLanguages))]
    [MapProperty(
        nameof(Language.Courses),
        nameof(LanguageDto.CourseCount),
        Use = nameof(CountCourses)
    )]
    public partial LanguageDto MapToDto(Language entity);

    [MapperIgnoreTarget(nameof(Language.LanguageId))]
    [MapperIgnoreTarget(nameof(Language.CreatedAt))]
    [MapperIgnoreTarget(nameof(Language.Courses))]
    [MapperIgnoreTarget(nameof(Language.UserLanguages))]
    public partial Language MapToEntity(CreateLanguageDto dto);

    // Course mappings
    [MapperIgnoreSource(nameof(Course.LanguageId))]
    [MapperIgnoreSource(nameof(Course.CreatedById))]
    [MapperIgnoreSource(nameof(Course.CreatedAt))]
    [MapperIgnoreSource(nameof(Course.UpdatedAt))]
    [MapperIgnoreSource(nameof(Course.CreatedBy))]
    [MapProperty(nameof(Course.Language.LanguageName), nameof(CourseDto.LanguageName))]
    [MapProperty(nameof(Course.Lessons), nameof(CourseDto.LessonCount), Use = nameof(CountLessons))]
    public partial CourseDto MapToDto(Course entity);

    [MapperIgnoreSource(nameof(CreateCourseDto.LanguageName))]
    [MapperIgnoreTarget(nameof(Course.CourseId))]
    [MapperIgnoreTarget(nameof(Course.LanguageId))]
    [MapperIgnoreTarget(nameof(Course.CreatedById))]
    [MapperIgnoreTarget(nameof(Course.CreatedAt))]
    [MapperIgnoreTarget(nameof(Course.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Course.Language))]
    [MapperIgnoreTarget(nameof(Course.Lessons))]
    [MapperIgnoreTarget(nameof(Course.CreatedBy))]
    public partial Course MapToEntity(CreateCourseDto dto);

    // Lesson mappings
    [MapperIgnoreSource(nameof(Lesson.CreatedAt))]
    [MapProperty(nameof(Lesson.Course.Title), nameof(LessonDto.CourseTitle))]
    [MapperIgnoreTarget(nameof(LessonDto.CompletedExercises))]
    [MapperIgnoreTarget(nameof(LessonDto.EarnedXp))]
    [MapperIgnoreTarget(nameof(LessonDto.TotalPossibleXp))]
    [MapperIgnoreTarget(nameof(LessonDto.IsCompleted))]
    public partial LessonDto MapToDto(Lesson entity);

    [MapProperty(nameof(CreateLessonDto.Content), nameof(Lesson.LessonContent))]
    [MapperIgnoreSource(nameof(CreateLessonDto.CourseId))]
    [MapperIgnoreSource(nameof(CreateLessonDto.OrderIndex))]
    [MapperIgnoreSource(nameof(CreateLessonDto.Exercises))]
    [MapperIgnoreTarget(nameof(Lesson.LessonId))]
    [MapperIgnoreTarget(nameof(Lesson.CourseId))]
    [MapperIgnoreTarget(nameof(Lesson.OrderIndex))]
    [MapperIgnoreTarget(nameof(Lesson.IsLocked))]
    [MapperIgnoreTarget(nameof(Lesson.CreatedAt))]
    [MapperIgnoreTarget(nameof(Lesson.Course))]
    [MapperIgnoreTarget(nameof(Lesson.Exercises))]
    public partial Lesson MapToEntity(CreateLessonDto dto);

    // Exercise polymorphic mapping - manual dispatch
    public ExerciseDto MapToDto(Exercise entity)
    {
        return entity switch
        {
            FillInBlankExercise fib => MapToDto(fib),
            ListeningExercise le => MapToDto(le),
            TrueFalseExercise tf => MapToDto(tf),
            ImageChoiceExercise ic => MapToDto(ic),
            AudioMatchingExercise am => MapToDto(am),
            _ => throw new NotSupportedException(
                $"Exercise type {entity.GetType().Name} is not supported"
            ),
        };
    }

    // Exercise type-specific mappings
    [MapperIgnoreSource(nameof(FillInBlankExercise.CreatedById))]
    [MapperIgnoreSource(nameof(FillInBlankExercise.Lesson))]
    [MapperIgnoreSource(nameof(FillInBlankExercise.CreatedBy))]
    [MapperIgnoreSource(nameof(FillInBlankExercise.ExerciseProgress))]
    [MapperIgnoreTarget(nameof(FillInBlankExerciseDto.UserProgress))]
    public partial FillInBlankExerciseDto MapToDto(FillInBlankExercise entity);

    [MapperIgnoreSource(nameof(ListeningExercise.CreatedById))]
    [MapperIgnoreSource(nameof(ListeningExercise.Lesson))]
    [MapperIgnoreSource(nameof(ListeningExercise.CreatedBy))]
    [MapperIgnoreSource(nameof(ListeningExercise.ExerciseProgress))]
    [MapperIgnoreTarget(nameof(ListeningExerciseDto.UserProgress))]
    public partial ListeningExerciseDto MapToDto(ListeningExercise entity);

    [MapperIgnoreSource(nameof(TrueFalseExercise.CreatedById))]
    [MapperIgnoreSource(nameof(TrueFalseExercise.Lesson))]
    [MapperIgnoreSource(nameof(TrueFalseExercise.CreatedBy))]
    [MapperIgnoreSource(nameof(TrueFalseExercise.ExerciseProgress))]
    [MapperIgnoreTarget(nameof(TrueFalseExerciseDto.UserProgress))]
    public partial TrueFalseExerciseDto MapToDto(TrueFalseExercise entity);

    [MapperIgnoreSource(nameof(ImageChoiceExercise.CreatedById))]
    [MapperIgnoreSource(nameof(ImageChoiceExercise.Lesson))]
    [MapperIgnoreSource(nameof(ImageChoiceExercise.CreatedBy))]
    [MapperIgnoreSource(nameof(ImageChoiceExercise.ExerciseProgress))]
    [MapperIgnoreTarget(nameof(ImageChoiceExerciseDto.UserProgress))]
    public partial ImageChoiceExerciseDto MapToDto(ImageChoiceExercise entity);

    [MapperIgnoreSource(nameof(AudioMatchingExercise.CreatedById))]
    [MapperIgnoreSource(nameof(AudioMatchingExercise.Lesson))]
    [MapperIgnoreSource(nameof(AudioMatchingExercise.CreatedBy))]
    [MapperIgnoreSource(nameof(AudioMatchingExercise.ExerciseProgress))]
    [MapperIgnoreTarget(nameof(AudioMatchingExerciseDto.UserProgress))]
    public partial AudioMatchingExerciseDto MapToDto(AudioMatchingExercise entity);

    // Exercise creation mappings - ignore auto-generated fields
    [MapperIgnoreSource(nameof(CreateFillInBlankExerciseDto.LessonId))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.ExerciseId))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.LessonId))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.CreatedById))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.IsLocked))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.CreatedAt))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.Lesson))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.CreatedBy))]
    [MapperIgnoreTarget(nameof(FillInBlankExercise.ExerciseProgress))]
    public partial FillInBlankExercise MapToEntity(CreateFillInBlankExerciseDto dto);

    [MapperIgnoreSource(nameof(CreateListeningExerciseDto.LessonId))]
    [MapperIgnoreTarget(nameof(ListeningExercise.ExerciseId))]
    [MapperIgnoreTarget(nameof(ListeningExercise.LessonId))]
    [MapperIgnoreTarget(nameof(ListeningExercise.CreatedById))]
    [MapperIgnoreTarget(nameof(ListeningExercise.IsLocked))]
    [MapperIgnoreTarget(nameof(ListeningExercise.CreatedAt))]
    [MapperIgnoreTarget(nameof(ListeningExercise.Lesson))]
    [MapperIgnoreTarget(nameof(ListeningExercise.CreatedBy))]
    [MapperIgnoreTarget(nameof(ListeningExercise.ExerciseProgress))]
    public partial ListeningExercise MapToEntity(CreateListeningExerciseDto dto);

    [MapperIgnoreSource(nameof(CreateTrueFalseExerciseDto.LessonId))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.ExerciseId))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.LessonId))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.CreatedById))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.IsLocked))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.CreatedAt))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.Lesson))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.CreatedBy))]
    [MapperIgnoreTarget(nameof(TrueFalseExercise.ExerciseProgress))]
    public partial TrueFalseExercise MapToEntity(CreateTrueFalseExerciseDto dto);

    [MapperIgnoreSource(nameof(CreateImageChoiceExerciseDto.LessonId))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.ExerciseId))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.LessonId))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.CreatedById))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.IsLocked))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.CreatedAt))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.Lesson))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.CreatedBy))]
    [MapperIgnoreTarget(nameof(ImageChoiceExercise.ExerciseProgress))]
    public partial ImageChoiceExercise MapToEntity(CreateImageChoiceExerciseDto dto);

    [MapperIgnoreSource(nameof(CreateAudioMatchingExerciseDto.LessonId))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.ExerciseId))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.LessonId))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.CreatedById))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.IsLocked))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.CreatedAt))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.Lesson))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.CreatedBy))]
    [MapperIgnoreTarget(nameof(AudioMatchingExercise.ExerciseProgress))]
    public partial AudioMatchingExercise MapToEntity(CreateAudioMatchingExerciseDto dto);

    // Child entity mappings
    [MapperIgnoreSource(nameof(ExerciseOption.ExerciseId))]
    [MapperIgnoreSource(nameof(ExerciseOption.Exercise))]
    public partial ExerciseOptionDto MapToDto(ExerciseOption entity);

    [MapperIgnoreSource(nameof(ImageOption.ImageChoiceExerciseId))]
    [MapperIgnoreSource(nameof(ImageOption.Exercise))]
    public partial ImageOptionDto MapToDto(ImageOption entity);

    [MapperIgnoreSource(nameof(AudioMatchPair.AudioMatchingExerciseId))]
    [MapperIgnoreSource(nameof(AudioMatchPair.Exercise))]
    public partial AudioMatchPairDto MapToDto(AudioMatchPair entity);

    [MapperIgnoreTarget(nameof(ExerciseOption.ExerciseOptionId))]
    [MapperIgnoreTarget(nameof(ExerciseOption.ExerciseId))]
    [MapperIgnoreTarget(nameof(ExerciseOption.Exercise))]
    public partial ExerciseOption MapToEntity(CreateExerciseOptionDto dto);

    [MapperIgnoreTarget(nameof(ImageOption.ImageOptionId))]
    [MapperIgnoreTarget(nameof(ImageOption.ImageChoiceExerciseId))]
    [MapperIgnoreTarget(nameof(ImageOption.Exercise))]
    public partial ImageOption MapToEntity(CreateImageOptionDto dto);

    [MapperIgnoreTarget(nameof(AudioMatchPair.AudioMatchPairId))]
    [MapperIgnoreTarget(nameof(AudioMatchPair.AudioMatchingExerciseId))]
    [MapperIgnoreTarget(nameof(AudioMatchPair.Exercise))]
    public partial AudioMatchPair MapToEntity(CreateAudioMatchPairDto dto);

    // Helper methods for collection counts
    private int CountCourses(ICollection<Course> courses) => courses.Count;

    private int CountLessons(ICollection<Lesson> lessons) => lessons.Count;
}
