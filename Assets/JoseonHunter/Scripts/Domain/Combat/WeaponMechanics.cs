namespace JoseonHunter.Domain.Combat
{
    public enum WeaponTargeting
    {
        Nearest,
        HighestThreat,
        NearestUnmarked,
        DensestCenter,
        PlayerBoundary,
        DensestDirection,
        PredictedCrowd,
        DangerousSector
    }

    public enum WeaponGeometry
    {
        ReturningPath,
        NarrowLine,
        SequentialHop,
        ExpandingCircle,
        Boundary,
        MultiLane,
        PersistentCircle,
        ConeThenLinks
    }

    public enum ContactPhase
    {
        Outbound,
        Inbound,
        Direct,
        Attach,
        Seal,
        Blast,
        BoundaryCrossing,
        Tick,
        Wind,
        Lightning
    }

    public enum DamageElement
    {
        Physical,
        Magic,
        Fire,
        Ice,
        Lightning
    }

    public enum RepeatHitPolicy
    {
        OncePerInstance,
        OncePerPhase,
        TimedTicks,
        BoundaryReentry
    }
}
