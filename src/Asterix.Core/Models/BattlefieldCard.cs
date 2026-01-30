namespace Asterix.Core.Models
{
    public enum SideColor
    {
        Red,
        Blue
    }

    public record BattlefieldCard(
        string Name,
        int Points = 15,
        string RuleModifier = null
    );

    public record BattlefieldInstance(
        BattlefieldCard Card,
        SideColor FacingPlayer0,
        IReadOnlyList<Card> SideRedCards,
        IReadOnlyList<Card> SideBlueCards
    );
}
