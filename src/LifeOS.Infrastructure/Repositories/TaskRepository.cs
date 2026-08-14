using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public TaskRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<TaskItem?> GetByIdAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                task => task.Id == taskId && task.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .Where(task => task.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Tasks.Add(task);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Tasks.Update(task);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskCompletionWriteResult> CompleteAsync(
        Guid userId,
        Guid taskId,
        DateTimeOffset completedAtUtc,
        DateOnly completedDate,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var affectedRows = await context.Tasks
            .Where(task =>
                task.Id == taskId &&
                task.UserId == userId &&
                task.Status == TaskItemStatus.Active)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(task => task.Status, TaskItemStatus.Completed)
                    .SetProperty(task => task.CompletedAtUtc, completedAtUtc)
                    .SetProperty(task => task.CompletedDate, completedDate)
                    .SetProperty(task => task.UpdatedAtUtc, completedAtUtc),
                cancellationToken);

        var authoritative = await context.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                task => task.Id == taskId && task.UserId == userId,
                cancellationToken);

        if (affectedRows == 1 && authoritative is not null)
        {
            return new TaskCompletionWriteResult
            {
                Status = TaskCompletionWriteStatus.NewlyCompleted,
                Task = authoritative
            };
        }

        if (authoritative is null)
        {
            return new TaskCompletionWriteResult
            {
                Status = TaskCompletionWriteStatus.NotFound
            };
        }

        return new TaskCompletionWriteResult
        {
            Status = authoritative.Status switch
            {
                TaskItemStatus.Completed => TaskCompletionWriteStatus.AlreadyCompleted,
                TaskItemStatus.Archived => TaskCompletionWriteStatus.Archived,
                _ => TaskCompletionWriteStatus.NotFound
            },
            Task = authoritative
        };
    }

    public async Task DeleteAsync(
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Tasks.Remove(task);

        await context.SaveChangesAsync(cancellationToken);
    }
}