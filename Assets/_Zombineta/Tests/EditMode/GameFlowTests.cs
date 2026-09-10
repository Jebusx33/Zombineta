using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Flow;

namespace Zombineta.Tests
{
    public class GameFlowTests
    {
        static GameFlow InLevel(int levels, int level)
        {
            var f = new GameFlow(levels);
            f.Play();
            f.ChooseCharacter(0);
            f.CinematicFinished();
            for (int i = 0; i < level; i++)
            {
                f.LevelWon();
                f.Continue();
                f.CinematicFinished();
            }
            return f;
        }

        [Test]
        public void TheGameStartsAtTheMainMenu()
        {
            Assert.AreEqual(GameScreen.MainMenu, new GameFlow(2).Current);
        }

        [Test]
        public void MainMenu_ToOptions_AndBack()
        {
            var f = new GameFlow(2);

            Assert.IsTrue(f.OpenOptions());
            Assert.AreEqual(GameScreen.Options, f.Current);

            Assert.IsTrue(f.Back());
            Assert.AreEqual(GameScreen.MainMenu, f.Current);
        }

        [Test]
        public void TheFullHappyPath_FollowsTheGddDiagram()
        {
            var f = new GameFlow(2);
            var seen = new List<GameScreen> { f.Current };
            f.Changed += (from, to) => seen.Add(to);

            f.Play();
            f.ChooseCharacter(1);
            f.CinematicFinished();
            f.LevelWon();
            f.Continue();          // nivel 1 -> cinematica del nivel 2
            f.CinematicFinished();
            f.LevelWon();
            f.Continue();          // era el ultimo -> final
            f.ToMainMenu();

            CollectionAssert.AreEqual(new[]
            {
                GameScreen.MainMenu, GameScreen.CharacterSelect, GameScreen.Cinematic,
                GameScreen.Playing, GameScreen.LevelComplete, GameScreen.Cinematic,
                GameScreen.Playing, GameScreen.LevelComplete, GameScreen.Ending,
                GameScreen.MainMenu,
            }, seen);
        }

        [Test]
        public void ChoosingACharacter_RemembersIt_AndStartsAtLevelOne()
        {
            var f = new GameFlow(3);
            f.Play();
            f.ChooseCharacter(1);

            Assert.AreEqual(1, f.CharacterIndex);
            Assert.AreEqual(0, f.LevelIndex);
            Assert.AreEqual(GameScreen.Cinematic, f.Current);
        }

        [Test]
        public void WinningALevelWithMoreLeft_GoesToTheNextCinematic()
        {
            var f = InLevel(levels: 3, level: 0);
            f.LevelWon();
            f.Continue();

            Assert.AreEqual(GameScreen.Cinematic, f.Current);
            Assert.AreEqual(1, f.LevelIndex);
        }

        [Test]
        public void WinningTheLastLevel_GoesToTheEnding()
        {
            var f = InLevel(levels: 2, level: 1);
            Assert.IsTrue(f.IsLastLevel);

            f.LevelWon();
            f.Continue();

            Assert.AreEqual(GameScreen.Ending, f.Current);
        }

        [Test]
        public void LosingGoesToGameOver_AndRetryReplaysTheSameLevelWithoutTheCinematic()
        {
            var f = InLevel(levels: 3, level: 1);
            f.LevelLost();
            Assert.AreEqual(GameScreen.GameOver, f.Current);

            f.Retry();

            Assert.AreEqual(GameScreen.Playing, f.Current);
            Assert.AreEqual(1, f.LevelIndex, "reintentar no cambia de nivel");
        }

        [Test]
        public void GameOver_CanGoBackToTheMainMenu()
        {
            var f = InLevel(levels: 2, level: 0);
            f.LevelLost();

            Assert.IsTrue(f.ToMainMenu());
            Assert.AreEqual(GameScreen.MainMenu, f.Current);
        }

        [Test]
        public void PlayingAgainAfterGameOver_StartsFromLevelOne()
        {
            var f = InLevel(levels: 3, level: 2);
            f.LevelLost();
            f.ToMainMenu();
            f.Play();
            f.ChooseCharacter(0);

            Assert.AreEqual(0, f.LevelIndex);
        }

        [Test]
        public void ActionsThatDontBelongToTheCurrentScreen_AreIgnored()
        {
            var f = new GameFlow(2);
            int changes = 0;
            f.Changed += (a, b) => changes++;

            // Desde el menu no se puede ganar, perder, reintentar ni saltear la cinematica.
            Assert.IsFalse(f.LevelWon());
            Assert.IsFalse(f.LevelLost());
            Assert.IsFalse(f.Retry());
            Assert.IsFalse(f.CinematicFinished());
            Assert.IsFalse(f.Continue());
            Assert.IsFalse(f.ChooseCharacter(0));
            Assert.IsFalse(f.Back());

            Assert.AreEqual(GameScreen.MainMenu, f.Current);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void ALateWinDuringGameOver_CannotSkipAhead()
        {
            // Si la simulacion reporta victoria cuando ya se perdio, no pasa nada.
            var f = InLevel(levels: 2, level: 0);
            f.LevelLost();

            Assert.IsFalse(f.LevelWon());
            Assert.AreEqual(GameScreen.GameOver, f.Current);
        }

        [Test]
        public void AGameNeedsAtLeastOneLevel()
        {
            Assert.Throws<System.ArgumentException>(() => new GameFlow(0));
        }
    }
}
