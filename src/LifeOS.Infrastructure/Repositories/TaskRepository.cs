using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Entities;
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