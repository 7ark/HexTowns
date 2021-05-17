namespace TerrainMods
{
    public interface ITerrainMod
    {
        void ApplyModification(HexTile[] tiles);
    }

    public interface IBoardMod
    {
        void ApplyModification(HexBoard hexBoard);
    }
}