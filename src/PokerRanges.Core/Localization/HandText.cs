using System.Globalization;
using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Localization;

/// <summary>
/// The vocabulary of hands and boards. Both languages sit side by side: a translation drifting
/// from its original is easier to spot on the same line than across two files.
/// </summary>
public static class HandText
{
    public static string RoyalFlush => Language.Pick("Royal flush", "Quinte flush royale");

    public static string StraightFlushTo(Rank rank)
    {
        return Language.Pick(
            $"Straight flush to {CardSymbols.ToCharacter(rank)}",
            $"Quinte flush à {CardSymbols.ToCharacter(rank)}");
    }

    public static string FourOfAKind(Rank rank)
    {
        return Language.Pick(
            $"Four of a kind, {CardSymbols.ToCharacter(rank)}s",
            $"Carré de {CardSymbols.ToCharacter(rank)}");
    }

    public static string FullHouse(Rank over, Rank under)
    {
        return Language.Pick(
            $"Full house, {CardSymbols.ToCharacter(over)}s full of {CardSymbols.ToCharacter(under)}s",
            $"Full aux {CardSymbols.ToCharacter(over)} par les {CardSymbols.ToCharacter(under)}");
    }

    public static string FlushTo(Rank rank)
    {
        return Language.Pick(
            $"Flush, {CardSymbols.ToCharacter(rank)} high",
            $"Couleur à {CardSymbols.ToCharacter(rank)}");
    }

    public static string StraightTo(Rank rank)
    {
        return Language.Pick(
            $"Straight to {CardSymbols.ToCharacter(rank)}",
            $"Quinte à {CardSymbols.ToCharacter(rank)}");
    }

    public static string ThreeOfAKind(Rank rank)
    {
        return Language.Pick(
            $"Three of a kind, {CardSymbols.ToCharacter(rank)}s",
            $"Brelan de {CardSymbols.ToCharacter(rank)}");
    }

    public static string TwoPair(Rank high, Rank low)
    {
        return Language.Pick(
            $"Two pair, {CardSymbols.ToCharacter(high)}s and {CardSymbols.ToCharacter(low)}s",
            $"Deux paires, {CardSymbols.ToCharacter(high)} et {CardSymbols.ToCharacter(low)}");
    }

    public static string OnePair(Rank rank)
    {
        return Language.Pick(
            $"Pair of {CardSymbols.ToCharacter(rank)}s",
            $"Paire de {CardSymbols.ToCharacter(rank)}");
    }

    public static string HighCard(Rank rank)
    {
        return Language.Pick(
            $"{CardSymbols.ToCharacter(rank)} high",
            $"Hauteur {CardSymbols.ToCharacter(rank)}");
    }

    public static string FlushDraw => Language.Pick("flush draw", "tirage couleur");

    public static string OpenEndedDraw => Language.Pick("open-ended straight draw", "tirage quinte bilatéral");

    public static string Gutshot => Language.Pick("gutshot straight draw", "tirage quinte par le ventre");

    public static string Nuts => Language.Pick("the nuts", "meilleure main possible");

    public static string TierHighCard => Language.Pick("high card", "hauteur");

    public static string TierUnderPair => Language.Pick("underpair", "sous-paire");

    public static string TierBottomPair => Language.Pick("bottom pair", "paire basse");

    public static string TierMiddlePair => Language.Pick("middle pair", "paire moyenne");

    public static string TierTopPair => Language.Pick("top pair", "top paire");

    public static string TierOverpair => Language.Pick("overpair", "overpaire");

    public static string TierTwoPair => Language.Pick("two pair", "deux paires");

    public static string TierTrips => Language.Pick("trips with the board", "brelan avec le board");

    public static string TierSet => Language.Pick("set", "set");

    public static string TierStraight => Language.Pick("straight", "quinte");

    public static string TierFlush => Language.Pick("flush", "couleur");

    public static string TierFullHouse => Language.Pick("full house", "full");

    public static string TierQuads => Language.Pick("quads", "carré");

    public static string TierStraightFlush => Language.Pick("straight flush", "quinte flush");

    public static string BoardTrips => Language.Pick("trips on board", "brelan au board");

    public static string BoardPaired => Language.Pick("paired", "pairé");

    public static string BoardMonotone => Language.Pick("monotone", "monocolore");

    public static string BoardTwoTone => Language.Pick("two-tone", "bicolore");

    public static string BoardRainbow => Language.Pick("rainbow", "arc-en-ciel");

    public static string BoardVeryConnected => Language.Pick("very connected", "très connecté");

    public static string BoardConnected => Language.Pick("connected", "connecté");

    public static string BoardWet => Language.Pick("wet", "humide");

    public static string BoardSemiWet => Language.Pick("fairly wet", "moyennement humide");

    public static string BoardDry => Language.Pick("dry", "sec");

    public static string BoardSummary(Rank highCard, string traits)
    {
        return Language.Pick(
            $"{CardSymbols.ToCharacter(highCard)}-high board — {traits}",
            $"Board hauteur {CardSymbols.ToCharacter(highCard)} — {traits}");
    }

    public static string BoardNeedsThreeToFiveCards(int received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Board texture is read from 3 to 5 cards, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"La texture s'analyse sur 3 à 5 cartes, reçu {received}."));
    }
}
