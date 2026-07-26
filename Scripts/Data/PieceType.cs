namespace TileMatchGame.Data;

public enum PieceType
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    // Tier-unlocked 6th color (see TileDatabase.GetActiveTypes) - keep last so
    // base 5 colors stay index-stable if more tiers/colors are added later.
    Orange
}
