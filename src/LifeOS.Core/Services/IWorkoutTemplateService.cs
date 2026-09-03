using LifeOS.Core.DTOs.WorkoutTemplates;

namespace LifeOS.Core.Services;

public interface IWorkoutTemplateService
{
    Task<IReadOnlyList<WorkoutTemplateSummaryDto>> GetTemplatesAsync(
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto?> GetTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto> CreateTemplateAsync(
        CreateWorkoutTemplateDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto> RenameTemplateAsync(
        Guid templateId,
        RenameWorkoutTemplateDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto> AddExerciseAsync(
        Guid templateId,
        AddWorkoutTemplateExerciseDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto> UpdateExerciseAsync(
        Guid templateId,
        UpdateWorkoutTemplateExerciseDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto> RemoveExerciseAsync(
        Guid templateId,
        Guid templateExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateDetailDto> ReorderExercisesAsync(
        Guid templateId,
        ReorderWorkoutTemplateExercisesDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteTemplateAsync(
        Guid templateId,
        long expectedVersion,
        CancellationToken cancellationToken = default);
}
