# Skill: Blazor Development — belongs to platform-web, not platform-final

**platform-final has no UI.** Blazor/component/page/CSS/JS-interop work lives in the sibling
**`platform-web`** repo. Do **not** create UI code in platform-final.

## If a task is UI work
- Switch context to `platform-web` (a separate repo with its own `.claude/` + `docs/`).
- For how platform-web consumes platform-final, see (in this repo, for reference only):
  [docs/UIArchitecture.md](../../docs/UIArchitecture.md), [docs/ComponentHierarchy.md](../../docs/ComponentHierarchy.md),
  [docs/StateManagement.md](../../docs/StateManagement.md), [docs/Rendering.md](../../docs/Rendering.md),
  [docs/JavaScriptInterop.md](../../docs/JavaScriptInterop.md), [docs/Navigation.md](../../docs/Navigation.md),
  [docs/SharedComponents.md](../../docs/SharedComponents.md).

## If the task is the *backend* of a UI feature
That IS platform-final work — use the **Feature Development** skill and the gateway/WebSocket contracts
([docs/PlatformApi.md](../../docs/PlatformApi.md), [docs/WebSocket.md](../../docs/WebSocket.md)).
