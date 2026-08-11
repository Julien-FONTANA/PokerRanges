namespace PokerRanges.Core.Cards;

public static class CardSymbols
{
    private const string RankCharacters = "23456789TJQKA";
    private const string SuitCharacters = "cdhs";
    private const string SuitGlyphs = "♣♦♥♠";

    public static char ToCharacter(Rank rank)
    {
        return RankCharacters[(int)rank - 2];
    }

    public static char ToCharacter(Suit suit)
    {
        return SuitCharacters[(int)suit];
    }

    public static char ToGlyph(Suit suit)
    {
        return SuitGlyphs[(int)suit];
    }

    public static char ToCharacter(HandShape shape)
    {
        return shape switch
        {
            HandShape.Suited => 's',
            HandShape.Offsuit => 'o',
            _ => '\0',
        };
    }

    public static bool TryParseRank(char character, out Rank rank)
    {
        int index = RankCharacters.IndexOf(char.ToUpperInvariant(character));
        rank = index < 0 ? default : (Rank)(index + 2);
        return index >= 0;
    }

    public static bool TryParseSuit(char character, out Suit suit)
    {
        int index = SuitCharacters.IndexOf(char.ToLowerInvariant(character));
        suit = index < 0 ? default : (Suit)index;
        return index >= 0;
    }
}
