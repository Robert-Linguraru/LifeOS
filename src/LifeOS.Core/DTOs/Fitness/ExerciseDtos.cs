using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.DTOs.Fitness;

public sealed record ExerciseSummaryDto(
    Guid Id,
    string Name,
    MuscleGroup PrimaryMuscleGroup,
    ExerciseEquipment Equipment,
    MovementPattern MovementPattern,
    ExerciseLoggingMode LoggingMode);

public sealed record ExerciseDetailDto(
    Guid Id,
    string Name,
    MuscleGroup PrimaryMuscleGroup,
    ExerciseEquipment Equipment,
    MovementPattern MovementPattern,
    ExerciseLoggingMode LoggingMode,
    IReadOnlyList<MuscleGroup> SecondaryMuscleGroups);

public sealed record ExerciseQuery(
    string? Search = null,
    MuscleGroup? PrimaryMuscleGroup = null,
    ExerciseEquipment? Equipment = null,
    MovementPattern? MovementPattern = null,
    ExerciseLoggingMode? LoggingMode = null);
