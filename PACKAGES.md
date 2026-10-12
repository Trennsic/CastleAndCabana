# Castle and Cabana — Package & Rebuild Manifest

This file is the rebuild manifest for the project. `Assets\_Project\` is the only
folder under version control; everything else in `Assets\` is reconstructed by
installing the packages listed below.

## Rebuild steps

1. Install Unity **6000.5.10f1** (see [Unity version](#unity-version)).
2. Create a new empty 3D (URP) Unity project.
3. Delete the generated `Assets\_Project` placeholder (if any) and clone this
   repo into `Assets\_Project`.
4. Open **Window → Package Manager** and install every package listed in
   [Package Manager packages](#package-manager-packages) (all are registry
   packages — no git/local-path setup required).
5. In the Package Manager, switch to **My Assets**, filter by **Imported**,
   and download + import each package listed in
   [Asset Store packages](#asset-store-packages), in any order.
6. Open `Assets\_Project\Castle-And-Cabana\Scenes\SampleScene.unity` (the real
   game scene — **not** `Assets\Scenes\SampleScene.unity`, which is Unity's
   empty template scene left over from project creation).

---

## Unity version

From `ProjectSettings\ProjectVersion.txt`:

```
m_EditorVersion: 6000.5.10f1
m_EditorVersionWithRevision: 6000.5.10f1 (3bd4f66ad299)
```

---

## Package Manager packages

All dependencies in `Packages\manifest.json` are standard Unity **registry**
packages resolved from `https://packages.unity.com` (or built-in modules) —
**no git or local-path dependencies**, and **no scoped registries** are
configured. Versions cross-checked against `Packages\packages-lock.json`:
every direct dependency below resolves to exactly the version requested (no
overrides).

| Package | Version | Source | Purpose |
|---|---|---|---|
| com.unity.2d.pixel-perfect | 6.0.0 | registry | 2D pixel-perfect camera |
| com.unity.2d.sprite | 1.0.0 | builtin | Sprite editor support |
| com.unity.2d.tilemap | 1.0.0 | builtin | Tilemap system |
| com.unity.2d.tilemap.extras | 8.0.3 | registry | Extra tilemap brushes/tools |
| com.unity.ai.navigation | 2.0.14 | registry | NavMesh / pathfinding |
| com.unity.cinemachine | 3.1.7 | registry | Camera rigs |
| com.unity.ide.rider | 3.0.39 | registry | Rider IDE integration |
| com.unity.ide.visualstudio | 2.0.27 | registry | Visual Studio IDE integration |
| com.unity.inputsystem | 1.20.0 | registry | New Input System (used by `InputManager.cs`) |
| com.unity.mathematics | 1.4.0 | builtin | Math library |
| com.unity.multiplayer.center | 1.0.1 | builtin | Multiplayer setup hub (default template) |
| com.unity.nuget.newtonsoft-json | 3.2.2 | registry | JSON serialization |
| com.unity.postprocessing | 3.5.4 | registry | Legacy post-processing stack |
| com.unity.render-pipelines.universal | 17.5.0 | builtin | URP render pipeline |
| com.unity.shadergraph | 17.5.0 | builtin | Shader Graph |
| com.unity.test-framework | 1.7.0 | builtin | Unit testing |
| com.unity.timeline | 1.8.13 | registry | Timeline/cutscenes |
| com.unity.toonshader | 0.13.0-preview | registry | Toon shading |
| com.unity.ugui | 2.5.0 | builtin | uGUI (UI Toolkit predecessor) |
| com.unity.visualscripting | 1.9.12 | registry | Visual Scripting |
| com.unity.modules.* (34 entries) | 1.0.0 each | builtin | Standard built-in engine modules (physics, audio, animation, XR, video, etc.) — installed automatically with Unity, no action needed |

Transitive-only packages pulled in by the above (not direct dependencies, no
action needed — Package Manager resolves these automatically): `com.unity.burst`,
`com.unity.collections`, `com.unity.ext.nunit`, `com.unity.film-internal-utilities`,
`com.unity.nuget.mono-cecil`, `com.unity.render-pipelines.core`,
`com.unity.render-pipelines.universal-config`, `com.unity.searcher`,
`com.unity.settings-manager`, `com.unity.splines`, `com.unity.test-framework.performance`,
`com.unity.modules.hierarchycore`, `com.unity.modules.subsystems`.

---

## Asset Store packages

Authoritative list from Package Manager → My Assets (Imported filter).
Publishers and folder mappings below were cross-checked against the local
Asset Store cache at `%APPDATA%\Unity\Asset Store-5.x\<Publisher>\...` and the
installed `Assets\` folders.

| Asset Store name | Publisher | Version | Installed into |
|---|---|---|---|
| 3D Modern Menu UI | SlimUI | 1.3.1 | `Assets\SlimUI` |
| Bamao Pack : Fantasy GUI | BamaoGi | 1.1.0 | `Assets\Bamao` |
| Better UI | TheraBytes *(verify — Asset Store cache lists this package under publisher folder "there-it-is", not "Thera Bytes"; imported folder is named `TheraBytes`, which is the long-standing publisher name for this asset)* | 3.2 | `Assets\TheraBytes\BetterUI` |
| Buttons Set | KartInnka | 1.0 | `Assets\Buttons` |
| Damage Numbers Pro | Ekincan Tas | 4.55 | `Assets\DamageNumbersPro` |
| Feel | More Mountains | 6.2 | `Assets\Feel` (also brings `Feel\MMFeedbacks`, `Feel\MMTools` (MoreMountains Tools), `Feel\NiceVibrations` (Lofelt NiceVibrations), `Feel\FeelDemos`, `Feel\FeelDemosURP`, `Feel\FeelDemosHDRP`, `Feel\Skills` as subfolders — no separate top-level MoreMountains/Lofelt folders exist) |
| Grasslands - Stylized Nature | Raygeas | 1.0.1 | `Assets\Raygeas` |
| GUI - Casual Fantasy | LayerLab *(Asset Store cache folder is "LAYERLAB"; installed Assets folder is "Layer Lab" — same publisher, inconsistent casing/spacing between the two)* | 2.0.4 | `Assets\Layer Lab` |
| Human Mega Animations Pack | Kevin Iglesias | 2.0 | `Assets\Kevin Iglesias` |
| KayKit - Skeletons Character Pack for Unity | Kay Lousberg | 1.1.1 *(verify — cache stores no version metadata for `.unitypackage` files; could not independently confirm beyond the value you supplied)* | `Assets\KayKit` (Skeletons pack + its bundled URP conversion sub-package only — the publisher's other KayKit packs, e.g. Platformer Pack, Dungeon Remastered, Resource Bits, are present in the Asset Store cache but **not** imported into this project) |
| Little Heroes Pack | SURIYUN | 3.2 | `Assets\Suriyun` |
| Polygon Arsenal | Archanor VFX | 2.07 | `Assets\Polygon Arsenal` |
| POLYGON - Dungeon Realms Pack - Art by Synty | Synty Studios | 1.4.5 | `Assets\PolygonDungeonRealms` (+ shared `Assets\Synty` common assets) |
| POLYGON - Dungeons Pack - Art by Synty | Synty Studios | 1.1.? *(verify — truncated in source screenshot; cache has no separate version metadata)* | `Assets\PolygonDungeon` (+ shared `Assets\Synty`) |
| POLYGON - Fantasy Dungeon Map - Art by Synty | Synty Studios | 1.3.2 | `Assets\PolygonDungeonMap` (+ shared `Assets\Synty`) |
| POLYGON - Fantasy Kingdom Pack - Art by Synty | Synty Studios | 1.1.? *(verify — truncated in source screenshot)* | `Assets\PolygonFantasyKingdom` (+ shared `Assets\Synty`) |
| POLYGON - Fantasy Rivals Pack - Art by Synty | Synty Studios | 1.4.2 | `Assets\PolygonFantasyRivals` (+ shared `Assets\Synty`) |
| POLYGON - Prototype Pack - Art by Synty | Synty Studios | 1.9.2 | `Assets\PolygonPrototype` (+ shared `Assets\Synty`) |
| RPG Monster Partners PBR Polyart | Dungeon Mason | 1.5 | `Assets\RPGMonsterPartnersPBRPolyart` |
| RPG Monster Wave Polyart | Dungeon Mason | 1.5 | `Assets\RPG Monster Wave Polyart` |
| Target Training Dummy Evolution Pack Cute Series | Meshtint Studio | 1.0 | `Assets\Target Training Dummy Evolution Pack Cute Series` |

**Shared dependency note:** all six Synty "POLYGON" packs above share common
assets installed into the single top-level `Assets\Synty` folder — reinstall
at least one Synty pack (any order) to regenerate it, then the rest.

---

## Unmatched folders

Top-level `Assets\` folders not accounted for by any package above:

- `Assets\Scenes` — Unity-generated (default template scene created with the
  project). Contains only the stock empty `SampleScene.unity` (432 lines) and
  is still referenced in `ProjectSettings\EditorBuildSettings.asset` as the
  sole build scene. **This is not your game scene** — the real content
  (`TowerDefenseManager` and everything else, ~299k lines) already lives at
  `Assets\_Project\Castle-And-Cabana\Scenes\SampleScene.unity`, correctly
  inside `_Project`. Nothing needs to move, but note that the project's
  actual build scene list currently points at the wrong (empty) scene — you
  may want to fix that in Build Settings separately.
- `Assets\Settings` — Unity-generated (default URP render pipeline/volume
  profile assets created with the project template).
- `Assets\TextMesh Pro` — Unity-generated (imported automatically the first
  time TextMeshPro is used).
- `Assets\TutorialInfo` — Unity-generated (leftover from the default
  3D-with-URP project template; safe to ignore/delete, not part of any
  package above).

## Unmatched packages

None — every package in the table above was matched to an installed
top-level `Assets\` folder.
