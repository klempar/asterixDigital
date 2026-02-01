namespace Asterix.Core.Models
{
    public enum CardBackColor
    {
        Red,
        Blue
    }

    public record Card(
        string Name,
        string Color,
        int Power,
        string Type,
        string Ability,
        CardBackColor Back
    );
}
