using TMPro;
using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public enum RuntimeFontRole
    {
        Body,
        BodyEmphasis,
        Title,
        Damage
    }

    public static class RuntimeFontCatalog
    {
        private const string FallbackPath = "Fonts/NotoSansKR-Dynamic SDF";
        private static readonly TMP_FontAsset[] Fonts = new TMP_FontAsset[4];

        public static TMP_FontAsset For(RuntimeFontRole role)
        {
            var index = (int)role;
            if (Fonts[index] != null)
                return Fonts[index];

            var path = PathFor(role);
            Fonts[index] = Resources.Load<TMP_FontAsset>(path);
            if (Fonts[index] == null)
            {
                Debug.LogError($"Missing runtime font for {role} at Resources/{path}.");
                Fonts[index] = Resources.Load<TMP_FontAsset>(FallbackPath);
            }

            return Fonts[index];
        }

        private static string PathFor(RuntimeFontRole role)
        {
            switch (role)
            {
                case RuntimeFontRole.Title: return "Fonts/ChosunGs-Dynamic SDF";
                case RuntimeFontRole.BodyEmphasis: return "Fonts/MaruBuri-SemiBold-Dynamic SDF";
                case RuntimeFontRole.Damage: return "Fonts/BlackAndWhitePicture-Dynamic SDF";
                default: return "Fonts/MaruBuri-Regular-Dynamic SDF";
            }
        }
    }
}
