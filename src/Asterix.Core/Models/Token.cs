namespace Asterix.Core.Models
{
    public enum TokenType
    {
        Helmet,
        Boar,
        Fish,
        Potion
    }

    public record Token(
        TokenType Type,
        string Name,
        int Value = 0,
        string Ability = "noop"
    );

    public record PendingReplacementChoice(
        int PlayerId,
        int BattlefieldIndex,
        BattlefieldCard NewCard,
        IReadOnlyList<SideColor> OrientationOptions,
        IReadOnlyList<TokenType> TokenOptions
    );
}
