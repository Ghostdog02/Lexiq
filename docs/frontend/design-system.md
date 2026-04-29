# Lexiq Frontend Design System

Visual reference. Read when building new components or pages.

> Quick conventions live in [`frontend/CLAUDE.md`](../../frontend/CLAUDE.md). This file is the catalog.

## CSS conventions

- **No `!important`** — increase specificity (Editor.js `::ng-deep` is the only exception).
- **`rem` units** for new styles, base 16px (`24px` → `1.5rem`).
- **No hardcoded hex/rgba** — use CSS vars from `src/styles.scss`. For tints: `rgba(var(--accent-rgb), 0.15)`.
- **Sass `@use` only** — `@use 'shared/styles' as styles;` then `@include styles.animated-background`. `@use` must come before `:root` and any other rule.
- **Mixin scope** — `transition` belongs to the calling rule, never inside a visual mixin.
- **Component SCSS** — always `.scss`, configured in `angular.json`.
- **`transition: all` is banned** — enumerate the actual changing properties (`background, color, transform, border-color`).
- **Extract repeated easing** as a Sass `$_ease` variable at the top of the file.

## Shared mixins

| Mixin | Path | Use |
|-------|------|-----|
| `buttons.system` | `shared/_buttons.scss` | `.btn` + variants (primary, secondary, small, icon-only, link-btn, success, large, no-exercises-btn) |
| `cards.system` | `shared/_cards.scss` | `.card` glass morphism with inner glow, responsive |
| `mixins.glass-card` | `shared/_mixins.scss` | Bare glassmorphic base — no transition |
| `state-feedback.*` | `shared/_state-feedback.scss` | Correct/incorrect/warning gradients & glows |

```scss
// State feedback usage
@use 'path/to/shared/state-feedback' as state;

.feedback.correct {
  @include state.state-feedback(var(--color-correct-rgb), 'top right');
  @include state.state-gradient-background(var(--color-correct-rgb), 0.18, 0.12);
}
.option.incorrect {
  @include state.option-state(var(--color-error-rgb));
}
```

Available state-feedback mixins:

```scss
state-feedback($rgb, $position)
state-gradient-background($rgb, $opacity-start, $opacity-end, $border-opacity, $glow-intensity)
radial-overlay($rgb, $position, $opacity, $radius)
state-glow($rgb, $intensity, $spread)
option-state($rgb, $opacity)
```

## Color tokens (in `src/styles.scss`)

```scss
// Backgrounds
--bg-dark:  #0f1419;
--bg:       #1a2429;
--panel:    #1e2732;

// Accents (purple)
--accent:        #7c5cff;
--accent-rgb:    124, 92, 255;
--accent-light:  #9178ff;
--accent-dark:   #5a3ce6;

// Admin / privilege
--admin-gold:      #ffc107;
--admin-gold-dark: #ffa000;

// Text
--white:          #ffffff;
--text-secondary: #b8c4cf;
--muted:          #8b98a5;

// Glass
--glass:       rgba(255,255,255,0.04);
--glass-hover: rgba(255,255,255,0.08);
--border:      rgba(255,255,255,0.08);

// State
--color-correct:        #10b981;   --color-correct-rgb:   16, 185, 129;
--color-correct-light:  #34d399;
--color-error-light:    #f87171;
--color-xp:             #fbbf24;   --color-xp-rgb:        251, 191, 36;
--muted-rgb:            139, 152, 165;
--bg-rgb:               26, 36, 41;
--border-highlight:     rgba(255,255,255,0.2);
```

## Typography

- Font: `"Bricolage Grotesque", sans-serif` (Google Fonts).
- Primary text: `var(--white)` weight 600–800.
- Secondary: `var(--text-secondary)` weight 500.
- Muted: `var(--muted)`.

```scss
.title {
  font-size: 31px;
  font-weight: 800;
  letter-spacing: -0.3px;
  background: linear-gradient(135deg, var(--white) 0%, rgba(255,255,255,0.9) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}
.subtitle { color: var(--text-secondary); font-size: 18px; font-weight: 500; }
```

## Radius & shadow

- Cards / panels: `var(--radius)` = 16px.
- Buttons / pills: `var(--radius-sm)` = 100px (fully rounded).
- `--shadow: 0 20px 60px rgba(0,0,0,0.5);`
- `--shadow-hover: 0 24px 70px rgba(124, 92, 255, 0.15);`

## Glass morphism (canonical card)

```scss
.card {
  background: linear-gradient(135deg, rgba(255,255,255,0.06) 0%, rgba(255,255,255,0.02) 100%);
  border-radius: var(--radius);
  padding: 38px 40px;
  box-shadow: var(--shadow);
  border: 1px solid var(--border);
  backdrop-filter: blur(10px);
  position: relative;

  &::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: var(--radius);
    padding: 1px;
    background: linear-gradient(135deg, rgba(124, 92, 255, 0.2), transparent 50%, rgba(145, 120, 255, 0.1));
    -webkit-mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
            mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
    -webkit-mask-composite: xor;
            mask-composite: exclude;
    pointer-events: none;
  }
}
```

## Buttons

```scss
.btn.primary {
  background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dark) 100%);
  box-shadow: 0 8px 24px rgba(124, 92, 255, 0.25), 0 16px 48px rgba(90, 60, 230, 0.15);
  border-radius: var(--radius-sm);
  padding: 14px 18px;
  height: 50px;
  font-weight: 700; font-size: 15px;
  color: var(--white);
  border: 1px solid rgba(255,255,255,0.1);
  cursor: pointer;
  transition: background, box-shadow, transform 200ms cubic-bezier(0.4, 0, 0.2, 1);
  &:hover  { transform: translateY(-2px); box-shadow: 0 12px 32px rgba(124, 92, 255, 0.35), 0 20px 60px rgba(90, 60, 230, 0.25); }
  &:active { transform: translateY(0); }
}

.btn.oauth {
  background: var(--glass);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 14px 18px; height: 50px;
  font-weight: 600; font-size: 15px;
  color: var(--white);
  cursor: pointer;
  transition: background, border-color, transform, box-shadow 200ms cubic-bezier(0.4, 0, 0.2, 1);
  &:hover {
    background: var(--glass-hover);
    border-color: rgba(255,255,255,0.12);
    transform: translateY(-2px);
    box-shadow: var(--shadow-hover);
  }
}
```

## Hover & link patterns

```scss
transition: background, color, transform, box-shadow 200ms cubic-bezier(0.4, 0, 0.2, 1);
&:hover  { transform: translateY(-2px); box-shadow: var(--shadow-hover); }
&:active { transform: translateY(0); }

.link {
  color: var(--accent-light);
  text-decoration: none;
  transition: color 150ms ease;
  border-bottom: 1px solid transparent;
  &:hover { color: var(--accent-light); border-bottom-color: var(--accent-light); }
}
```

## Layout

```scss
// Split-screen auth pattern
.page {
  display: grid;
  grid-template-columns: 1fr 520px;
  min-height: 100vh;
  background: var(--bg);
}
```

Sidebar example: see `nav-bar` component.

## Responsive breakpoints

- Tablet: `@media (max-width: 1024px)` — single column, looser padding.
- Mobile: `@media (max-width: 480px)` — smaller text, tighter padding.

## Animations

```scss
&::before {
  content: '';
  position: absolute;
  background: radial-gradient(circle, rgba(124, 92, 255, 0.1) 0%, transparent 40%);
  animation: pulse 8s ease-in-out infinite;
}
```

## Accessibility

- `aria-label` on every interactive element.
- Semantic HTML — `<aside>`, `<nav>`, `<main>`.
- Focus states: `outline: 2px solid var(--accent); outline-offset: 3px;`.
- `role` attributes where appropriate.

## New-component checklist

- [ ] CSS custom properties (no hex/rgba literals).
- [ ] Glassmorphism for cards/panels.
- [ ] `.btn.primary` / `.btn.oauth` for buttons.
- [ ] Hover: `translateY(-2px)` + purple shadow.
- [ ] Radius: `var(--radius)` or `var(--radius-sm)`.
- [ ] Typography uses `var(--font-family)` and correct weights.
- [ ] Accent-light links with hover underline.
- [ ] 1024 / 480 responsive breakpoints.
- [ ] aria / role / focus outline.
- [ ] All sizes in `rem`.
- [ ] No `transition: all` — enumerate properties.
- [ ] Repeated easing extracted as `$_ease`.
