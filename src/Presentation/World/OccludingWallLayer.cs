using System.Collections.Generic;
using Godot;

namespace FairvilleInn.Presentation.World;

// Wall layer whose tiles fade when they are drawn in front of a subject (the player)
// or cover the point under the cursor. Per-cell alpha is applied through TileData
// runtime updates, so the tiles themselves are untouched.
public partial class OccludingWallLayer : TileMapLayer
{
    [Export]
    public float FadedAlpha { get; set; } = 0.35f;

    [Export]
    public float FadeSpeed { get; set; } = 6.0f;

    private readonly Dictionary<Vector2I, float> _alpha = [];
    private readonly HashSet<Vector2I> _occluding = [];

    public Godot.Collections.Array<Vector2I> FadedCells => [.. _alpha.Keys];

    public void UpdateOcclusion(Rect2 subjectRect, float subjectSortY, Vector2 hoverPoint, float hoverRadius)
    {
        _occluding.Clear();
        var localSubject = new Rect2(ToLocal(subjectRect.Position), subjectRect.Size);
        var localHover = ToLocal(hoverPoint);
        var hoverRect = new Rect2(localHover - Vector2.One * hoverRadius, Vector2.One * hoverRadius * 2);

        foreach (var cell in GetUsedCells())
        {
            var data = GetCellTileData(cell);
            if (data is null)
            {
                continue;
            }

            var rect = TileRect(cell, data);
            var sortY = MapToLocal(cell).Y + data.YSortOrigin;

            var coversSubject = sortY > subjectSortY && rect.Intersects(localSubject);
            var coversHover = sortY > localHover.Y && rect.Intersects(hoverRect);
            if (coversSubject || coversHover)
            {
                _occluding.Add(cell);
            }
        }
    }

    public override void _Process(double delta)
    {
        var step = (float)delta * FadeSpeed;
        var changed = false;

        foreach (var cell in _occluding)
        {
            _alpha.TryAdd(cell, 1.0f);
        }

        foreach (var cell in new List<Vector2I>(_alpha.Keys))
        {
            var target = _occluding.Contains(cell) ? FadedAlpha : 1.0f;
            var next = Mathf.MoveToward(_alpha[cell], target, step);
            if (Mathf.IsEqualApprox(next, _alpha[cell]))
            {
                continue;
            }

            changed = true;
            if (Mathf.IsEqualApprox(next, 1.0f))
            {
                _alpha.Remove(cell);
            }
            else
            {
                _alpha[cell] = next;
            }
        }

        if (changed)
        {
            NotifyRuntimeTileDataUpdate();
        }
    }

    public override bool _UseTileDataRuntimeUpdate(Vector2I coords) => _alpha.ContainsKey(coords);

    public override void _TileDataRuntimeUpdate(Vector2I coords, TileData tileData)
    {
        if (_alpha.TryGetValue(coords, out var alpha))
        {
            tileData.Modulate = new Color(1, 1, 1, alpha);
        }
    }

    private Rect2 TileRect(Vector2I cell, TileData data)
    {
        var source = (TileSetAtlasSource)TileSet.GetSource(GetCellSourceId(cell));
        var size = (Vector2)source.GetTileTextureRegion(GetCellAtlasCoords(cell)).Size;
        var centre = MapToLocal(cell) - (Vector2)data.TextureOrigin;
        return new Rect2(centre - size / 2, size);
    }
}
