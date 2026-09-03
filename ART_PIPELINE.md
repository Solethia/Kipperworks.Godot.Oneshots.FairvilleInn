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
5. **Pack all + rebuild rooms** after editing existing art or `rooms/*.txt`.

The same steps run headless:

```
godot --headless --path . tools/art_cli.tscn -- new floor carpet_red "Red carpet"
godot --headless --path . tools/art_cli.tscn -- new prop barrel "Barrel" 1x1 72
godot --headless --path . tools/art_cli.tscn -- pack
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
rooms/<room>.txt  ->  scenes/rooms/<room>.tscn
src/Tooling/ArtPipeline/           the pipeline (C#, runs inside Godot)
tools/art_pipeline.tscn            UI     tools/art_cli.tscn  headless CLI
```

Generated files are checked in so the game runs without a pack step.

## Conventions

Grid: isometric **diamond-down**, tile 64×32. Map `x` runs screen down-right, `y`
screen down-left. Tile centre in pixels: `((x - y) * 32 + 32, (x + y) * 16 + 16)`.

Wall tiles: `texture_origin (0,32)`, `y_sort_origin 8`, diamond physics polygon.
Floor tiles: diamond navigation polygon. Prop scenes: `StaticBody2D` + `Sprite2D`
(offset so the footprint centre is on the origin) + diamond `CollisionPolygon2D`
(±W·32, ±H·16). Door scenes: `Area2D` with `DoorNode.cs`, `Leaf` sprite `hframes=2`
(0 closed, 1 open), trigger circle, `Blocker` StaticBody2D.

Character sheets (64×96 frames, feet bottom-centre): rows **S, SW, W, NW, N, NE, E, SE**,
column 0 idle, remaining columns walk cycle (`DirectionalSprite.cs`).

## Rooms

```
door D: cellar_door | cellar door
visitor M: Innkeeper Marla | first line | second line
prop T: table_large
floor c: carpet_red
---
##########
#.TT.P...D,,,#
#.TT..cc.#
##########
```

Header keys: `floor <ch>: <asset>`, `wall <ch>: <asset>`, `door <ch>: <asset> [| display]`,
`prop <ch>: <asset>`, `visitor <ch>: <name> | lines…`. Defaults: `.` wood, `,` stone,
`#` plaster, `P` player spawn, space = void. Doors, visitors, props and `P` stand on
`.`. Multi-tile props are drawn as a full W×H block of their char (footprint comes from
`meta.json`), anchored at the top-left cell.

Generated scene structure (all Y-sorted so walls occlude actors correctly):

```
<Room> (Node2D, y_sort, group navigation_source)
  Floor (TileMapLayer, z_index -1, navigation_enabled off)
  Walls (TileMapLayer, y_sort, OccludingWallLayer.cs)
  PlayerSpawn (Marker2D)
  Props/  Door1.., TableLarge1..
  Actors/ Visitor1.. + Player spawned at runtime
```

Everything under the room root is parsed by the navmesh bake, so keep decorative
nodes (click marker, labels) out of the room scene — they live in `main.tscn`.
`Main` instantiates the room from its `RoomScene` export, or from `--room=res://…`
passed after `--` on the command line.

## Navigation

`main.tscn` has one `NavigationRegion2D` baking from the `navigation_source` group
with both mesh and collider parsing: floor navigation polygons are traversable, wall
tiles and `StaticBody2D`s are obstructions. `Main.cs` rebakes when an interactable
raises `NavigationChanged`. Floor layers must keep `navigation_enabled = false`.

## Characters

Not yet type-driven. Draw the 8×N sheet to `assets/characters/<name>/<name>.png`,
instance `scenes/characters/visitor.tscn`, set `Sprite.texture`/`hframes`,
`VisitorName` and `Lines`.