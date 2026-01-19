public interface IPlaceable
{
    void OnPlaced(Tile tile, GridManager grid);
    void OnRemoved(Tile tile, GridManager grid);
}
