using Npgsql;

namespace LifeOS.Tests.Infrastructure;

public sealed class DeveloperDatabaseM8VerificationTests
{
    private const string ConnectionString = "Host=localhost;Port=5433;Database=lifeos;Username=lifeos;Password=password";

    [Fact]
    public async Task DeveloperDatabase_ShouldContainAppliedM8SchemaAndSeeds()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        Assert.Equal(
            "20260909195028_AddStrengthTraining",
            await ScalarString(connection, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1;"));
        Assert.Equal(9, await ScalarInt(connection, "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";"));
        Assert.Equal(90, await ScalarInt(connection, "SELECT COUNT(*) FROM \"Exercises\";"));
        Assert.Equal(119, await ScalarInt(connection, "SELECT COUNT(*) FROM \"ExerciseSecondaryMuscleGroups\";"));
        Assert.Equal(
            "10000000-0000-4000-8000-000000000001",
            await ScalarString(connection, "SELECT \"Id\"::text FROM \"Exercises\" WHERE \"Name\" = 'Barbell Bench Press';"));
        Assert.Equal(
            "10000000-0000-4000-8000-000000000015",
            await ScalarString(connection, "SELECT \"Id\"::text FROM \"Exercises\" WHERE \"Name\" = 'Pull-Up';"));
        Assert.Equal(0, await ScalarInt(connection, "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'ExerciseSecondaryMuscleGroups' AND column_name = 'ExerciseId1';"));

        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_indexes WHERE indexname = 'IX_WorkoutSessions_UserId' AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%Status% = 0%' AND indexdef LIKE '%IsDeleted%false%';"));
        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_indexes WHERE indexname = 'IX_WorkoutTemplateExercises_WorkoutTemplateId_SortOrder' AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%IsDeleted%false%';"));
        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_indexes WHERE indexname = 'IX_WorkoutSessionExercises_WorkoutSessionId_SortOrder' AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%IsDeleted%false%';"));
        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_indexes WHERE indexname = 'IX_WorkoutSets_WorkoutSessionExerciseId_SortOrder' AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%IsDeleted%false%';"));
        Assert.Equal("numeric", await ScalarString(connection, "SELECT data_type FROM information_schema.columns WHERE table_name = 'WorkoutSets' AND column_name = 'WeightKg';"));
        Assert.Equal(18, await ScalarInt(connection, "SELECT numeric_precision FROM information_schema.columns WHERE table_name = 'WorkoutSets' AND column_name = 'WeightKg';"));
        Assert.Equal(2, await ScalarInt(connection, "SELECT numeric_scale FROM information_schema.columns WHERE table_name = 'WorkoutSets' AND column_name = 'WeightKg';"));

        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_constraint WHERE conname LIKE 'FK_WorkoutTemplateExercises_WorkoutTemplates%';"));
        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_constraint WHERE conname LIKE 'FK_WorkoutSessionExercises_WorkoutSessions%';"));
        Assert.Equal(1, await ScalarInt(connection, "SELECT COUNT(*) FROM pg_constraint WHERE conname LIKE 'FK_WorkoutSets_WorkoutSessionExercises%';"));
    }

    private static async Task<int> ScalarInt(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<string?> ScalarString(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync());
    }
}
