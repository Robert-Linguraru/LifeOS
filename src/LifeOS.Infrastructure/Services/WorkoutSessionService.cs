using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.Abstractions.WorkoutSessions;
using LifeOS.Core.Abstractions.WorkoutTemplates;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class WorkoutSessionService : IWorkoutSessionService
{
    private readonly IWorkoutSessionRepository _sessionRepository;
    private readonly IWorkoutTemplateRepository _templateRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WorkoutSessionService(
        IWorkoutSessionRepository sessionRepository,
        IWorkoutTemplateRepository templateRepository,
        IExerciseRepository exerciseRepository,
        IUserSettingsRepository userSettingsRepository,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _templateRepository = templateRepository;
        _exerciseRepository = exerciseRepository;
        _userSettingsRepository = userSettingsRepository;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WorkoutSessionDetailDto?> GetActiveSessionAsync(
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveByUserIdAsync(
            GetCurrentUserId(),
            cancellationToken);
        return session is null ? null : ToDetail(session);
    }

    public async Task<WorkoutHistoryPageDto> GetWorkoutHistoryAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        return await _sessionRepository.GetCompletedHistoryAsync(
            GetCurrentUserId(),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto?> GetCompletedWorkoutAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetCompletedByIdAsync(
            GetCurrentUserId(),
            sessionId,
            cancellationToken);
        return session is null ? null : ToDetail(session);
    }

    public async Task<IReadOnlyList<PreviousPerformanceDto>> GetPreviousPerformancesAsync(
        Guid currentSessionId,
        IReadOnlyCollection<Guid> exerciseIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exerciseIds);
        var distinctExerciseIds = exerciseIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        return await _sessionRepository.GetPreviousPerformancesAsync(
            GetCurrentUserId(),
            distinctExerciseIds,
            currentSessionId,
            cancellationToken);
    }

    public async Task<ExerciseHistoryDto> GetExerciseHistoryAsync(
        Guid exerciseId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        var exercise = await _exerciseRepository.GetByIdIncludingInactiveAsync(
            exerciseId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Exercise was not found.");
        return await _sessionRepository.GetExerciseHistoryAsync(
            GetCurrentUserId(),
            exerciseId,
            exercise.Name,
            exercise.LoggingMode,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> AddExerciseAsync(
        Guid sessionId,
        AddSessionExerciseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        if (session.OriginTemplateId is not null)
        {
            throw new ValidationException("Exercises cannot be added to a Template workout.");
        }

        var exercise = await GetAuthoritativeExerciseAsync(
            dto.ExerciseId,
            dto.LoggingModeSnapshot,
            cancellationToken);
        session.AddExercise(
            exercise.Id,
            exercise.Name,
            exercise.LoggingMode,
            dto.TargetSetCountSnapshot,
            dto.TargetRepMinSnapshot,
            dto.TargetRepMaxSnapshot,
            dto.DefaultRestSecondsSnapshot);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> RemoveExerciseAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var session = await GetMutableSessionAsync(sessionId, expectedVersion, cancellationToken);
        if (session.OriginTemplateId is not null)
        {
            throw new ValidationException("Exercises cannot be removed from a Template workout.");
        }

        if (session.Exercises.Count == 1)
        {
            throw new ValidationException("The final exercise cannot be removed from a workout.");
        }

        session.RemoveExercise(sessionExerciseId);
        return await PersistMutationAsync(session, expectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> SubstituteExerciseAsync(
        Guid sessionId,
        SubstituteSessionExerciseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        var current = session.Exercises.SingleOrDefault(item => item.Id == dto.SessionExerciseId)
            ?? throw new ResourceNotFoundException("Session Exercise was not found.");
        var replacement = await GetAuthoritativeExerciseAsync(
            dto.ExerciseId,
            dto.LoggingMode,
            cancellationToken);

        if ((current.TargetRepMinSnapshot is not null || current.TargetRepMaxSnapshot is not null) &&
            !IsRepBased(replacement.LoggingMode))
        {
            throw new ValidationException("The replacement Exercise is incompatible with the target rep range.");
        }

        session.SubstituteExercise(
            current.Id,
            replacement.Id,
            replacement.Name,
            replacement.LoggingMode);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> SkipExerciseAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var session = await GetMutableSessionAsync(sessionId, expectedVersion, cancellationToken);
        session.SkipExercise(sessionExerciseId);
        return await PersistMutationAsync(session, expectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> UnskipExerciseAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var session = await GetMutableSessionAsync(sessionId, expectedVersion, cancellationToken);
        session.UnskipExercise(sessionExerciseId);
        return await PersistMutationAsync(session, expectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> AddSetAsync(
        Guid sessionId,
        AddWorkoutSetDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.AddSet(
            dto.SessionExerciseId,
            dto.Kind,
            dto.WeightKg,
            dto.Repetitions,
            dto.DurationSeconds);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> UpdateSetAsync(
        Guid sessionId,
        UpdateWorkoutSetDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.UpdateSet(
            dto.SessionExerciseId,
            dto.SetId,
            dto.Kind,
            dto.WeightKg,
            dto.Repetitions,
            dto.DurationSeconds);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> RemoveSetAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        Guid setId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var session = await GetMutableSessionAsync(sessionId, expectedVersion, cancellationToken);
        session.RemoveSet(sessionExerciseId, setId);
        return await PersistMutationAsync(session, expectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> CompleteSetAsync(
        Guid sessionId,
        CompleteWorkoutSetDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;
        session.CompleteSetAndMaybeStartRest(
            dto.SessionExerciseId,
            dto.SetId,
            nowUtc,
            dto.WeightKg,
            dto.Repetitions,
            dto.DurationSeconds,
            dto.StartRestTimer);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> StartRestTimerAsync(
        Guid sessionId,
        StartRestTimerDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.StartRestTimer(dto.DurationSeconds, _dateTimeProvider.UtcNow);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> PauseRestTimerAsync(
        Guid sessionId,
        TimerTimestampDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.PauseRestTimer(_dateTimeProvider.UtcNow);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> ResumeRestTimerAsync(
        Guid sessionId,
        TimerTimestampDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.ResumeRestTimer(_dateTimeProvider.UtcNow);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> ResetRestTimerAsync(
        Guid sessionId,
        TimerTimestampDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.ResetRestTimer(_dateTimeProvider.UtcNow);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> AdjustRestTimerAsync(
        Guid sessionId,
        AdjustRestTimerDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.AdjustRestTimer(dto.DurationSeconds, _dateTimeProvider.UtcNow);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> ClearRestTimerAsync(
        Guid sessionId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var session = await GetMutableSessionAsync(sessionId, expectedVersion, cancellationToken);
        session.ClearRestTimer();
        return await PersistMutationAsync(session, expectedVersion, cancellationToken);
    }

    public async Task<WorkoutCompletionSummaryDto> CompleteWorkoutAsync(
        Guid sessionId,
        CompleteWorkoutDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.Complete(_dateTimeProvider.UtcNow, dto.SessionFeeling);
        var result = await _sessionRepository.UpdateAsync(
            session.UserId,
            session,
            dto.ExpectedVersion,
            cancellationToken);
        EnsureWriteSucceeded(result.Status);

        var authoritative = await _sessionRepository.GetByIdAsync(
            session.UserId,
            session.Id,
            cancellationToken)
            ?? throw new ResourceNotFoundException("The completed workout could not be loaded.");
        return ToCompletionSummary(authoritative);
    }

    public async Task<WorkoutSessionDetailDto> DiscardWorkoutAsync(
        Guid sessionId,
        DiscardWorkoutDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var session = await GetMutableSessionAsync(sessionId, dto.ExpectedVersion, cancellationToken);
        session.Discard(_dateTimeProvider.UtcNow);
        return await PersistMutationAsync(session, dto.ExpectedVersion, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> StartFromTemplateAsync(
        StartTemplateWorkoutDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = GetCurrentUserId();
        var existing = await _sessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return ToDetail(existing);
        }

        var template = await _templateRepository.GetByIdAsync(userId, dto.TemplateId, cancellationToken)
            ?? throw new ResourceNotFoundException("Workout template was not found.");
        if (template.Exercises.Count == 0)
        {
            throw new ValidationException("A workout template must contain at least one exercise.");
        }

        var exerciseIds = template.Exercises.Select(item => item.ExerciseId).Distinct().ToArray();
        var exercises = await _exerciseRepository.GetActiveByIdsAsync(exerciseIds, cancellationToken);
        var byId = exercises.ToDictionary(item => item.Id);
        var session = new WorkoutSession(
            Guid.NewGuid(),
            userId,
            template.Name,
            await GetWorkoutDateAsync(userId, cancellationToken),
            _dateTimeProvider.UtcNow,
            template.Id);

        foreach (var templateExercise in template.Exercises.OrderBy(item => item.SortOrder))
        {
            if (!byId.TryGetValue(templateExercise.ExerciseId, out var exercise))
            {
                throw new ResourceNotFoundException("A Template Exercise is no longer active.");
            }

            session.AddInitialExercise(
                exercise.Id,
                exercise.Name,
                exercise.LoggingMode,
                templateExercise.TargetSetCount,
                templateExercise.TargetRepMin,
                templateExercise.TargetRepMax,
                templateExercise.DefaultRestSeconds);
        }

        return await PersistStartAsync(session, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto> StartCustomWorkoutAsync(
        StartCustomWorkoutDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = GetCurrentUserId();
        var existing = await _sessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return ToDetail(existing);
        }

        if (dto.Exercises is null || dto.Exercises.Count == 0)
        {
            throw new ValidationException("A Custom Workout must contain at least one Exercise.");
        }

        var requested = dto.Exercises.Select(item => (item.ExerciseId, item.LoggingModeSnapshot)).ToArray();
        var exercises = await GetValidatedExercisesAsync(requested, cancellationToken);
        var session = new WorkoutSession(
            Guid.NewGuid(),
            userId,
            dto.NameSnapshot,
            await GetWorkoutDateAsync(userId, cancellationToken),
            _dateTimeProvider.UtcNow);

        foreach (var item in dto.Exercises)
        {
            var exercise = exercises[item.ExerciseId];
            session.AddInitialExercise(
                exercise.Id,
                exercise.Name,
                exercise.LoggingMode,
                item.TargetSetCountSnapshot,
                item.TargetRepMinSnapshot,
                item.TargetRepMaxSnapshot,
                item.DefaultRestSecondsSnapshot);
        }

        return await PersistStartAsync(session, cancellationToken);
    }

    public async Task<WorkoutSessionDetailDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(
            GetCurrentUserId(),
            sessionId,
            cancellationToken);
        return session is null ? null : ToDetail(session);
    }

    private async Task<WorkoutSessionDetailDto> PersistMutationAsync(
        WorkoutSession session,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var result = await _sessionRepository.UpdateAsync(
            session.UserId,
            session,
            expectedVersion,
            cancellationToken);
        EnsureWriteSucceeded(result.Status);
        var authoritative = await _sessionRepository.GetByIdAsync(
            session.UserId,
            session.Id,
            cancellationToken);
        return authoritative is null
            ? throw new ResourceNotFoundException("The workout session could not be loaded.")
            : ToDetail(authoritative);
    }

    private async Task<WorkoutSession> GetMutableSessionAsync(
        Guid sessionId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(
            GetCurrentUserId(),
            sessionId,
            cancellationToken);
        if (session is null)
        {
            throw new ResourceNotFoundException("Workout session was not found.");
        }

        if (session.Status != WorkoutSessionStatus.InProgress)
        {
            throw new ValidationException("Only an active workout can be changed.");
        }

        if (session.Version != expectedVersion)
        {
            throw new WorkoutSessionConcurrencyException();
        }

        return session;
    }

    private async Task<Exercise> GetAuthoritativeExerciseAsync(
        Guid exerciseId,
        ExerciseLoggingMode requestedLoggingMode,
        CancellationToken cancellationToken)
    {
        var exercise = (await _exerciseRepository.GetActiveByIdsAsync(
            [exerciseId],
            cancellationToken)).SingleOrDefault()
            ?? throw new ResourceNotFoundException("Active Exercise was not found.");
        if (exercise.LoggingMode != requestedLoggingMode)
        {
            throw new ValidationException("The Exercise logging mode does not match the catalog.");
        }

        return exercise;
    }

    private static void EnsureWriteSucceeded(WorkoutSessionWriteStatus status)
    {
        switch (status)
        {
            case WorkoutSessionWriteStatus.Succeeded:
                return;
            case WorkoutSessionWriteStatus.NotFound:
                throw new ResourceNotFoundException("Workout session was not found.");
            case WorkoutSessionWriteStatus.ConcurrencyConflict:
                throw new WorkoutSessionConcurrencyException();
            default:
                throw new InvalidOperationException("Unknown workout session write status.");
        }
    }

    private static bool IsRepBased(ExerciseLoggingMode mode) =>
        mode is ExerciseLoggingMode.WeightAndReps
            or ExerciseLoggingMode.BodyweightAndReps
            or ExerciseLoggingMode.AddedWeightAndReps
            or ExerciseLoggingMode.AssistedWeightAndReps
            or ExerciseLoggingMode.RepsOnly;

    private async Task<WorkoutSessionDetailDto> PersistStartAsync(
        WorkoutSession session,
        CancellationToken cancellationToken)
    {
        var result = await _sessionRepository.AddAsync(session, cancellationToken);
        if (result.Status == WorkoutSessionWriteStatus.ActiveSessionAlreadyExists && result.Session is not null)
        {
            return ToDetail(result.Session);
        }

        if (result.Status != WorkoutSessionWriteStatus.Succeeded)
        {
            throw new InvalidOperationException("The workout session could not be started.");
        }

        var authoritative = await _sessionRepository.GetByIdAsync(
            session.UserId,
            session.Id,
            cancellationToken);
        return authoritative is null
            ? throw new ResourceNotFoundException("The started workout session could not be loaded.")
            : ToDetail(authoritative);
    }

    private async Task<Dictionary<Guid, Exercise>> GetValidatedExercisesAsync(
        IReadOnlyCollection<(Guid ExerciseId, ExerciseLoggingMode LoggingMode)> requested,
        CancellationToken cancellationToken)
    {
        if (requested.Any(item => item.ExerciseId == Guid.Empty))
        {
            throw new ValidationException("Exercise ID is required.");
        }

        var exercises = await _exerciseRepository.GetActiveByIdsAsync(
            requested.Select(item => item.ExerciseId).Distinct().ToArray(),
            cancellationToken);
        var byId = exercises.ToDictionary(item => item.Id);
        foreach (var item in requested)
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

    private async Task<DateOnly> GetWorkoutDateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await _userSettingsRepository.GetByUserIdAsync(userId, cancellationToken);
        var timeZoneId = settings?.TimeZoneId;
        return _dateTimeProvider.GetCurrentDate(
            string.IsNullOrWhiteSpace(timeZoneId) || !_dateTimeProvider.IsValidTimeZone(timeZoneId)
                ? "UTC"
                : timeZoneId);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }

        return _currentUser.UserId;
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }
    }

    private static WorkoutSessionDetailDto ToDetail(WorkoutSession session) =>
        new(
            session.Id,
            session.OriginTemplateId,
            session.NameSnapshot,
            session.WorkoutDate,
            session.StartedAtUtc,
            session.Status,
            session.CompletedAtUtc,
            session.DiscardedAtUtc,
            session.SessionFeeling,
            session.Version,
            session.RestTimerDurationSeconds,
            session.RestTimerEndsAtUtc,
            session.RestTimerPausedRemainingSeconds,
            session.Exercises
                .OrderBy(item => item.SortOrder)
                .Select(exercise => new WorkoutSessionExerciseDto(
                    exercise.Id,
                    exercise.SortOrder,
                    exercise.OriginalExerciseId,
                    exercise.OriginalExerciseNameSnapshot,
                    exercise.ExerciseId,
                    exercise.ExerciseNameSnapshot,
                    exercise.LoggingModeSnapshot,
                    exercise.TargetSetCountSnapshot,
                    exercise.TargetRepMinSnapshot,
                    exercise.TargetRepMaxSnapshot,
                    exercise.DefaultRestSecondsSnapshot,
                    exercise.IsSkipped,
                    exercise.Sets
                        .OrderBy(set => set.SortOrder)
                        .Select(set => new WorkoutSetDto(
                            set.Id,
                            set.SortOrder,
                            set.Kind,
                            set.WeightKg,
                            set.Repetitions,
                            set.DurationSeconds,
                            set.CompletedAtUtc))
                        .ToList()))
                .ToList());

    private static WorkoutCompletionSummaryDto ToCompletionSummary(WorkoutSession session)
    {
        var completedExercises = session.Exercises
            .Where(exercise => exercise.Sets.Any(set => set.IsCompleted))
            .ToList();
        var performance = completedExercises
            .Select(ToPerformanceSummary)
            .ToList();

        return new WorkoutCompletionSummaryDto(
            session.Id,
            session.NameSnapshot,
            session.CompletedAtUtc!.Value - session.StartedAtUtc,
            session.Exercises.Count,
            session.Exercises.Count(exercise => exercise.Sets.Any(set => set.IsCompleted)),
            completedExercises.Sum(exercise => exercise.Sets.Count(set => set.IsCompleted && set.Kind == WorkoutSetKind.Working)),
            session.SessionFeeling,
            performance);
    }

    private static WorkoutPerformanceSummaryDto ToPerformanceSummary(WorkoutSessionExercise exercise)
    {
        var workingSets = exercise.Sets
            .Where(set => set.IsCompleted && set.Kind == WorkoutSetKind.Working)
            .ToList();

        if (workingSets.Count == 0)
        {
            return new WorkoutPerformanceSummaryDto(
                exercise.Id,
                exercise.ExerciseNameSnapshot,
                exercise.LoggingModeSnapshot,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        decimal? externalLoadTimesReps = null;
        decimal? addedWeightTimesReps = null;
        decimal? assistanceWeight = null;
        decimal? bestWeight = null;
        int? repetitions = null;
        int? durationSeconds = null;

        switch (exercise.LoggingModeSnapshot)
        {
            case ExerciseLoggingMode.WeightAndReps:
                externalLoadTimesReps = workingSets.Sum(set => set.WeightKg!.Value * set.Repetitions!.Value);
                repetitions = workingSets.Sum(set => set.Repetitions!.Value);
                break;
            case ExerciseLoggingMode.AddedWeightAndReps:
                addedWeightTimesReps = workingSets.Sum(set => set.WeightKg!.Value * set.Repetitions!.Value);
                repetitions = workingSets.Sum(set => set.Repetitions!.Value);
                break;
            case ExerciseLoggingMode.BodyweightAndReps:
            case ExerciseLoggingMode.RepsOnly:
                repetitions = workingSets.Sum(set => set.Repetitions!.Value);
                break;
            case ExerciseLoggingMode.Duration:
                durationSeconds = workingSets.Sum(set => set.DurationSeconds!.Value);
                break;
            case ExerciseLoggingMode.WeightAndDuration:
                bestWeight = workingSets.Count == 0 ? null : workingSets.Max(set => set.WeightKg!.Value);
                durationSeconds = workingSets.Sum(set => set.DurationSeconds!.Value);
                break;
            case ExerciseLoggingMode.AssistedWeightAndReps:
                assistanceWeight = workingSets.Count == 0 ? null : workingSets.Max(set => set.WeightKg!.Value);
                repetitions = workingSets.Sum(set => set.Repetitions!.Value);
                break;
        }

        return new WorkoutPerformanceSummaryDto(
            exercise.Id,
            exercise.ExerciseNameSnapshot,
            exercise.LoggingModeSnapshot,
            externalLoadTimesReps,
            addedWeightTimesReps,
            assistanceWeight,
            bestWeight,
            repetitions,
            durationSeconds);
    }

}
