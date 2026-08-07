namespace JoseonHunter.Runtime.Audio
{
    public enum GameAudioCueId
    {
        None = 0,
        UiClick = 1,
        UiConfirm = 2,
        UiCancel = 3,
        ExperiencePickup = 10,
        YeopjeonPickup = 11,
        MagnetPickup = 12,
        LevelUp = 13,
        UpgradeSelected = 14,
        Gakgung = 20,
        Hwando = 21,
        ThunderBomb = 22,
        FrostFlask = 23,
        WindThunderFan = 24,
        Talisman = 25,
        Jangseung = 26,
        Geumjul = 27,
        Singijeon = 28,
        NormalHit = 40,
        CriticalHit = 41,
        PlayerHurt = 42,
        PlayerDefeat = 43,
        EliteDefeat = 44,
        BossWarning = 50,
        BossAppear = 51,
        BossDefeat = 52,
        Victory = 53,
        Defeat = 54,
        BossSlam = 55,
        BossCharge = 56,
        BossVolley = 57,
        TreasureAppear = 60,
        TreasureOpen = 61,
        WaveWarning = 62,
        EliteAppear = 63,
        PauseOpen = 70,
        AppraisalTick = 71,
        AppraisalReveal = 72
    }

    public enum GameAudioPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}
