using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;
using PokerRanges.Data.Storage;
using Shouldly;

namespace PokerRanges.Data.Tests;

/// <summary>
/// Session storage is the only place in the project where a file written by one version may be
/// read back by another. These tests are therefore as much about what reads back correctly as
/// about what still reads back after being damaged.
/// </summary>
public sealed class SessionStorageTests : IDisposable
{
    private readonly string _directory;
    private readonly SessionStoreOptions _options;

    public SessionStorageTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "PokerRanges.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);

        _options = new SessionStoreOptions
        {
            PreferencesFilePath = Path.Combine(_directory, "settings.json"),
            HandFilePath = Path.Combine(_directory, "hand.json"),
            JournalFilePath = Path.Combine(_directory, "journal.json"),
            JournalCapacity = 3,
        };
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void WithNothingOnDiskTheDefaultsAreReturnedRatherThanAFailure()
    {
        JsonSessionStore store = NewStore();

        store.LoadPreferences().ShouldBe(UserPreferences.Default);
        store.LoadHand().ShouldBeNull();
    }

    [Fact]
    public void PreferencesComeBackExactlyAsTheyWereWritten()
    {
        UserPreferences preferences = new()
        {
            PlayerCount = 3,
            BigBlind = 50,
            StartingStack = 1200,
            AnteStyle = AnteStyle.BigBlindAnte,
            AnteAmount = 50,
            HeroPosition = Position.SmallBlind,
            OpponentProfile = "Agressif",
            PrefersCompactLayout = true,
        };

        NewStore().SavePreferences(preferences);

        NewStore().LoadPreferences().ShouldBe(preferences);
    }

    [Fact]
    public void AHandComesBackWithItsTableItsCardsAndItsActions()
    {
        HandState hand = SampleHand();

        NewStore().SaveHand(hand);
        HandState? reloaded = NewStore().LoadHand();

        reloaded.ShouldNotBeNull();
        reloaded.HeroCards.ShouldBe(hand.HeroCards);
        reloaded.Board.ShouldBe(hand.Board);
        reloaded.Actions.ShouldBe(hand.Actions);
        reloaded.Table.PlayerCount.ShouldBe(6);
        reloaded.Table.HeroPosition.ShouldBe(Position.Button);
        reloaded.Table.StackOf(Position.Button).ShouldBe(800);
    }

    [Fact]
    public void SavingNothingErasesTheResumeFile()
    {
        JsonSessionStore store = NewStore();
        store.SaveHand(SampleHand());

        store.SaveHand(null);

        File.Exists(_options.HandFilePath).ShouldBeFalse();
        store.LoadHand().ShouldBeNull();
    }

    /// <summary>
    /// An assistant that refuses to start because of its own resume file would be worse than one
    /// that has forgotten everything.
    /// </summary>
    [Fact]
    public void ATruncatedFileIsTreatedAsNothingSavedInsteadOfThrowing()
    {
        File.WriteAllText(_options.HandFilePath, "{\"playerCount\": 6, \"board\": \"Ks8d");
        File.WriteAllText(_options.PreferencesFilePath, "ceci n'est pas du JSON");

        JsonSessionStore store = NewStore();

        store.LoadHand().ShouldBeNull();
        store.LoadPreferences().ShouldBe(UserPreferences.Default);
    }

    [Fact]
    public void AHandWithNonsenseCardsIsRefusedWithoutBringingDownTheStartup()
    {
        File.WriteAllText(
            _options.HandFilePath,
            "{\"playerCount\": 6, \"bigBlind\": 8, \"heroPosition\": \"Button\", \"heroCards\": \"Zz9d\", \"board\": \"\"}");

        NewStore().LoadHand().ShouldBeNull();
    }

    [Fact]
    public void TheJournalKeepsTheMostRecentHandsFirst()
    {
        JsonHandJournal journal = NewJournal();

        journal.Append(Entry("Ah2h", "Fold"));
        journal.Append(Entry("Kh9d", "Miser 44"));

        journal.Entries[0].Advice.ShouldBe("Miser 44");
        journal.Entries[1].Advice.ShouldBe("Fold");
    }

    [Fact]
    public void TheJournalDropsTheOldestHandsOnceItIsFull()
    {
        JsonHandJournal journal = NewJournal();

        journal.Append(Entry("Ah2h", "première"));
        journal.Append(Entry("Kh9d", "deuxième"));
        journal.Append(Entry("QsJs", "troisième"));
        journal.Append(Entry("7c7d", "quatrième"));

        journal.Entries.Count.ShouldBe(3);
        journal.Entries.ShouldNotContain(entry => entry.Advice == "première");
    }

    [Fact]
    public void AJournalledHandIsWholeEnoughToBeReplayed()
    {
        NewJournal().Append(Entry("Kh9d", "Miser 44"));

        JournalEntry reloaded = NewJournal().Entries.ShouldHaveSingleItem();

        reloaded.Hand.HeroCards.ShouldBe(HoleCards.Parse("Kh9d"));
        reloaded.Hand.Board.Count.ShouldBe(3);
        reloaded.Hand.Actions.Count.ShouldBe(6);
        reloaded.Rationale.ShouldNotBeEmpty();
        reloaded.DescribeHand().ShouldContain("BTN");
    }

    [Fact]
    public void ClearingTheJournalRemovesItFromDiskToo()
    {
        NewJournal().Append(Entry("Kh9d", "Miser 44"));

        NewJournal().Clear();

        NewJournal().Entries.ShouldBeEmpty();
    }

    private JsonSessionStore NewStore()
    {
        return new JsonSessionStore(_options, NullLogger<JsonSessionStore>.Instance);
    }

    private JsonHandJournal NewJournal()
    {
        return new JsonHandJournal(_options, NullLogger<JsonHandJournal>.Instance);
    }

    private static JournalEntry Entry(string heroCards, string advice)
    {
        return new JournalEntry
        {
            PlayedAt = new DateTimeOffset(2026, 7, 31, 14, 22, 0, TimeSpan.Zero),
            Hand = SampleHand() with { HeroCards = HoleCards.Parse(heroCards) },
            Advice = advice,
            Rationale = ["Ta main : top paire.", "Board hauteur K."],
        };
    }

    private static HandState SampleHand()
    {
        return new HandState
        {
            Table = TableConfiguration.Uniform(6, 8, 800, Position.Button),
            HeroCards = HoleCards.Parse("Kh9d"),
        }
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 18))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Call(Street.Preflop, Position.BigBlind))
            .WithBoard(TestCards.Parse("Ks8d3c"));
    }
}
