# Art pipeline

How rooms, doors and props get their graphics. The pipeline is **type-driven**: each
asset *type* (floor, wall, door, prop) defines a file contract once, and every asset of
that type is a folder of PNGs that follows it. Nothing in the game references artist
files directly — a pack step turns them into atlases, a TileSet and prop scenes.

```
Create art ──► assets/art/<type>/<name>/   (placeholders + guides + meta.json)
   paint  ──► same files, same sizes
   Test   ──► pack ► import ► game opens a test room with the asset
```

## Using the Art Pipeline window

Open `tools/art_pipeline.tscn` in the Godot editor and press **F6** (run current
scene), or run `tools\art_pipeline.cmd` (set `GODOT` to the Godot .NET exe).

1. Pick a **type**, enter a **name** (`carpet_red`), optional display name (shown in
   prompts: "Open *cellar door*"), for props a **footprint** and **height**.
2. **Create art** — the folder opens in Explorer with:
   - the required PNGs, pre-filled with a flat grey placeholder in the right shape,
   - `_guide_<file>.png` overlays: magenta = footprint diamond, cyan = anchor,
   - `meta.json` (type, name, footprint…) and `README.txt` with the sizes.
3. Paint over the placeholders. Keep the exact size; transparent outside the art.
4. Select the asset, **Test in game** — packs everything, runs the Godot importer,
   launches the game in a small room built around that asset (`--room=` argument).
5. **Pack all** after editing existing art. Rooms pick up the new palette when reopened
   in the editor.
6. **New room** — name, size, default floor/wall → `scenes/rooms/<name>.tscn`, ready to
   paint in the Godot editor (see [Rooms](#rooms)).

The same steps run headless:

```
godot --headless --path . tools/art_cli.tscn -- new floor carpet_red "Red carpet"
godot --headless --path . tools/art_cli.tscn -- new prop barrel "Barrel" 1x1 72
godot --headless --path . tools/art_cli.tscn -- pack
godot --headless --path . tools/art_cli.tscn -- room cellar 12x10 stone stone
godot --headless --path . tools/art_cli.tscn -- preview prop barrel
godot --headless --path . tools/art_cli.tscn -- placeholders     # regenerate the coloured placeholder set
godot --headless --path . --import                                # after pack, before running the game
```

## Asset types

| Type | Files | Size | Notes |
| --- | --- | --- | --- |
| `floor` | `tile.png` | 64×32 | Diamond fills the image. Walkable. |
| `wall` | `tile.png` | 64×96 | Footprint diamond in the bottom 32 px, 64 px of height. Blocks; fades when occluding. |
| `door` | `closed.png`, `open.png` | 128×96 each | Footprint diamond centred at the bottom; door sits on the wall centre line. |
| `prop` | `sprite.png` | (W·64)×(H·32+height) | Footprint W×H tiles fills the bottom; `height` px of art above. Static obstacle. |

Types live in `src/Tooling/ArtPipeline/Types/BuiltinTypes.cs`. A type declares its
files/sizes and draws its placeholder + guide; `Packer.cs` decides how it is packed.

## Layout

```
assets/art/<type>/<name>/          artist sources (.gdignore — Godot never imports these)
assets/generated/tilesets/         floors.png, walls.png, inn.tres, tiles.json   (packed)
assets/generated/props/<name>.png  door sheets (closed|open) and prop sprites     (packed)
scenes/generated/props/<name>.tscn generated door / prop scenes                   (packed)
assets/characters/<name>/<name>.png 8-direction sheets (not yet in the pipeline)
scenes/characters/                 player.tscn, visitor.tscn
scenes/rooms/<room>.tscn           rooms, painted in the Godot editor
src/Tooling/ArtPipeline/           the pipeline (C#, runs inside Godot)
tools/art_pipeline.tscn            UI     tools/art_cli.tscn  headless CLI
```

Generated files are checked in so the game runs without a pack step.

`inn.tres` has three sources: **Floors** (0) and **Walls** (1) atlases, and **Props** (2), a
scene collection with every door and prop scene. `tiles.json` records which atlas cell or
scene-tile id each asset owns; packing reuses those slots, so adding or removing art never
moves tiles under already painted rooms. Do not edit it by hand.

## Conventions

Grid: isometric **diamond-down**, tile 64×32. Map `x` runs screen down-right, `y`
screen down-left. Tile centre in pixels: `((x - y) * 32 + 32, (x + y) * 16 + 16)`.

Wall tiles: `texture_origin (0,32)`, `y_sort_origin 8`, diamond physics polygon.
Floor tiles: diamond navigation polygon. Prop scenes: `StaticBody2D` (Y-sorted) +
`Sprite2D` + diamond `CollisionPolygon2D` (±W·32, ±H·16). The node origin is the centre of
the footprint's **anchor cell** (top-left), which is where a painted scene tile lands;
sprite and collision are shifted to the footprint centre so the prop still sorts and
blocks correctly. Door scenes: `Area2D` with `DoorNode.cs` (`DoorName` defaults to the
asset's display name), `Leaf` sprite `hframes=2` (0 closed, 1 open), trigger circle,
`Blocker` StaticBody2D.

Character sheets (64×96 frames, feet bottom-centre): rows **S, SW, W, NW, N, NE, E, SE**,
column 0 idle, remaining columns walk cycle (`DirectionalSprite.cs`).

## Rooms

Rooms are Godot scenes painted in the editor with the packed TileSet. Create one with
**New room** (or `art_cli -- room <name> [WxH] [floor] [wall]`), open
`scenes/rooms/<name>.tscn`, select a layer and paint in the **TileMap** bottom panel:

| Layer | Source | Paint |
| --- | --- | --- |
| `Floor` | Floors | Walkable cells. Also put floor under doors. |
| `Walls` | Walls | Blocking cells. Erase the floor under them. |
| `Props` | Props | Doors and props. Click the **anchor cell** (top-left of the footprint); the rest of the footprint is blocked by the prop's collision, not by the grid, so don't paint another prop there. |

Everything with per-instance data is a normal node, placed by hand:

- **Visitors**: instance `scenes/characters/visitor.tscn` under `Actors`, set `VisitorName`
  and `Lines`. Snap: grid step 32×16 (tile centres are at `((x−y)·32+32, (x+y)·16+16)`).
- **Special doors/props** (unique `DoorName`, `StartsLocked`…): instance the generated
  scene under `Props` as a child node instead of painting it; painted scene tiles all share
  the scene's defaults.
- **Paths, triggers, anything custom**: plain nodes under the room; only nodes with
  collision or navigation shapes affect the navmesh bake.

Scene structure (all Y-sorted so walls and props occlude actors correctly):

```
<Room> (Node2D, y_sort, group navigation_source)
  Floor (TileMapLayer, z_index -1, navigation_enabled off)
  Walls (TileMapLayer, y_sort, OccludingWallLayer.cs)
  Props (TileMapLayer, y_sort, scene tiles + hand-placed exceptions)
  PlayerSpawn (Marker2D)
  Actors/ Visitor1.. + Player spawned at runtime
```

Everything under the room root is parsed by the navmesh bake, so keep decorative
nodes (click marker, labels) out of the room scene — they live in `main.tscn`.
`Main` instantiates the room from its `RoomScene` export, or from `--room=res://…`
passed after `--` on the command line. **Test in game** builds
`scenes/rooms/_preview.tscn` around the selected asset the same way.

## Navigation

`main.tscn` has one `NavigationRegion2D` baking from the `navigation_source` group
with both mesh and collider parsing: floor navigation polygons are traversable, wall
tiles and `StaticBody2D`s are obstructions. `Main.cs` rebakes when an interactable
raises `NavigationChanged`. Floor layers must keep `navigation_enabled = false`.

## Characters

Not yet type-driven. Draw the 8×N sheet to `assets/characters/<name>/<name>.png`,
instance `scenes/characters/visitor.tscn`, set `Sprite.texture`/`hframes`,
`VisitorName` and `Lines`.