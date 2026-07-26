using System;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class FrontFacingCharacterPreflight
    {
        public static FrontFacingCharacterSheetValidationResult Validate(string sourceRoot, string runtimePath) =>
            FrontFacingCharacterSheetContract.Validate(sourceRoot, runtimePath);

        public static void ValidateFromCommandLine()
        {
            var sourceRoot = CommandLineValue("-frontFacingSourceRoot");
            var runtimePath = CommandLineValue("-frontFacingRuntimePath");
            var result = Validate(sourceRoot, runtimePath);
            if (result.Errors.Count == 0)
            {
                Debug.Log("Front-facing character preflight passed.");
                return;
            }

            Debug.LogError("Front-facing character preflight failed: " + string.Join("; ", result.Errors));
            EditorApplication.Exit(1);
        }

        private static string CommandLineValue(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == name) return arguments[index + 1];
            return string.Empty;
        }
    }
}
