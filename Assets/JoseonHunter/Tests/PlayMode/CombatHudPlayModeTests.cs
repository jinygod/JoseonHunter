using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatHudPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [Test]
        public void CombatHudUsesOnlyKoreanLabelsForPlayerFacingStats()
        {
            var root = new GameObject("Korean HUD Test");
            var presenter = root.AddComponent<CombatHudPresenter>();
            presenter.Render(new FirstPlayableUiState(3, 4, 12, 7, 21,
                10f, 180f, 85f, 100f, true, true, 90f, 120f,
                System.Array.Empty<WeaponSlotView>()));

            Assert.That(TextNamed(root, "Level"), Is.EqualTo("레벨 3"));
            Assert.That(TextNamed(root, "Health"), Does.StartWith("체력 "));
            Assert.That(TextNamed(root, "Experience"), Does.StartWith("경험치 "));
            Assert.That(TextNamed(root, "Experience"), Does.Contain("엽전 7"));
            Assert.That(TextNamed(root, "Kills"), Is.EqualTo("처치 21"));
            Assert.That(TextNamed(root, "Boss Warning"), Is.EqualTo("강한 기운이 다가옵니다"));
            Assert.That(TextNamed(root, "Boss Label"), Does.StartWith("우두머리 "));
            Assert.That(TextNamed(root, "Return Label"), Is.EqualTo("귀환"));
            Assert.That(AllText(root), Does.Not.Match("HP|XP|COIN|KILLS|BOSS|DREADFUL"));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RunResultIsOpaqueKoreanCanvasAndRequestsLobbyReturn()
        {
            var root = new GameObject("Korean Result Test");
            var presenter = root.AddComponent<RunResultPresenter>();
            var returns = 0;
            presenter.LobbyReturnRequested += () => returns++;
            presenter.Render(new FirstPlayableUiState(6, 0, 10, 13, 42,
                83.4f, 180f, 0f, 100f, false, false, 0f, 0f,
                System.Array.Empty<WeaponSlotView>(), runEnded: true, victory: false));

            Assert.That(TextNamed(root, "Result Title"), Is.EqualTo("전투 종료"));
            var summary = TextNamed(root, "Result Summary");
            Assert.That(summary, Does.Contain("생존 시간"));
            Assert.That(summary, Does.Contain("처치"));
            Assert.That(summary, Does.Contain("도달 레벨"));
            Assert.That(summary, Does.Contain("획득 엽전"));
            Assert.That(TextNamed(root, "Lobby Return Label"), Is.EqualTo("로비로 돌아가기"));
            Assert.That(ImageNamed(root, "Result Panel").color.a, Is.EqualTo(1f));
            Assert.That(AllText(root), Does.Not.Match("Run|Restart|Survived|Try again"));

            var button = System.Array.Find(root.GetComponentsInChildren<Button>(true),
                candidate => candidate.name == "Lobby Return Button");
            Assert.That(button, Is.Not.Null);
            button.onClick.Invoke();
            Assert.That(returns, Is.EqualTo(1));
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator GameplayRunResultButtonReturnsToLobby()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var presenter = Object.FindFirstObjectByType<RunResultPresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            controller.EndRunForTests(false);
            yield return new WaitForSecondsRealtime(.15f);
            var root = System.Array.Find(presenter.GetComponentsInChildren<RectTransform>(true),
                candidate => candidate.name == "Run Result Root");
            Assert.That(root.gameObject.activeSelf, Is.True);
            var button = System.Array.Find(presenter.GetComponentsInChildren<Button>(true),
                candidate => candidate.name == "Lobby Return Button");
            button.onClick.Invoke();
            for (var frame = 0; frame < 240 && SceneManager.GetActiveScene().name != "Lobby"; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
        }

        [Test]
        public void AbandonConfirmationUsesOpaqueKoreanCopyAndTwoClearChoices()
        {
            var root = new GameObject("Abandon Test");
            var presenter = root.AddComponent<AbandonRunPresenter>();
            var confirmed = 0;
            var cancelled = 0;
            presenter.Confirmed += () => confirmed++;
            presenter.Cancelled += () => cancelled++;
            presenter.Open();

            Assert.That(TextNamed(root, "Abandon Title"), Is.EqualTo("순찰에서 귀환"));
            Assert.That(TextNamed(root, "Abandon Message"), Does.Contain("숙련도와 엽전"));
            Assert.That(ImageNamed(root, "Abandon Panel").color.a, Is.EqualTo(1f));
            System.Array.Find(root.GetComponentsInChildren<Button>(true),
                candidate => candidate.name == "Continue Combat Button").onClick.Invoke();
            System.Array.Find(root.GetComponentsInChildren<Button>(true),
                candidate => candidate.name == "Confirm Return Button").onClick.Invoke();
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(confirmed, Is.EqualTo(1));
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator Level_up_keeps_combat_paused_until_reward_confirmation_then_sequences_the_queue()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var rewardReveal = Object.FindFirstObjectByType<RewardRevealPresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(choice, Is.Not.Null);
            Assert.That(rewardReveal, Is.Not.Null);
            Assert.That(bootstrap.BoundController, Is.EqualTo(controller));

            controller.OpenUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
            controller.AddExperienceForTests(100);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(choice.IsOpen, Is.True);

            var cards = choice.GetComponentsInChildren<Button>(true);
            Assert.That(cards, Has.Length.EqualTo(3));
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.GetComponent<BaseInputModule>(), Is.Not.Null);
            ExecuteEvents.Execute<IPointerClickHandler>(cards[0].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute<IPointerClickHandler>(cards[1].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.25f);

            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(rewardReveal.IsRevealing, Is.True);
            Assert.That(controller.IsUpgradeOpen, Is.False,
                "The queued choice must wait until the unscaled reward reveal completes.");

            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(rewardReveal.IsAwaitingConfirmation, Is.True);
            rewardReveal.Confirm();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(rewardReveal.IsRevealing, Is.False);
            Assert.That(controller.IsUpgradeOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            controller.TickGameplayIfRunningForTests(1.01f);
            Assert.That(controller.IsUpgradeOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Run_reset_closes_the_bootstrap_owned_upgrade_presentation()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(choice, Is.Not.Null);

            controller.OpenUpgradeForTests();
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(choice.IsOpen, Is.True);

            controller.ResetRunForTests();
            Assert.That(choice.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Bootstrap_creates_one_portrait_hud_with_a_safe_area_container()
        {
            yield return DestroyBootstraps();
            var root = new GameObject("UI Test");
            var bootstrap = root.AddComponent<FirstPlayableUiBootstrap>();
            yield return null;

            var canvas = root.GetComponentInChildren<Canvas>(true);
            var scaler = root.GetComponentInChildren<CanvasScaler>(true);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(.5f));
            Assert.That(root.GetComponentInChildren<CombatHudPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<WeaponRackPresenter>(true), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(EventSystem.current.GetComponent<BaseInputModule>(), Is.Not.Null);

            bootstrap.ApplySafeArea(new Rect(0f, 120f, 1000f, 1760f), new Vector2(1000f, 2000f));
            Assert.That(bootstrap.SafeAreaContainer.anchorMin, Is.EqualTo(new Vector2(0f, .06f)));
            Assert.That(bootstrap.SafeAreaContainer.anchorMax, Is.EqualTo(new Vector2(1f, .94f)));

            bootstrap.ApplySafeArea(new Rect(0f, 96f, 1080f, 1728f), new Vector2(1080f, 1920f));
            Assert.That(bootstrap.SafeAreaContainer.anchorMin, Is.EqualTo(new Vector2(0f, .05f)));
            Assert.That(bootstrap.SafeAreaContainer.anchorMax, Is.EqualTo(new Vector2(1f, .95f)));

            new GameObject("Duplicate UI Test").AddComponent<FirstPlayableUiBootstrap>();
            yield return null;
            Assert.That(Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(1));
            Assert.That(bootstrap.BoundController, Is.Null);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Weapon_rack_hides_a_null_icon_without_creating_extra_slots()
        {
            var root = new GameObject("Rack Test");
            var rack = root.AddComponent<WeaponRackPresenter>();
            rack.Render(new[] { new WeaponSlotView("hwando_flying_blade", "Hwando", 1, null) });
            yield return null;

            var icon = System.Array.Find(root.GetComponentsInChildren<Image>(true), image => image.name == "Icon");
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.enabled, Is.False);
            rack.Render(new[] { new WeaponSlotView("hwando_flying_blade", "Hwando", 1, null) });
            Assert.That(root.GetComponentsInChildren<Image>(true).Length, Is.GreaterThanOrEqualTo(3));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Weapon_rack_emits_the_current_weapon_when_tapped()
        {
            var root = new GameObject("Rack Tap Test");
            var rack = root.AddComponent<WeaponRackPresenter>();
            var expected = new WeaponSlotView("gakgung_shot", "각궁", 3, null,
                behavior: "적을 관통하는 화살");
            WeaponSlotView selected = default;
            var selectedCount = 0;
            rack.WeaponSelected += weapon =>
            {
                selected = weapon;
                selectedCount++;
            };

            rack.Render(new[] { expected });
            yield return null;
            root.GetComponentInChildren<Button>(true).onClick.Invoke();

            Assert.That(selectedCount, Is.EqualTo(1));
            Assert.That(selected.Id, Is.EqualTo(expected.Id));
            Assert.That(selected.Behavior, Is.EqualTo(expected.Behavior));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ReadOnlyWeaponDetailsDoNotOwnGameTime()
        {
            var root = new GameObject("Read Only Detail Test");
            var presenter = root.AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var weapon = new WeaponSlotView("gakgung_shot", "각궁", 3, null,
                "피해량 +24%", new[] { JoseonHunter.Domain.Progression.WeaponPotentialId.GakgungFullDraw },
                behavior: "적을 관통하는 화살");
            Time.timeScale = 1f;

            presenter.ShowDetails(weapon);
            yield return null;

            Assert.That(presenter.IsDetailOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(presenter.DisplayedAffixText, Is.EqualTo("피해량 +24%"));
            presenter.HideImmediately();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Object.Destroy(root);
        }

        private static IEnumerator DestroyBootstraps()
        {
            foreach (var bootstrap in Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(bootstrap.gameObject);
            foreach (var controller in Object.FindObjectsByType<FirstPlayableController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(controller.gameObject);
            yield return null;
        }

        private static string TextNamed(GameObject root, string name)
        {
            var target = System.Array.Find(root.GetComponentsInChildren<RectTransform>(true),
                candidate => candidate.name == name);
            Assert.That(target, Is.Not.Null, name);
            foreach (var component in target.GetComponents<Component>())
            {
                var property = component.GetType().GetProperty("text");
                if (property != null && property.PropertyType == typeof(string))
                    return (string)property.GetValue(component);
            }
            Assert.Fail("No text component on " + name);
            return string.Empty;
        }

        private static string AllText(GameObject root)
        {
            var values = new System.Collections.Generic.List<string>();
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                foreach (var component in rect.GetComponents<Component>())
                {
                    var property = component.GetType().GetProperty("text");
                    if (property == null || property.PropertyType != typeof(string)) continue;
                    values.Add((string)property.GetValue(component));
                }
            }
            return string.Join(" ", values);
        }

        private static Image ImageNamed(GameObject root, string name) =>
            System.Array.Find(root.GetComponentsInChildren<Image>(true), image => image.name == name);

        private static JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset TestCatalog()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
            var catalog = ScriptableObject.CreateInstance<JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset>();
            catalog.SetSlotKitForTests(sprite, sprite, sprite, sprite, sprite);
            return catalog;
        }
    }
}
