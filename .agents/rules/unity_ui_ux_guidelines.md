# Unity UI & UX Development Guidelines

## 1. Smooth UI Animations (DOTween)
- Use **DOTween** for all UI micro-interactions and screen transitions:
  - Button Clicks: Punch scale effect (`transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.15f, 10, 1).SetUpdate(true)`).
  - Popups / Modals: Scale up from 0 to 1 with elastic/back easing (`transform.DOScale(1f, 0.25f).From(0f).SetEase(Ease.OutBack).SetUpdate(true)`).
  - Screen Fades: Use `CanvasGroup.DOFade()` rather than altering individual graphic alpha.
  - Value Changes: Animate numbers and sliders smoothly using `DOVirtual.Float` or `slider.DOValue()`.

## 2. Responsive Canvas Design
- Set `CanvasScaler`:
  - UI Scale Mode: `Scale With Screen Size`.
  - Reference Resolution: `1920 x 1080` (or target orientation).
  - Screen Match Mode: `Match Width Or Height` with `matchWidthOrHeight = 0.5f`.
- Anchoring: Always configure RectTransform anchors appropriately to prevent UI drifting across aspect ratios.

## 3. Clean Architecture
- Separate View from Business Logic (MVP / Event-Driven):
  - View handles pure visuals, RectTransform references, and tween animations.
  - Presenter / Controller binds data and handles user events without hardcoupling to GameObjects.
