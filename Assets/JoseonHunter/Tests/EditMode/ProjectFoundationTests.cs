using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class ProjectFoundationTests
    {
        [Test]
        public void ProjectUsesUrp2DAndInputSystemOnly()
        {
            Assert.That(
                GraphicsSettings.defaultRenderPipeline,
                Is.InstanceOf<UniversalRenderPipelineAsset>());

            var settings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            StringAssert.Contains("activeInputHandler: 1", settings);
        }

        [Test]
        public void AndroidPlayerUsesPortraitOrientation()
        {
            Assert.That(
                PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
        }
    }
}
