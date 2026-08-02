using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Repositories;

public sealed class UserSettingsRepository : IUserSettingsRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UserSettingsRepository(
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<UserSettings?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        return await context.UserSettings
            .FirstOrDefaultAsync(
                settings => settings.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        UserSettings settings,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        await context.UserSettings.AddAsync(
            settings,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        UserSettings settings,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        context.UserSettings.Update(settings);

        await context.SaveChangesAsync(cancellationToken);
    }
}