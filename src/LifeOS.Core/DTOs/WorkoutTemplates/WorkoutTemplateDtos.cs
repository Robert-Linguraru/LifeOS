using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.DTOs.WorkoutTemplates;

public sealed record WorkoutTemplateSummaryDto(
    Guid Id,
    string Name,
    int ExerciseCount,
    long Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record WorkoutTemplateDetailDto(
    Guid Id,
    string Name,
    long Version,
    IReadOnlyList<WorkoutTemplateExerciseDto> Exercises,
    DateTimeOffset UpdatedAtUtc);

public sealed record WorkoutTemplateExerciseDto(
    Guid TemplateExerciseId,
    Guid ExerciseId,
    string ExerciseName,
    ExerciseLoggingMode LoggingMode,
    int SortOrder,
    int TargetSetCount,
    int? TargetRepMin,
    int? TargetRepMax,
    int DefaultRestSeconds);

public sealed record CreateWorkoutTemplateDto(
    string Name,
    IReadOnlyList<CreateWorkoutTemplateExerciseDto> Exercises);

public sealed record CreateWorkoutTemplateExerciseDto(
    Guid ExerciseId,
    ExerciseLoggingMode LoggingMode,
    int TargetSetCount,
    int? TargetRepMin,
    int? TargetRepMax,
    int DefaultRestSeconds);

public sealed record RenameWorkoutTemplateDto(
    string Name,
    long ExpectedVersion);

public sealed record AddWorkoutTemplateExerciseDto(
    Guid ExerciseId,
    ExerciseLoggingMode LoggingMode,
    int TargetSetCount,
    int? TargetRepMin,
    int? TargetRepMax,
    int DefaultRestSeconds,
    long ExpectedVersion);

public sealed record UpdateWorkoutTemplateExerciseDto(
    Guid TemplateExerciseId,
    ExerciseLoggingMode LoggingMode,
    int TargetSetCount,
    int? TargetRepMin,
    int? TargetRepMax,
    int DefaultRestSeconds,
    long ExpectedVersion);

public sealed record ReorderWorkoutTemplateExercisesDto(
    IReadOnlyList<Guid> OrderedTemplateExerciseIds,
    long ExpectedVersion);
