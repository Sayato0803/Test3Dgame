# Graph Report - Test3Dgame  (2026-10-25)

## Corpus Check
- Large corpus: 2319 files · ~128,369 words. Semantic extraction will be expensive (many Claude tokens). Consider running on a subfolder.

## Summary
- 816 nodes · 2142 edges · 33 communities (27 shown, 6 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 121 edges (avg confidence: 0.85)
- Token cost: 81,908 input · 0 output

## Community Hubs (Navigation)
- Unit Animations & Reactions
- Attack Range & Grid Cells
- Game Phases & HUD
- Editor Tools & File Modules
- Card View & Hand
- Cartoon Environment Builder
- Procedural SFX Synth
- Legacy Card Manager
- Legacy Mage & Projectiles
- Card Rule Tests & Finishers
- Phases State Machine
- GridUnit Component
- Character Rig
- King Stats & Battle Damage
- Camera Rig
- Battle Rules (Docs)
- Player Data & Win Check
- Code Architecture (Docs)
- Card Play & Targeting
- GUI Guide Prefabs
- Card Types
- Spell Card Rules & Workflow
- Card Target Modes
- Game Settings
- Project Conventions & HUD
- Attack Range Rules (Docs)
- Project Overview
- 9-slice Cartoon UI Style
- King & Win Condition Rules
- Unused Legacy Code
- Healing Card

## God Nodes (most connected - your core abstractions)
1. `Phases` - 102 edges
2. `GridUnit` - 64 edges
3. `UnitReactions` - 54 edges
4. `GridPlacer` - 51 edges
5. `GridSystem` - 51 edges
6. `GameHud` - 49 edges
7. `CardView` - 44 edges
8. `GridBattle` - 34 edges
9. `Player` - 33 edges
10. `SfxId` - 32 edges

## Surprising Connections (you probably didn't know these)
- `Project conventions` --semantically_similar_to--> `HUONG_DAN_GUI (GUI editing guide)`  [INFERRED] [semantically similar]
  Docs/GIOI_THIEU_DU_AN.md → Docs/HUONG_DAN_GUI.md
- `Battle resolution order` --rationale_for--> `Phases state machine (ResolveBattle)`  [INFERRED]
  Docs/LUAT_GAME.md → Docs/GIOI_THIEU_DU_AN.md
- `Phase flow (Deploy, Setup, Attack, End)` --rationale_for--> `Phases state machine (ResolveBattle)`  [INFERRED]
  Docs/LUAT_GAME.md → Docs/GIOI_THIEU_DU_AN.md
- `Card draw (5 first, then 1; 25% unit card; infinite deck)` --rationale_for--> `PhasesCards (Draw, card effects, TickStatuses)`  [INFERRED]
  Docs/LUAT_GAME.md → Docs/GIOI_THIEU_DU_AN.md
- `Status duration counting` --rationale_for--> `PhasesCards (Draw, card effects, TickStatuses)`  [INFERRED]
  Docs/LUAT_GAME.md → Docs/GIOI_THIEU_DU_AN.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Nine implemented spell cards** — docs_luat_game_dmg_boost, docs_luat_game_reflect, docs_luat_game_shield, docs_luat_game_chest, docs_luat_game_rift, docs_luat_game_tornado, docs_luat_game_healing, docs_luat_game_suicide_bomb, docs_luat_game_clone [EXTRACTED 1.00]
- **CONFIRM battle code flow** — docs_gioi_thieu_du_an_gamehud, docs_gioi_thieu_du_an_phases, docs_gioi_thieu_du_an_unitreactions, docs_gioi_thieu_du_an_cartoonfx, docs_gioi_thieu_du_an_sfx [EXTRACTED 1.00]
- **Card use flow** — docs_gioi_thieu_du_an_cardhand, docs_gioi_thieu_du_an_gridplacer, docs_gioi_thieu_du_an_phasescards, docs_gioi_thieu_du_an_carddata [EXTRACTED 1.00]

## Communities (33 total, 6 thin omitted)

### Community 0 - "Unit Animations & Reactions"
Cohesion: 0.07
Nodes (9): UnitReactions, Anim, Feet, IsShown, TopHeight, Visual, Billboard, CartoonFx (+1 more)

### Community 1 - "Attack Range & Grid Cells"
Cohesion: 0.06
Nodes (23): AttackRange, Cell, Attacker, Coord, King, UnitKind, King, Knight (+15 more)

### Community 2 - "Game Phases & HUD"
Cohesion: 0.05
Nodes (17): GamePhase, Attack, Deploy, End, GameOver, Setup, GameHud, IsPaused (+9 more)

### Community 3 - "Editor Tools & File Modules"
Cohesion: 0.08
Nodes (6): GuiSpriteImporter, PlayFromMainMenu, CardScripts.Editor, GridBattle, GridBattle.EditorTools, CardScripts

### Community 4 - "Card View & Hand"
Cohesion: 0.06
Nodes (9): CardView, ClickHandler, IsConsumed, RightClickHandler, SpawnPrefab, Tag, ButtonPop, CardHand (+1 more)

### Community 5 - "Cartoon Environment Builder"
Cohesion: 0.08
Nodes (6): CartoonEnvironmentBuilder, CartoonAssets, Instance, Sway, OutlineNormals, ToonStyle

### Community 6 - "Procedural SFX Synth"
Cohesion: 0.07
Nodes (29): Sfx, SfxId, Boing, CameraWhoosh, CardDeal, CardFlip, CardHover, Chime (+21 more)

### Community 7 - "Legacy Card Manager"
Cohesion: 0.11
Nodes (6): CardEntityFlight, CardManager, Instance, CardSceneBuilder, CardFx, StatusFx

### Community 8 - "Legacy Mage & Projectiles"
Cohesion: 0.07
Nodes (10): FlyingCube, MageScript, BulletDeflector, CameraManager, FireboltBehavior, PlayerShooting, CloudDrift, SkyCycle (+2 more)

### Community 10 - "Phases State Machine"
Cohesion: 0.08
Nodes (14): Phases, BattleReport, Current, CurrentPlayer, HintText, IsAnimating, IsHandingOver, KingsPerPlayer (+6 more)

### Community 11 - "GridUnit Component"
Cohesion: 0.12
Nodes (12): GridUnit, BombTurns, Coord, DamageBoost, Grid, IsOnGrid, Kind, Owner (+4 more)

### Community 12 - "Character Rig"
Cohesion: 0.20
Nodes (4): CharacterRig, EyeShape, Part, FlagWave

### Community 13 - "King Stats & Battle Damage"
Cohesion: 0.24
Nodes (3): KingStats, IsDead, KingHitResult

### Community 14 - "Camera Rig"
Cohesion: 0.18
Nodes (5): CameraRig, Camera, CurrentIndex, Instance, IsMoving

### Community 15 - "Battle Rules (Docs)"
Cohesion: 0.16
Nodes (10): GridUnit component (PlacedAt, status), Unfinished work / next ideas, Brawler (not implemented), DMG Boost, Rule points pending confirmation, Reflect, Scout card (3/match, 2x2 reveal), Shield (+2 more)

### Community 16 - "Player Data & Win Check"
Cohesion: 0.15
Nodes (12): TornadoTrap, Player, AttackCameraIndex, Attackers, Board, CameraIndex, DisplayName, Hand (+4 more)

### Community 17 - "Code Architecture (Docs)"
Cohesion: 0.19
Nodes (13): CameraRig, CardHand (Master Duel style hand), CardScripts legacy namespace (CardView), CartoonFx / CardFx / StatusFx visuals, Cell (King slot + attacker slot), GridPlacer (mouse input, BeginCard), GridSystem (10x10 board), Phases state machine (ResolveBattle) (+5 more)

### Community 18 - "Card Play & Targeting"
Cohesion: 0.33
Nodes (3): CardData, PickCount, Target

### Community 19 - "GUI Guide Prefabs"
Cohesion: 0.18
Nodes (10): PlayFromMainMenu editor script, Sfx (Sfx.Play(SfxId)) + SfxLibrary, HUONG_DAN_GUI (GUI editing guide), Card prefab (Body, ShineSweep), DamageText prefab, MainMenu background (Showcase, orbit camera, Environment, PostFX), MainMenu scene MenuCanvas, SceneFader prefab (+2 more)

### Community 20 - "Card Types"
Cohesion: 0.20
Nodes (10): CardType, Chest, Clone, DmgBoost, Healing, Reflect, Rift, Shield (+2 more)

### Community 21 - "Spell Card Rules & Workflow"
Cohesion: 0.22
Nodes (7): CardData ScriptableObject (CardType, Target, PickCount), CardRuleTests (EditMode tests), PhasesCards (Draw, card effects, TickStatuses), Battle Report (hold Tab, card names only), Card classification table (Attacker / King / Board), Chest, Spell cards (usage rules)

### Community 22 - "Card Target Modes"
Cohesion: 0.25
Nodes (8): CardTarget, Area2x2, KingThenCell, OwnAttacker, OwnKing, OwnUnit, Self, TwoCells

### Community 23 - "Game Settings"
Cohesion: 0.25
Nodes (5): GameSettings, CameraSpeed, Fullscreen, Quality, Volume

### Community 24 - "Project Conventions & HUD"
Cohesion: 0.29
Nodes (6): GameHud (top bar, CONFIRM, Battle Report, pause, win), GridTest scene (match), ARCO for OSX font, F1 hide HUD (Hide On F1), GameUI/HudCanvas, Assets/Resources/GUI image folders

### Community 26 - "Attack Range Rules (Docs)"
Cohesion: 0.33
Nodes (4): AttackRange (static range + Rift Walk), Knight (range 3), Mage (range 4), Rift (two-way portals)

### Community 27 - "Project Overview"
Cohesion: 0.50
Nodes (5): GIOI_THIEU_DU_AN (Project Overview), King Hunt Game, Unity 6000.0.75f1 / URP 17 / DOTween stack, LUAT_GAME (Game Rules, source of truth), Hot-seat 2-player with turn cover screen

### Community 29 - "King & Win Condition Rules"
Cohesion: 0.50
Nodes (4): KingHpBar prefab, Clone (fake King), King (10 HP, 2 per player), Win condition (all real Kings dead)

## Knowledge Gaps
- **150 isolated node(s):** `Instance`, `SpawnPrefab`, `RightClickHandler`, `Tag`, `IsConsumed` (+145 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 237 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Phases` connect `Phases State Machine` to `Unit Animations & Reactions`, `Attack Range & Grid Cells`, `Game Phases & HUD`, `Editor Tools & File Modules`, `Card View & Hand`, `Cartoon Environment Builder`, `Procedural SFX Synth`, `Legacy Card Manager`, `Legacy Mage & Projectiles`, `Card Rule Tests & Finishers`, `GridUnit Component`, `King Stats & Battle Damage`, `Player Data & Win Check`, `Card Play & Targeting`, `Status & King Visibility`?**
  _High betweenness centrality (0.147) - this node is a cross-community bridge._
- **What connects `Instance`, `SpawnPrefab`, `RightClickHandler` to the rest of the system?**
  _150 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Unit Animations & Reactions` be split into smaller, more focused modules?**
  _Cohesion score 0.06827309236947791 - nodes in this community are weakly interconnected._
- **Why does `GameHud` connect `Game Phases & HUD` to `Attack Range & Grid Cells`, `Editor Tools & File Modules`, `Card View & Hand`, `Cartoon Environment Builder`, `Legacy Card Manager`, `Legacy Mage & Projectiles`, `Phases State Machine`, `GridUnit Component`, `Player Data & Win Check`?**
  _High betweenness centrality (0.079) - this node is a cross-community bridge._
- **Should `Attack Range & Grid Cells` be split into smaller, more focused modules?**
  _Cohesion score 0.0629746835443038 - nodes in this community are weakly interconnected._
- **Why does `GridPlacer` connect `Attack Range & Grid Cells` to `Game Phases & HUD`, `Editor Tools & File Modules`, `Card View & Hand`, `Legacy Card Manager`, `Legacy Mage & Projectiles`, `Card Rule Tests & Finishers`, `Phases State Machine`, `GridUnit Component`, `Camera Rig`, `Card Play & Targeting`?**
  _High betweenness centrality (0.074) - this node is a cross-community bridge._
- **Should `Game Phases & HUD` be split into smaller, more focused modules?**
  _Cohesion score 0.05217391304347826 - nodes in this community are weakly interconnected._