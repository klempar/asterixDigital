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
        string Ability = "noop"
    );
}
