using NUnit.Framework;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Tests
{
    public class SceneRoutePlannerTests
    {
        const string L1 = "Level_01";
        const string L2 = "Level_02";

        static SceneRoutePlanner At(string baseScene, params string[] overlays)
        {
            var p = new SceneRoutePlanner();
            p.Start(baseScene, overlays);
            return p;
        }

        [Test]
        public void FromNothing_TheMenuLoadsWithFade()
        {
            var plan = At(null).Go(GameScreen.MainMenu, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Load);
            CollectionAssert.IsEmpty(plan.Unload);
            Assert.AreEqual(SceneNames.MainMenu, plan.Active);
            Assert.IsTrue(plan.Fade);
        }

        [Test]
        public void ChangingScreen_SwapsTheBaseWithFade()
        {
            var plan = At(SceneNames.MainMenu).Go(GameScreen.CharacterSelect, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.CharacterSelect }, plan.Load);
            Assert.IsTrue(plan.Fade);
        }

        [Test]
        public void OptionsFromTheMenu_IsAnOverlay_AndBackOnlyRemovesIt()
        {
            var p = At(SceneNames.MainMenu);

            var open = p.Go(GameScreen.Options, null, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, open.Load);
            CollectionAssert.IsEmpty(open.Unload);
            Assert.IsFalse(open.Fade);

            var back = p.Go(GameScreen.MainMenu, null, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, back.Unload);
            CollectionAssert.IsEmpty(back.Load);
            Assert.IsFalse(back.Fade, "el menu no se recarga");
        }

        [Test]
        public void TheCinematicLeadsToTheLevelScene()
        {
            var plan = At(SceneNames.Cinematic).Go(GameScreen.Playing, L1, true);

            CollectionAssert.AreEqual(new[] { SceneNames.Cinematic }, plan.Unload);
            CollectionAssert.AreEqual(new[] { L1 }, plan.Load);
            Assert.AreEqual(L1, plan.Active);
        }

        [Test]
        public void PauseIsAnOverlay_AndResumeDoesNotReloadTheLevel()
        {
            var p = At(L1);

            var pause = p.Go(GameScreen.Paused, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Pause }, pause.Load);
            CollectionAssert.IsEmpty(pause.Unload);
            Assert.AreEqual(L1, pause.Active, "el nivel sigue siendo la escena activa");

            var resume = p.Go(GameScreen.Playing, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Pause }, resume.Unload);
            CollectionAssert.IsEmpty(resume.Load);
            Assert.IsFalse(resume.Fade);
        }

        [Test]
        public void OptionsFromPause_StackOnTop_AndBackLeavesThePause()
        {
            var p = At(L1, SceneNames.Pause);

            var open = p.Go(GameScreen.Options, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, open.Load);
            CollectionAssert.IsEmpty(open.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.Pause, SceneNames.Options }, p.Overlays);

            var back = p.Go(GameScreen.Paused, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, back.Unload);
            CollectionAssert.IsEmpty(back.Load);
            Assert.AreEqual(SceneNames.Pause, p.TopScene);
        }

        [Test]
        public void RetryFromPause_ReloadsTheLevel()
        {
            var plan = At(L1, SceneNames.Pause).Go(GameScreen.Playing, L1, true);

            CollectionAssert.AreEqual(new[] { SceneNames.Pause, L1 }, plan.Unload);
            CollectionAssert.AreEqual(new[] { L1 }, plan.Load);
            Assert.IsTrue(plan.Fade);
        }

        [Test]
        public void QuittingFromPause_UnloadsTheOverlaysAndTheLevel()
        {
            var plan = At(L1, SceneNames.Pause).Go(GameScreen.MainMenu, L1, false);

            CollectionAssert.AreEqual(new[] { SceneNames.Pause, L1 }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Load);
        }

        [Test]
        public void NextLevel_ComesFromTheCinematic_WithItsOwnScene()
        {
            var p = At(SceneNames.LevelComplete);
            p.Go(GameScreen.Cinematic, L2, false);
            var plan = p.Go(GameScreen.Playing, L2, true);

            CollectionAssert.AreEqual(new[] { L2 }, plan.Load);
            Assert.AreEqual(L2, p.BaseScene);
        }

        [Test]
        public void StartingFromAnOverlayAlone_TheMenuReplacesIt()
        {
            var plan = At(null, SceneNames.Options).Go(GameScreen.MainMenu, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.Options }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Load);
        }

        // --- Creditos ------------------------------------------------------------

        [Test]
        public void CreditsIsABase()
        {
            Assert.AreEqual(SceneNames.Credits, SceneRoutePlanner.ScreenScene(GameScreen.Credits));
        }

        [Test]
        public void FromTheMenu_CreditsUnloadsTheMenuAndLoadsCredits_WithFade()
        {
            var plan = At(SceneNames.MainMenu).Go(GameScreen.Credits, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.Credits }, plan.Load);
            Assert.AreEqual(SceneNames.Credits, plan.Active);
            Assert.IsTrue(plan.Fade);
        }
    }
}
