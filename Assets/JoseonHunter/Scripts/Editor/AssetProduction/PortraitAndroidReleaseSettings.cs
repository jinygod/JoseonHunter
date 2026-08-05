using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class PortraitAndroidReleaseSettings
    {
        [MenuItem("Joseon Hunter/Asset Production/Apply Portrait Android Release Contract")]
        public static void ApplyPortraitAndroidReleaseContract()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 360;
            PlayerSettings.defaultScreenHeight = 640;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.jinygod.joseonhunter");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            AssetDatabase.SaveAssets();
        }
    }
}
