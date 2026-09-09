using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.Abstractions.WorkoutTemplates;
using LifeOS.Core.DTOs.WorkoutTemplates;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class WorkoutTemplateService : IWorkoutTemplateService
{
    private readonly IWorkoutTemplateRepository _repository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly ICurrentUserService _currentUser;

    public WorkoutTemplateService(
        IWorkoutTemplateRepository repository,
        IExerciseRepository exerciseRepository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _exerciseRepository = exerciseRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WorkoutTemplateSummaryDto>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var templates = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        return templates
            .Select(template => new WorkoutTemplateSummaryDto(
                template.Id,
                template.Name,
                template.Exercises.Count,
                template.Version,
                template.UpdatedAtUtc))
            .ToList();
    }

    public async Task<WorkoutTemplateDetailDto?> GetTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(
            GetCurrentUserId(),
            templateId,
            cancellationToken);

        return template is null
            ? null
            : await ToDetailAsync(template, cancellationToken);
    }

    public async Task<WorkoutTemplateDetailDto> CreateTemplateAsync(
        CreateWorkoutTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = GetCurrentUserId();
        if (dto.Exercises is null || dto.Exercises.Count == 0)
        {
            throw new ValidationException("A workout template must contain at least one exercise.");
        }

        var exercises = await GetValidatedExercisesAsync(
            dto.Exercises.Select(item => (item.ExerciseId, item.LoggingMode)),
            cancellationToken);
        var template = new WorkoutTemplate(Guid.NewGuid(), userId, dto.Name);

        foreach (var item in dto.Exercises)
        {
            var exercise = exercises[item.ExerciseId];
            template.AddExercise(
                exercise.Id,
                exercise.LoggingMode,
                item.TargetSetCount,
                item.TargetRepMin,
                item.TargetRepMax,
                item.DefaultRestSeconds);
        }

        template.ValidateForPersistence();
        await _repository.AddAsync(template, cancellationToken);
        return await GetRequiredTemplateAsync(template.Id, cancellationToken);
    }

    public async Task<WorkoutTemplateDetailDto> RenameTemplateAsync(
        Guid templateId,
        RenameWorkoutTemplateDto dto,
        CancellationToken cancellationToken = default) =>
        await MutateAsync(
            templateId,
            dto?.ExpectedVersion ?? throw new ArgumentNullException(nameof(dto)),
            cancellationToken,
            template => template.Rename(dto.Name));

    public async Task<WorkoutTemplateDetailDto> AddExerciseAsync(
        Guid templateId,
        AddWorkoutTemplateExerciseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var exercises = await GetValidatedExercisesAsync(
            [(dto.ExerciseId, dto.LoggingMode)],
            cancellationToken);
        var exercise = exercises[dto.ExerciseId];

        return await MutateAsync(
            templateId,
            dto.ExpectedVersion,
            cancellationToken,
            template => template.AddExercise(
                exercise.Id,
                exercise.LoggingMode,
                dto.TargetSetCount,
                dto.TargetRepMin,
                dto.TargetRepMax,
                dto.DefaultRestSeconds));
    }

    public async Task<WorkoutTemplateDetailDto> UpdateExerciseAsync(
        Guid templateId,
        UpdateWorkoutTemplateExerciseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var template = await GetRequiredEntityAsync(templateId, cancellationToken);
        EnsureExpectedVersion(template, dto.ExpectedVersion);
        var child = template.Exercises.SingleOrDefault(item => item.Id == dto.TemplateExerciseId)
            ?? throw new ResourceNotFoundException("Template exercise was not found.");
        var exercises = await GetValidatedExercisesAsync(
            [(child.ExerciseId, dto.LoggingMode)],
            cancellationToken);

        template.UpdateExercise(
            child.Id,
            exercises[child.ExerciseId].LoggingMode,
            dto.TargetSetCount,
            dto.TargetRepMin,
            dto.TargetRepMax,
            dto.DefaultRestSeconds);
        return await PersistMutationAsync(template, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutTemplateDetailDto> RemoveExerciseAsync(
        Guid templateId,
        Guid templateExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var template = await GetRequiredEntityAsync(templateId, cancellationToken);
        EnsureExpectedVersion(template, expectedVersion);
        if (template.Exercises.Count == 1)
        {
            throw new ValidationException("The final exercise cannot be removed from a template.");
        }

        template.RemoveExercise(templateExerciseId);
        return await PersistMutationAsync(template, expectedVersion, cancellationToken);
    }

    public async Task<WorkoutTemplateDetailDto> ReorderExercisesAsync(
        Guid templateId,
        ReorderWorkoutTemplateExercisesDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var template = await GetRequiredEntityAsync(templateId, cancellationToken);
        EnsureExpectedVersion(template, dto.ExpectedVersion);
        template.ReorderExercises(dto.OrderedTemplateExerciseIds);
        return await PersistMutationAsync(template, dto.ExpectedVersion, cancellationToken);
    }

    public async Task DeleteTemplateAsync(
        Guid templateId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteAsync(
            GetCurrentUserId(),
            templateId,
            expectedVersion,
            cancellationToken);
        EnsureWriteSucceeded(result.Status, "Workout template was not found.");
    }

    private async Task<WorkoutTemplateDetailDto> MutateAsync(
        Guid templateId,
        long expectedVersion,
        CancellationToken cancellationToken,
        Action<WorkoutTemplate> mutation)
    {
        var template = await GetRequiredEntityAsync(templateId, cancellationToken);
        EnsureExpectedVersion(template, expectedVersion);
        mutation(template);
        return await PersistMutationAsync(template, expectedVersion, cancellationToken);
    }

    private async Task<WorkoutTemplateDetailDto> PersistMutationAsync(
        WorkoutTemplate template,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        template.ValidateForPersistence();
        var result = await _repository.UpdateAsync(
            GetCurrentUserId(),
            template,
            expectedVersion,
            cancellationToken);
        EnsureWriteSucceeded(result.Status, "Workout template was not found.");
        return await GetRequiredTemplateAsync(template.Id, cancellationToken);
    }

    private async Task<WorkoutTemplate> GetRequiredEntityAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(
            GetCurrentUserId(),
            templateId,
            cancellationToken);
        return template ?? throw new ResourceNotFoundException("Workout template was not found.");
    }

    private async Task<WorkoutTemplateDetailDto> GetRequiredTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var detail = await GetTemplateAsync(templateId, cancellationToken);
        return detail ?? throw new ResourceNotFoundException("Workout template was not found.");
    }

    private async Task<Dictionary<Guid, Exercise>> GetValidatedExercisesAsync(
        IEnumerable<(Guid ExerciseId, ExerciseLoggingMode LoggingMode)> requested,
        CancellationToken cancellationToken)
    {
        var requestedItems = requested.ToArray();
        if (requestedItems.Any(item => item.ExerciseId == Guid.Empty))
        {
            throw new ValidationException("Exercise ID is required.");
        }

        var exercises = await _exerciseRepository.GetActiveByIdsAsync(
            requestedItems.Select(item => item.ExerciseId).Distinct().ToArray(),
            cancellationToken);
        var byId = exercises.ToDictionary(exercise => exercise.Id);

        foreach (var item in requestedItems)
        {
            if (!byId.TryGetValue(item.ExerciseId, out var exercise))
            {
                throw new ResourceNotFoundException("Active Exercise was not found.");
            }

            if (exercise.LoggingMode != item.LoggingMode)
            {
                throw new ValidationException("The Exercise logging mode does not match the catalog.");
            }
        }

        return byId;
    }

    private async Task<WorkoutTemplateDetailDto> ToDetailAsync(
        WorkoutTemplate template,
        CancellationToken cancellationToken)
    {
        var exercises = await _exerciseRepository.GetActiveByIdsAsync(
            template.Exercises.Select(item => item.ExerciseId).Distinct().ToArray(),
            cancellationToken);
        var byId = exercises.ToDictionary(item => item.Id);

        return new WorkoutTemplateDetailDto(
            template.Id,
            template.Name,
            template.Version,
            template.Exercises
                .OrderBy(item => item.SortOrder)
                .Select(item =>
                {
                    if (!byId.TryGetValue(item.ExerciseId, out var exercise))
                    {
                        throw new ResourceNotFoundException("Template Exercise reference was not found.");
                    }

                    return new WorkoutTemplateExerciseDto(
                        item.Id,
                        item.ExerciseId,
                        exercise.Name,
                        exercise.LoggingMode,
                        item.SortOrder,
                        item.TargetSetCount,
                        item.TargetRepMin,
                        item.TargetRepMax,
                        item.DefaultRestSeconds);
                })
                .ToList(),
            template.UpdatedAtUtc);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }

        return _currentUser.UserId;
    }

    private static void EnsureExpectedVersion(WorkoutTemplate template, long expectedVersion)
    {
        if (expectedVersion <= 0 || template.Version != expectedVersion)
        {
            throw new WorkoutTemplateConcurrencyException();
        }
    }

    private static void EnsureWriteSucceeded(
        WorkoutTemplateWriteStatus status,
        string notFoundMessage)
    {
        switch (status)
        {
            case WorkoutTemplateWriteStatus.Succeeded:
                return;
            case WorkoutTemplateWriteStatus.NotFound:
                throw new ResourceNotFoundException(notFoundMessage);
            case WorkoutTemplateWriteStatus.ConcurrencyConflict:
                throw new WorkoutTemplateConcurrencyException();
            default:
                throw new InvalidOperationException("Unknown workout template write status.");
        }
    }
}
