using System;

namespace JoseonHunter.Editor.AssetProduction
{
    [Serializable]
    public sealed class ProductionAssetManifest
    {
        public int schemaVersion;
        public ProductionAssetEntry[] assets;
    }

    [Serializable]
    public sealed class ProductionAssetEntry
    {
        public string id;
        public string batch;
        public string kind;
        public string sourcePath;
        public string runtimePath;
        public int width;
        public int height;
        public int frameCount;
        public float pivotX;
        public float pivotY;
        public int pixelsPerUnit;
        public string sha256;
        public string licenseStatus;
        public string approvalStatus;
        public string promptRevision;
    }
}
