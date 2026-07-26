namespace JoseonHunter.Editor.AssetImport
{
    [System.Serializable]
    public sealed class AssetMigrationManifest
    {
        public int version;
        public AssetMigrationEntry[] entries;
    }

    [System.Serializable]
    public sealed class AssetMigrationEntry
    {
        public string source;
        public string destination;
        public string profile;
        public string licenseStatus;
    }
}
