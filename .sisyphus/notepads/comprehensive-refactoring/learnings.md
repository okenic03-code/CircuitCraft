## Notepad: Learnings
<!-- Append-only. Do NOT overwrite. -->

- 2026-02-20: ComponentView static sprite/texture generation code can be extracted into `ComponentSymbolGenerator` without behavior change by moving all symbol drawing caches/helpers and keeping only callsites (`GetOrCreateFallbackSprite`, glow sprites, pin dot sprite/radius) in `ComponentView`.

- `WireRoutingController`: extracted repeated status literals into `StatusWiring`, `StatusWiringMode`, and `StatusReady` constants to satisfy inline-string cleanup.

- 2026-02-20: Extracted duplicated SI formatting into `CircuitUnitFormatter` (`CircuitCraft.Utils`) and switched `ComponentView.FormatComponentLabel` / `PowerFlowVisualizer` overlay formatting to shared static calls while preserving original thresholds and numeric precision (`0.##`, `0.000`, `0.###`).

- 2026-02-20: Extracted duplicated UI Toolkit pointer hit-testing from `PlacementController` and `WireRoutingController` into `UIInputHelper` (`CircuitCraft.Utils`) with identical behavior, including `TemplateContainer` exclusion and `GameView` subtree passthrough logic for wiring mode.

- 2026-02-20: LED/heat glow runtime object ownership can be isolated in a plain helper (`ComponentEffects`) while preserving behavior by keeping `ComponentView` serialized config fields in place and delegating unchanged public facade signatures (`ShowLEDGlow`, `HideLEDGlow`, `ShowResistorHeatGlow`, `HideResistorHeatGlow`) to the helper instance.

- 2026-02-20: Simulation overlay label lifecycle can be extracted to an `internal sealed` plain helper (`ComponentOverlay`) with constructor-injected parent/sprite/config while preserving `ComponentView` inspector fields and public facade API (`ShowSimulationOverlay(string)`, `HideSimulationOverlay()`) used by `PowerFlowVisualizer`.

- 2026-02-21: `ServiceRegistry` can gain extensibility without breaking callers by introducing a single `Dictionary<Type, object>` generic core (`Register<T>`, `Resolve<T>`, `Unregister<T>`) and keeping typed `GameManager`/`SimulationManager` properties and overloads as thin wrappers.

- 2026-02-21: `UIController` palette drag-resize code can be moved unchanged into a plain helper (`PaletteResizer`) by constructor-injecting `ComponentPalette`, `PaletteResizeHandle`, and root `VisualElement`, then delegating registration lifecycle in `OnEnable`/`OnDisable`; pointer capture, width clamping (280-420), and stop-propagation behavior remain intact.

- 2026-02-21: `UIController` StatusBar per-frame work can be reduced without changing output by wiring discrete updates to `GridCursor.OnPositionChanged` and `CommandHistory.OnHistoryChanged`, keeping only zoom as a lightweight per-frame dirty check when no camera zoom event exists.

- 2026-02-21: `CircuitUnitFormatterTests` (28 [Test] cases) added to `Assets/10_Scripts/30_Tests/10_Utils/`. Float literal threshold comparisons in `FormatCapacitance`/`FormatInductance` are safe even at very small values (1e-9f, 1e-12f) because C# uses the same float32 literal for both the test input and the formatter's `>=` threshold — identical bit patterns mean equality is guaranteed. Unity MCP was not reachable (port 8080 refused) so tests were verified by manual logic tracing against the formatter implementation.

- 2026-02-21: asmdef visibility follows assembly boundaries; moving `CircuitUnitFormatter` from `90_Utils` into `10_Core` keeps namespace `CircuitCraft.Utils` while allowing `CircuitCraft.Components` and `CircuitCraft.Tests` to consume it via `CircuitCraft.Core` references (`noEngineReferences: true`, `autoReferenced: true`).
