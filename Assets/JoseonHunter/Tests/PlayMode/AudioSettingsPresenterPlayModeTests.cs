using System;
using System.Linq;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class AudioSettingsPresenterPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
            if (GameMusicDirector.Instance != null) Object.DestroyImmediate(GameMusicDirector.Instance.gameObject);
            if (GameAudioDirector.Instance != null) Object.DestroyImmediate(GameAudioDirector.Instance.gameObject);
        }

        [Test]
        public void ReinitializingAuthoredViewPreservesExternalListenersAndBindsOneOwnedListener()
        {
            var data = SaveDataV1.CreateDefaults();
            data.MusicVolume = .2f;
            data.SoundEffectVolume = .3f;
            var repository = new RecordingRepository(data);
            var session = MetaGameSession.EnsureExists(repository);
            var root = new GameObject("Authored Audio Settings", typeof(RectTransform));
            var view = CreateCompleteView(root.transform);
            var presenter = root.AddComponent<AudioSettingsPresenter>();
            var externalMusicChanges = 0;
            var externalCloseRequests = 0;
            var closeRequests = 0;
            view.MusicSlider.onValueChanged.AddListener(_ => externalMusicChanges++);
            view.CloseButton.onClick.AddListener(() => externalCloseRequests++);
            presenter.CloseRequested += () => closeRequests++;
            var controlIds = Enumerable.Range(0, root.transform.childCount)
                .Select(index => root.transform.GetChild(index).gameObject.GetEntityId()).ToArray();
            var childCount = root.transform.childCount;

            presenter.InitializeAuthored(view, session);
            presenter.InitializeAuthored(view, session);
            view.MusicSlider.value = .73f;
            view.SoundEffectSlider.value = .41f;
            view.CloseButton.onClick.Invoke();
            presenter.CommitPending();

            Assert.That(externalMusicChanges, Is.EqualTo(1),
                "Reinitialization must not remove external slider listeners or duplicate the presenter's listener.");
            Assert.That(externalCloseRequests, Is.EqualTo(1),
                "Reinitialization must not remove external close listeners or duplicate the presenter's listener.");
            Assert.That(closeRequests, Is.EqualTo(1));
            Assert.That(GameMusicDirector.Instance.MasterVolume, Is.EqualTo(.73f).Within(.001f));
            Assert.That(GameAudioDirector.Instance.MasterVolume, Is.EqualTo(.41f).Within(.001f));
            Assert.That(repository.SaveCount, Is.EqualTo(1), "Close and a later explicit commit must persist once.");
            Assert.That(session.Data.MusicVolume, Is.EqualTo(.73f).Within(.001f));
            Assert.That(session.Data.SoundEffectVolume, Is.EqualTo(.41f).Within(.001f));
            Assert.That(root.transform.childCount, Is.EqualTo(childCount));
            var reinitializedControlIds = Enumerable.Range(0, root.transform.childCount)
                .Select(index => root.transform.GetChild(index).gameObject.GetEntityId()).ToArray();
            CollectionAssert.AreEqual(controlIds, reinitializedControlIds);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void InitializeAuthoredRejectsIncompleteViewBeforeBinding()
        {
            var presenter = new GameObject("Audio Settings Presenter").AddComponent<AudioSettingsPresenter>();
            var session = MetaGameSession.EnsureExists(new RecordingRepository(SaveDataV1.CreateDefaults()));
            var incompleteView = new GameObject("Incomplete Audio Settings View").AddComponent<LobbyAudioSettingsView>();

            Assert.That(() => presenter.InitializeAuthored(incompleteView, session),
                Throws.TypeOf<ArgumentException>());

            Object.DestroyImmediate(presenter.gameObject);
            Object.DestroyImmediate(incompleteView.gameObject);
        }

        private static LobbyAudioSettingsView CreateCompleteView(Transform parent)
        {
            var title = CreateText("Title", parent);
            var musicSlider = CreateSlider("Music Slider", parent);
            var soundEffectSlider = CreateSlider("Sound Effect Slider", parent);
            var musicValue = CreateText("Music Value", parent);
            var soundEffectValue = CreateText("Sound Effect Value", parent);
            var closeButton = CreateButton("Close", parent);
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dim.transform.SetParent(parent, false);
            var dialog = new GameObject("Dialog", typeof(RectTransform));
            dialog.transform.SetParent(parent, false);
            var view = parent.gameObject.AddComponent<LobbyAudioSettingsView>();
            view.Configure(title, musicSlider, soundEffectSlider, musicValue, soundEffectValue, closeButton,
                dim.GetComponent<Image>(), dialog.GetComponent<RectTransform>());
            return view;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            var slider = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider))
                .GetComponent<Slider>();
            slider.transform.SetParent(parent, false);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            return text;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button))
                .GetComponent<Button>();
            button.transform.SetParent(parent, false);
            return button;
        }

        private sealed class RecordingRepository : ISaveRepository
        {
            private SaveDataV1 stored;

            public RecordingRepository(SaveDataV1 data) => stored = data.Copy();
            public int SaveCount { get; private set; }
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                SaveCount++;
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
