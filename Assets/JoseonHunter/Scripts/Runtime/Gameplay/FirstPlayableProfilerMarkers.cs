using Unity.Profiling;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class FirstPlayableProfilerMarkers
    {
        public const string RunUpdateName = "JoseonHunter.Run.Update";
        public const string EnemyGridName = "JoseonHunter.Enemy.Grid";
        public const string EnemyMoveName = "JoseonHunter.Enemy.Move";
        public const string SpawnName = "JoseonHunter.Spawn";
        public const string WeaponName = "JoseonHunter.Weapon";
        public const string PickupName = "JoseonHunter.Pickup";
        public const string UiHudName = "JoseonHunter.UI.Hud";
        public const string UiModalName = "JoseonHunter.UI.Modal";

        public static readonly ProfilerMarker RunUpdate = new ProfilerMarker(RunUpdateName);
        public static readonly ProfilerMarker EnemyGrid = new ProfilerMarker(EnemyGridName);
        public static readonly ProfilerMarker EnemyMove = new ProfilerMarker(EnemyMoveName);
        public static readonly ProfilerMarker Spawn = new ProfilerMarker(SpawnName);
        public static readonly ProfilerMarker Weapon = new ProfilerMarker(WeaponName);
        public static readonly ProfilerMarker Pickup = new ProfilerMarker(PickupName);
        public static readonly ProfilerMarker UiHud = new ProfilerMarker(UiHudName);
        public static readonly ProfilerMarker UiModal = new ProfilerMarker(UiModalName);
    }
}
