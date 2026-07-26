namespace TileMatchGame.Data;

/// <summary>
/// Kind of special power-up a Piece can become after a 4+ run match.
/// None = ordinary piece.
/// </summary>
public enum SpecialPieceKind
{
    None,
    LineClearRow,
    LineClearCol,
    ColorBomb
}
