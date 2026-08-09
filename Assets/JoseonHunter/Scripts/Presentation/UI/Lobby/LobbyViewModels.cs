using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Presentation.UI.Lobby
{
    internal static class LobbyViewModels
    {
        internal static string WeaponName(WeaponId id)
        {
            if (id.Equals(WeaponId.HwandoFlyingBlade)) return "환도 비검";
            if (id.Equals(WeaponId.GakgungShot)) return "각궁";
            if (id.Equals(WeaponId.TalismanThrow)) return "주술 부적";
            if (id.Equals(WeaponId.ThunderCrashBomb)) return "벽력탄";
            if (id.Equals(WeaponId.JangseungWard)) return "장승진";
            if (id.Equals(WeaponId.SingijeonVolley)) return "신기전";
            if (id.Equals(WeaponId.FrostFlask)) return "서리병";
            return "풍뢰선";
        }

        internal static string DifficultyName(StageDifficulty difficulty) => difficulty switch
        {
            StageDifficulty.Normal => "보통",
            StageDifficulty.Omen => "흉조",
            StageDifficulty.GreatOmen => "대흉",
            _ => "알 수 없는 난이도"
        };

        internal static string TrainingName(CommonTrainingId id) => id switch
        {
            CommonTrainingId.Vitality => "활력",
            CommonTrainingId.Power => "완력",
            CommonTrainingId.Footwork => "보법",
            CommonTrainingId.Learning => "학습",
            CommonTrainingId.Guard => "수호",
            _ => "공명"
        };

        internal static string TrainingEffect(CommonTrainingId id) => id switch
        {
            CommonTrainingId.Vitality => "최대 체력",
            CommonTrainingId.Power => "무기 피해",
            CommonTrainingId.Footwork => "이동 속도",
            CommonTrainingId.Learning => "경험치 획득",
            CommonTrainingId.Guard => "받는 피해 감소",
            _ => "획득 범위"
        };
    }
}
