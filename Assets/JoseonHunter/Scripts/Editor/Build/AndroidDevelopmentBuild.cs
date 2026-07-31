using System.IO;
using System.Linq;
using JoseonHunter.Editor.AssetProduction;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace JoseonHunter.Editor.Build
{
    public static class AndroidDevelopmentBuild
    {
        public static void Build()
        {
            PortraitAndroidReleaseSettings.ApplyPortraitAndroidReleaseContract();
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            var output = Path.GetFullPath("Builds/Android/JoseonHunter-development.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development |
                          BuildOptions.ConnectWithProfiler |
                          BuildOptions.AllowDebugging
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(report.summary.result.ToString());
        }
    }
}
