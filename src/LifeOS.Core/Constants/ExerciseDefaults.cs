using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Constants;

public static class ExerciseDefaults
{
    public static IReadOnlyList<ExerciseDefinition> Definitions { get; } =
    [
        D("10000000-0000-4000-8000-000000000001", "Barbell Bench Press", MuscleGroup.Chest, ExerciseEquipment.Barbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 1, MuscleGroup.Triceps, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000002", "Incline Barbell Bench Press", MuscleGroup.Chest, ExerciseEquipment.Barbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 2, MuscleGroup.Shoulders, MuscleGroup.Triceps),
        D("10000000-0000-4000-8000-000000000003", "Dumbbell Bench Press", MuscleGroup.Chest, ExerciseEquipment.Dumbbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 3, MuscleGroup.Triceps, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000004", "Incline Dumbbell Bench Press", MuscleGroup.Chest, ExerciseEquipment.Dumbbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 4, MuscleGroup.Shoulders, MuscleGroup.Triceps),
        D("10000000-0000-4000-8000-000000000005", "Machine Chest Press", MuscleGroup.Chest, ExerciseEquipment.Machine, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 5, MuscleGroup.Triceps, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000006", "Cable Fly", MuscleGroup.Chest, ExerciseEquipment.Cable, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 6, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000007", "Push-Up", MuscleGroup.Chest, ExerciseEquipment.Bodyweight, MovementPattern.HorizontalPush, ExerciseLoggingMode.BodyweightAndReps, 7, MuscleGroup.Triceps, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000008", "Weighted Push-Up", MuscleGroup.Chest, ExerciseEquipment.Other, MovementPattern.HorizontalPush, ExerciseLoggingMode.AddedWeightAndReps, 8, MuscleGroup.Triceps, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000009", "Decline Bench Press", MuscleGroup.Chest, ExerciseEquipment.Barbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 9, MuscleGroup.Triceps),
        D("10000000-0000-4000-8000-000000000010", "Dumbbell Pullover", MuscleGroup.Chest, ExerciseEquipment.Dumbbell, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 10, MuscleGroup.BackLats),

        D("10000000-0000-4000-8000-000000000011", "Barbell Row", MuscleGroup.BackLats, ExerciseEquipment.Barbell, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 11, MuscleGroup.Biceps, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000012", "Dumbbell Row", MuscleGroup.BackLats, ExerciseEquipment.Dumbbell, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 12, MuscleGroup.Biceps),
        D("10000000-0000-4000-8000-000000000013", "Seated Cable Row", MuscleGroup.BackLats, ExerciseEquipment.Cable, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 13, MuscleGroup.Biceps, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000014", "Lat Pulldown", MuscleGroup.BackLats, ExerciseEquipment.Cable, MovementPattern.VerticalPull, ExerciseLoggingMode.WeightAndReps, 14, MuscleGroup.Biceps, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000015", "Pull-Up", MuscleGroup.BackLats, ExerciseEquipment.PullUpBar, MovementPattern.VerticalPull, ExerciseLoggingMode.BodyweightAndReps, 15, MuscleGroup.Biceps, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000016", "Chin-Up", MuscleGroup.BackLats, ExerciseEquipment.PullUpBar, MovementPattern.VerticalPull, ExerciseLoggingMode.BodyweightAndReps, 16, MuscleGroup.Biceps),
        D("10000000-0000-4000-8000-000000000017", "Weighted Pull-Up", MuscleGroup.BackLats, ExerciseEquipment.PullUpBar, MovementPattern.VerticalPull, ExerciseLoggingMode.AddedWeightAndReps, 17, MuscleGroup.Biceps, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000018", "Assisted Pull-Up", MuscleGroup.BackLats, ExerciseEquipment.PullUpBar, MovementPattern.VerticalPull, ExerciseLoggingMode.AssistedWeightAndReps, 18, MuscleGroup.Biceps),
        D("10000000-0000-4000-8000-000000000019", "Chest-Supported Row", MuscleGroup.BackLats, ExerciseEquipment.Machine, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 19, MuscleGroup.Biceps),
        D("10000000-0000-4000-8000-000000000020", "Single-Arm Cable Row", MuscleGroup.BackLats, ExerciseEquipment.Cable, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 20, MuscleGroup.Biceps),
        D("10000000-0000-4000-8000-000000000021", "Inverted Row", MuscleGroup.BackLats, ExerciseEquipment.Bodyweight, MovementPattern.HorizontalPull, ExerciseLoggingMode.BodyweightAndReps, 21, MuscleGroup.Biceps, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000022", "Straight-Arm Pulldown", MuscleGroup.BackLats, ExerciseEquipment.Cable, MovementPattern.VerticalPull, ExerciseLoggingMode.WeightAndReps, 22, MuscleGroup.Core),

        D("10000000-0000-4000-8000-000000000023", "Overhead Press", MuscleGroup.Shoulders, ExerciseEquipment.Barbell, MovementPattern.VerticalPush, ExerciseLoggingMode.WeightAndReps, 23, MuscleGroup.Triceps, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000024", "Dumbbell Shoulder Press", MuscleGroup.Shoulders, ExerciseEquipment.Dumbbell, MovementPattern.VerticalPush, ExerciseLoggingMode.WeightAndReps, 24, MuscleGroup.Triceps),
        D("10000000-0000-4000-8000-000000000025", "Lateral Raise", MuscleGroup.Shoulders, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 25, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000026", "Cable Lateral Raise", MuscleGroup.Shoulders, ExerciseEquipment.Cable, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 26),
        D("10000000-0000-4000-8000-000000000027", "Rear Delt Fly", MuscleGroup.Shoulders, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 27, MuscleGroup.BackLats),
        D("10000000-0000-4000-8000-000000000028", "Face Pull", MuscleGroup.Shoulders, ExerciseEquipment.Cable, MovementPattern.HorizontalPull, ExerciseLoggingMode.WeightAndReps, 28, MuscleGroup.BackLats),
        D("10000000-0000-4000-8000-000000000029", "Pike Push-Up", MuscleGroup.Shoulders, ExerciseEquipment.Bodyweight, MovementPattern.VerticalPush, ExerciseLoggingMode.BodyweightAndReps, 29, MuscleGroup.Triceps, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000030", "Handstand Push-Up", MuscleGroup.Shoulders, ExerciseEquipment.Bodyweight, MovementPattern.Skill, ExerciseLoggingMode.BodyweightAndReps, 30, MuscleGroup.Triceps, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000031", "Arnold Press", MuscleGroup.Shoulders, ExerciseEquipment.Dumbbell, MovementPattern.VerticalPush, ExerciseLoggingMode.WeightAndReps, 31, MuscleGroup.Triceps),

        D("10000000-0000-4000-8000-000000000032", "Back Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Barbell, MovementPattern.Squat, ExerciseLoggingMode.WeightAndReps, 32, MuscleGroup.Glutes, MuscleGroup.Hamstrings),
        D("10000000-0000-4000-8000-000000000033", "Front Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Barbell, MovementPattern.Squat, ExerciseLoggingMode.WeightAndReps, 33, MuscleGroup.Glutes, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000034", "Goblet Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Dumbbell, MovementPattern.Squat, ExerciseLoggingMode.WeightAndReps, 34, MuscleGroup.Glutes, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000035", "Bodyweight Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Bodyweight, MovementPattern.Squat, ExerciseLoggingMode.RepsOnly, 35, MuscleGroup.Glutes),
        D("10000000-0000-4000-8000-000000000036", "Leg Press", MuscleGroup.Quadriceps, ExerciseEquipment.Machine, MovementPattern.Squat, ExerciseLoggingMode.WeightAndReps, 36, MuscleGroup.Glutes, MuscleGroup.Hamstrings),
        D("10000000-0000-4000-8000-000000000037", "Romanian Deadlift", MuscleGroup.Hamstrings, ExerciseEquipment.Barbell, MovementPattern.Hinge, ExerciseLoggingMode.WeightAndReps, 37, MuscleGroup.Glutes, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000038", "Conventional Deadlift", MuscleGroup.Hamstrings, ExerciseEquipment.Barbell, MovementPattern.Hinge, ExerciseLoggingMode.WeightAndReps, 38, MuscleGroup.Glutes, MuscleGroup.BackLats),
        D("10000000-0000-4000-8000-000000000039", "Hip Thrust", MuscleGroup.Glutes, ExerciseEquipment.Barbell, MovementPattern.Hinge, ExerciseLoggingMode.WeightAndReps, 39, MuscleGroup.Hamstrings),
        D("10000000-0000-4000-8000-000000000040", "Bulgarian Split Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Dumbbell, MovementPattern.Lunge, ExerciseLoggingMode.WeightAndReps, 40, MuscleGroup.Glutes, MuscleGroup.Hamstrings),
        D("10000000-0000-4000-8000-000000000041", "Walking Lunge", MuscleGroup.Quadriceps, ExerciseEquipment.Dumbbell, MovementPattern.Lunge, ExerciseLoggingMode.WeightAndReps, 41, MuscleGroup.Glutes, MuscleGroup.Hamstrings),
        D("10000000-0000-4000-8000-000000000042", "Reverse Lunge", MuscleGroup.Quadriceps, ExerciseEquipment.Bodyweight, MovementPattern.Lunge, ExerciseLoggingMode.BodyweightAndReps, 42, MuscleGroup.Glutes),
        D("10000000-0000-4000-8000-000000000043", "Step-Up", MuscleGroup.Quadriceps, ExerciseEquipment.Dumbbell, MovementPattern.Lunge, ExerciseLoggingMode.WeightAndReps, 43, MuscleGroup.Glutes),
        D("10000000-0000-4000-8000-000000000044", "Leg Extension", MuscleGroup.Quadriceps, ExerciseEquipment.Machine, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 44),
        D("10000000-0000-4000-8000-000000000045", "Seated Leg Curl", MuscleGroup.Hamstrings, ExerciseEquipment.Machine, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 45),
        D("10000000-0000-4000-8000-000000000046", "Standing Leg Curl", MuscleGroup.Hamstrings, ExerciseEquipment.Machine, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 46),
        D("10000000-0000-4000-8000-000000000047", "Standing Calf Raise", MuscleGroup.Calves, ExerciseEquipment.Machine, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 47),
        D("10000000-0000-4000-8000-000000000048", "Seated Calf Raise", MuscleGroup.Calves, ExerciseEquipment.Machine, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 48),
        D("10000000-0000-4000-8000-000000000049", "Kettlebell Goblet Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Kettlebell, MovementPattern.Squat, ExerciseLoggingMode.WeightAndReps, 49, MuscleGroup.Glutes),
        D("10000000-0000-4000-8000-000000000050", "Kettlebell Swing", MuscleGroup.Glutes, ExerciseEquipment.Kettlebell, MovementPattern.Hinge, ExerciseLoggingMode.WeightAndReps, 50, MuscleGroup.Hamstrings, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000051", "Good Morning", MuscleGroup.Hamstrings, ExerciseEquipment.Barbell, MovementPattern.Hinge, ExerciseLoggingMode.WeightAndReps, 51, MuscleGroup.Glutes, MuscleGroup.BackLats),
        D("10000000-0000-4000-8000-000000000052", "Barbell Hip Thrust", MuscleGroup.Glutes, ExerciseEquipment.Barbell, MovementPattern.Hinge, ExerciseLoggingMode.WeightAndReps, 52, MuscleGroup.Hamstrings),
        D("10000000-0000-4000-8000-000000000053", "Bodyweight Calf Raise", MuscleGroup.Calves, ExerciseEquipment.Bodyweight, MovementPattern.Isolation, ExerciseLoggingMode.RepsOnly, 53),
        D("10000000-0000-4000-8000-000000000054", "Cossack Squat", MuscleGroup.Quadriceps, ExerciseEquipment.Bodyweight, MovementPattern.Squat, ExerciseLoggingMode.RepsOnly, 54, MuscleGroup.Glutes, MuscleGroup.Hamstrings),

        D("10000000-0000-4000-8000-000000000055", "Barbell Curl", MuscleGroup.Biceps, ExerciseEquipment.Barbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 55, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000056", "Dumbbell Curl", MuscleGroup.Biceps, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 56, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000057", "Hammer Curl", MuscleGroup.Biceps, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 57, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000058", "Cable Curl", MuscleGroup.Biceps, ExerciseEquipment.Cable, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 58, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000059", "Incline Dumbbell Curl", MuscleGroup.Biceps, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 59, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000060", "Triceps Pushdown", MuscleGroup.Triceps, ExerciseEquipment.Cable, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 60, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000061", "Skull Crusher", MuscleGroup.Triceps, ExerciseEquipment.Barbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 61, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000062", "Overhead Triceps Extension", MuscleGroup.Triceps, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 62, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000063", "Dip", MuscleGroup.Triceps, ExerciseEquipment.DipBars, MovementPattern.VerticalPush, ExerciseLoggingMode.BodyweightAndReps, 63, MuscleGroup.Chest, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000064", "Weighted Dip", MuscleGroup.Triceps, ExerciseEquipment.DipBars, MovementPattern.VerticalPush, ExerciseLoggingMode.AddedWeightAndReps, 64, MuscleGroup.Chest, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000065", "Assisted Dip", MuscleGroup.Triceps, ExerciseEquipment.DipBars, MovementPattern.VerticalPush, ExerciseLoggingMode.AssistedWeightAndReps, 65, MuscleGroup.Chest),
        D("10000000-0000-4000-8000-000000000066", "Close-Grip Bench Press", MuscleGroup.Triceps, ExerciseEquipment.Barbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, 66, MuscleGroup.Chest, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000067", "Wrist Curl", MuscleGroup.Forearms, ExerciseEquipment.Dumbbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 67),
        D("10000000-0000-4000-8000-000000000068", "Reverse Curl", MuscleGroup.Forearms, ExerciseEquipment.Barbell, MovementPattern.Isolation, ExerciseLoggingMode.WeightAndReps, 68, MuscleGroup.Biceps),
        D("10000000-0000-4000-8000-000000000069", "Bodyweight Triceps Extension", MuscleGroup.Triceps, ExerciseEquipment.Bodyweight, MovementPattern.HorizontalPush, ExerciseLoggingMode.BodyweightAndReps, 69, MuscleGroup.Chest),
        D("10000000-0000-4000-8000-000000000070", "Ring Dip", MuscleGroup.Triceps, ExerciseEquipment.Other, MovementPattern.VerticalPush, ExerciseLoggingMode.BodyweightAndReps, 70, MuscleGroup.Chest, MuscleGroup.Shoulders),

        D("10000000-0000-4000-8000-000000000071", "Plank", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.Duration, 71, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000072", "Side Plank", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.Duration, 72, MuscleGroup.Glutes),
        D("10000000-0000-4000-8000-000000000073", "Hanging Knee Raise", MuscleGroup.Core, ExerciseEquipment.PullUpBar, MovementPattern.Core, ExerciseLoggingMode.BodyweightAndReps, 73, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000074", "Hanging Leg Raise", MuscleGroup.Core, ExerciseEquipment.PullUpBar, MovementPattern.Core, ExerciseLoggingMode.BodyweightAndReps, 74, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000075", "Ab Wheel Rollout", MuscleGroup.Core, ExerciseEquipment.Other, MovementPattern.Core, ExerciseLoggingMode.RepsOnly, 75, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000076", "Crunch", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.RepsOnly, 76),
        D("10000000-0000-4000-8000-000000000077", "Bicycle Crunch", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.RepsOnly, 77),
        D("10000000-0000-4000-8000-000000000078", "L-Sit", MuscleGroup.Core, ExerciseEquipment.DipBars, MovementPattern.Skill, ExerciseLoggingMode.Duration, 78, MuscleGroup.Shoulders, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000079", "Russian Twist", MuscleGroup.Core, ExerciseEquipment.Other, MovementPattern.Core, ExerciseLoggingMode.RepsOnly, 79, MuscleGroup.Shoulders),
        D("10000000-0000-4000-8000-000000000080", "Dead Bug", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.RepsOnly, 80),

        D("10000000-0000-4000-8000-000000000081", "Farmer's Carry", MuscleGroup.FullBody, ExerciseEquipment.Dumbbell, MovementPattern.Carry, ExerciseLoggingMode.WeightAndDuration, 81, MuscleGroup.Forearms, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000082", "Suitcase Carry", MuscleGroup.Core, ExerciseEquipment.Dumbbell, MovementPattern.Carry, ExerciseLoggingMode.WeightAndDuration, 82, MuscleGroup.Forearms),
        D("10000000-0000-4000-8000-000000000083", "Bear Crawl", MuscleGroup.FullBody, ExerciseEquipment.Bodyweight, MovementPattern.FullBody, ExerciseLoggingMode.Duration, 83, MuscleGroup.Shoulders, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000084", "Burpee", MuscleGroup.FullBody, ExerciseEquipment.Bodyweight, MovementPattern.FullBody, ExerciseLoggingMode.RepsOnly, 84, MuscleGroup.Chest, MuscleGroup.Quadriceps),
        D("10000000-0000-4000-8000-000000000085", "Mountain Climber", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.RepsOnly, 85, MuscleGroup.Shoulders, MuscleGroup.Quadriceps),
        D("10000000-0000-4000-8000-000000000086", "Kettlebell Clean", MuscleGroup.FullBody, ExerciseEquipment.Kettlebell, MovementPattern.FullBody, ExerciseLoggingMode.WeightAndReps, 86, MuscleGroup.Shoulders, MuscleGroup.Glutes),
        D("10000000-0000-4000-8000-000000000087", "Kettlebell Turkish Get-Up", MuscleGroup.FullBody, ExerciseEquipment.Kettlebell, MovementPattern.Skill, ExerciseLoggingMode.WeightAndReps, 87, MuscleGroup.Shoulders, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000088", "Resistance Band Pull-Apart", MuscleGroup.Shoulders, ExerciseEquipment.ResistanceBand, MovementPattern.HorizontalPull, ExerciseLoggingMode.RepsOnly, 88, MuscleGroup.BackLats),
        D("10000000-0000-4000-8000-000000000089", "Battle Rope", MuscleGroup.FullBody, ExerciseEquipment.Other, MovementPattern.FullBody, ExerciseLoggingMode.Duration, 89, MuscleGroup.Shoulders, MuscleGroup.Core),
        D("10000000-0000-4000-8000-000000000090", "Box Jump", MuscleGroup.Quadriceps, ExerciseEquipment.Other, MovementPattern.Skill, ExerciseLoggingMode.RepsOnly, 90, MuscleGroup.Glutes, MuscleGroup.Calves)
    ];

    public static IReadOnlyList<Exercise> All { get; } =
        Definitions.Select(definition => new Exercise(
            definition.Id,
            definition.Name,
            definition.PrimaryMuscleGroup,
            definition.Equipment,
            definition.MovementPattern,
            definition.LoggingMode,
            definition.IsActive,
            definition.SortOrder,
            definition.SecondaryMuscleGroups)).ToArray();

    private static ExerciseDefinition D(
        string id,
        string name,
        MuscleGroup primaryMuscleGroup,
        ExerciseEquipment equipment,
        MovementPattern movementPattern,
        ExerciseLoggingMode loggingMode,
        int sortOrder,
        params MuscleGroup[] secondaryMuscleGroups) =>
        new(Guid.Parse(id), name, primaryMuscleGroup, equipment, movementPattern, loggingMode, true, sortOrder, secondaryMuscleGroups);
}

public sealed record ExerciseDefinition(
    Guid Id,
    string Name,
    MuscleGroup PrimaryMuscleGroup,
    ExerciseEquipment Equipment,
    MovementPattern MovementPattern,
    ExerciseLoggingMode LoggingMode,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<MuscleGroup> SecondaryMuscleGroups);
