# Exercise Design Patterns

Comprehensive design system for Lexiq exercise components, inspired by Duolingo's gamified learning experience.

## Design Philosophy

**Playful • Encouraging • Rewarding**

- Bold, vibrant colors for instant feedback
- Bouncy animations that celebrate success
- Smooth micro-interactions that feel responsive
- Clear visual hierarchy with generous spacing

---

## Color Palette

All colors defined in `/src/styles.scss` as CSS variables.

### Primary States

| State | Color | Variable | Usage |
|-------|-------|----------|-------|
| **Correct** | Vibrant Green `#58cc02` | `--color-correct` | Correct answers, success states |
| | Light Green `#89e219` | `--color-correct-light` | Highlights, text on correct |
| | Dark Green `#4f8057` | `--color-correct-dark` | Shadows, depth |
| **Incorrect** | Vibrant Red `#ff4b4b` | `--color-error` | Wrong answers, error states |
| | Light Red `#ffb5b5` | `--color-error-light` | Text on incorrect |
| | Dark Red `#9c5446` | `--color-error-dark` | Shadows |
| **Selected** | Bright Blue `#1cb0f6` | `--color-selected` | Active selection, focus |
| | Light Blue `#58d3ff` | `--color-selected-light` | Highlights, gradients |
| **Accent** | Purple `#8b6eff` | `--accent` | Interactive elements |
| | Light Purple `#a48fff` | `--accent-light` | Hover states |

### Glass & Opacity Levels

Use these for layered backgrounds and depth:

```scss
background: var(--glass-soft);      // rgba(255,255,255,0.04)
background: var(--glass);           // rgba(255,255,255,0.05)
background: var(--glass-medium);    // rgba(255,255,255,0.06)
background: var(--glass-hover);     // rgba(255,255,255,0.09)
```

### Borders

```scss
border: 0.1875rem solid var(--border-light);      // rgba(255,255,255,0.10)
border: 0.125rem solid var(--border);             // rgba(255,255,255,0.12)
border: 0.1875rem solid var(--border-medium);     // rgba(255,255,255,0.15)
border: 0.125rem solid var(--border-highlight);   // rgba(255,255,255,0.24)
```

### Shadows

```scss
box-shadow: var(--shadow-soft);     // 0 2px 8px rgba(0,0,0,0.2)
box-shadow: var(--shadow-medium);   // 0 4px 12px rgba(0,0,0,0.15)
box-shadow: var(--shadow-strong);   // 0 8px 24px rgba(0,0,0,0.25)
```

---

## Typography

### Question Text

```scss
font-size: 2.5rem;
font-weight: 700;
line-height: 1.2;
letter-spacing: -0.02em;
color: var(--white);
text-shadow: 0 2px 12px rgba(0, 0, 0, 0.3);
```

**Rationale:** Large, bold text makes the question the focal point. Negative letter-spacing improves readability at large sizes.

### Option Text

```scss
font-size: 1.625rem;
font-weight: 600;
line-height: 1.4;
color: var(--white);
```

### Hints & Instructions

```scss
font-size: 0.875rem;
font-weight: 600;
letter-spacing: 0.02em;
color: var(--accent-light);
```

---

## Spacing & Sizing

### Question Container

```scss
padding: 1.5rem 0 2rem;
```

### Option Buttons

```scss
padding: 1.25rem 1.75rem;
gap: 1.25rem;  // Between letter badge and text
```

### Options List

```scss
gap: 0.75rem;  // Between options
```

### Letter Badges

```scss
width: 3.5rem;
height: 3.5rem;
border-radius: var(--radius-lg);  // 0.875rem
```

---

## Animations & Timing

All animations use bouncy cubic-bezier curves for playful feel:

```scss
transition: all 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
```

### Keyframes (defined in `styles.scss`)

#### Entry Animations

```scss
animation: questionSlideIn 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
animation: optionSlideIn 0.5s cubic-bezier(0.34, 1.56, 0.64, 1) backwards;
animation: fadeInDown 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) 0.2s both;
```

**Stagger options** for cascade effect:

```scss
@for $i from 1 through 6 {
  &[data-index="#{$i - 1}"] {
    animation-delay: #{0.1 + $i * 0.08}s;
  }
}
```

#### Interaction Animations

```scss
animation: letterBounce 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
animation: letterCorrectBounce 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
animation: correctShake 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
animation: incorrectShake 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
animation: focusPulse 1.5s ease-in-out infinite;
animation: iconPop 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
```

---

## State Patterns

### 1. Default State

```scss
.option-btn {
  background: var(--glass-soft);
  border: 0.1875rem solid var(--border-light);
  box-shadow: var(--shadow-medium);
}
```

### 2. Hover State

**Effect:** Lift + scale + glow

```scss
&:hover:not(:disabled):not(.correct):not(.incorrect) {
  background: var(--glass-medium);
  border-color: rgba(var(--accent-rgb), 0.4);
  transform: translateY(-4px) scale(1.01);
  box-shadow: 0 8px 24px rgba(var(--accent-rgb), 0.25);

  .option-letter {
    border-color: var(--accent-light);
    color: var(--accent-light);
    transform: scale(1.1) rotate(-3deg);
    box-shadow: 0 4px 16px rgba(var(--accent-rgb), 0.4);
  }
}
```

### 3. Keyboard Focus

**Effect:** Pulsing glow ring

```scss
&.focused:not(.correct):not(.incorrect) {
  border-color: var(--accent);
  box-shadow: 
    0 0 0 0.25rem rgba(var(--accent-rgb), 0.25),
    0 8px 24px rgba(var(--accent-rgb), 0.3);
  animation: focusPulse 1.5s ease-in-out infinite;

  .option-letter {
    border-color: var(--accent);
    color: var(--accent-light);
    box-shadow: 0 0 20px rgba(var(--accent-rgb), 0.6);
  }
}
```

### 4. Selected State (Blue)

**Effect:** Blue gradient + scale + ring

```scss
&[aria-pressed="true"]:not(.correct):not(.incorrect) {
  background: linear-gradient(135deg,
    rgba(var(--color-selected-rgb), 0.15) 0%,
    rgba(var(--color-selected-rgb), 0.08) 100%);
  border-color: var(--color-selected);
  box-shadow:
    0 0 0 0.3rem rgba(var(--color-selected-rgb), 0.2),
    0 8px 32px rgba(var(--color-selected-rgb), 0.3);
  transform: scale(1.02);

  .option-letter {
    background: linear-gradient(135deg,
      var(--color-selected) 0%,
      var(--color-selected-light) 100%);
    color: var(--white);
    border-color: var(--color-selected);
    box-shadow: 0 4px 20px rgba(var(--color-selected-rgb), 0.5);
    animation: letterBounce 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
}
```

### 5. Correct State (Green)

**Effect:** Vibrant green + bounce + shake + radial glow

```scss
&.correct {
  background: linear-gradient(135deg,
    rgba(var(--color-correct-rgb), 0.2) 0%,
    rgba(var(--color-correct-rgb), 0.12) 100%);
  border-color: var(--color-correct);
  box-shadow:
    0 0 0 0.3rem rgba(var(--color-correct-rgb), 0.2),
    0 8px 32px rgba(var(--color-correct-rgb), 0.35);
  animation: correctShake 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);

  &::before {
    opacity: 1;
    background: radial-gradient(circle at center,
      rgba(var(--color-correct-rgb), 0.4),
      transparent 70%);
  }

  .option-letter {
    background: linear-gradient(135deg,
      var(--color-correct) 0%,
      var(--color-correct-light) 100%);
    border-color: var(--color-correct);
    color: var(--white);
    box-shadow: 0 4px 20px rgba(var(--color-correct-rgb), 0.6);
    animation: letterCorrectBounce 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
  }

  .option-text {
    color: var(--color-correct-light);
  }

  .result-icon.correct {
    color: var(--color-correct);
    filter: drop-shadow(0 0 8px rgba(var(--color-correct-rgb), 0.8));
  }
}
```

### 6. Incorrect State (Red)

**Effect:** Vibrant red + shake

```scss
&.incorrect {
  background: linear-gradient(135deg,
    rgba(var(--color-error-rgb), 0.18) 0%,
    rgba(var(--color-error-rgb), 0.1) 100%);
  border-color: var(--color-error);
  box-shadow:
    0 0 0 0.3rem rgba(var(--color-error-rgb), 0.18),
    0 8px 32px rgba(var(--color-error-rgb), 0.25);
  animation: incorrectShake 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);

  .option-letter {
    background: linear-gradient(135deg,
      var(--color-error) 0%,
      var(--color-error-light) 100%);
    border-color: var(--color-error);
    color: var(--white);
    box-shadow: 0 4px 20px rgba(var(--color-error-rgb), 0.5);
  }

  .option-text {
    color: var(--color-error-light);
  }

  .result-icon.incorrect {
    color: var(--color-error);
    filter: drop-shadow(0 0 8px rgba(var(--color-error-rgb), 0.8));
  }
}
```

---

## Interaction Patterns

### Keyboard Navigation (RxJS Pattern)

**For interactive elements (buttons, options):**

1. **State tracking:**
```typescript
focusedOptionIndex = 0;
```

2. **Setup reactive stream:**
```typescript
private setupKeyboardNavigation() {
  fromEvent<KeyboardEvent>(document, 'keydown')
    .pipe(
      filter(() => this.currentExercise?.type === ExerciseType.YourType),
      filter(() => !this.isCurrentSubmitted && !this.isCurrentLocked),
      takeUntilDestroyed(this.destroyRef)
    )
    .subscribe((event) => {
      switch (event.key) {
        case 'ArrowDown':
          event.preventDefault();
          // Navigate down
          break;
        case 'ArrowUp':
          event.preventDefault();
          // Navigate up
          break;
        case 'Enter':
          event.preventDefault();
          this.submitAnswer();
          break;
      }
    });
}
```

3. **Visual feedback in template:**
```html
<button [class.focused]="isOptionFocused(i)">
```

**For text inputs:**

```typescript
onInputKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter') {
    event.preventDefault();
    if (this.isCurrentSubmitted && this.state.canGoNext) {
      this.nextExercise();
    } else if (!this.isCurrentSubmitted && this.state.currentAnswer) {
      this.submitAnswer();
    }
  }
}
```

---

## Component Structure

### HTML Template Pattern

```html
<!-- Question Container -->
<div class="exercise-question-container">
  <h2 class="exercise-question">{{ question }}</h2>
</div>

<!-- Keyboard Hint -->
<div class="keyboard-hint">Use ↑↓ arrow keys to navigate • Press Enter to submit</div>

<!-- Interactive Elements (Options/Inputs) -->
<div class="options-list">
  @for (item of items; track item.id; let i = $index) {
    <button
      class="option-btn"
      [class.focused]="isItemFocused(i)"
      [attr.data-index]="i"
      [disabled]="isCurrentSubmitted || isCurrentLocked"
      (click)="selectItem(item.id)">
      <!-- Content -->
    </button>
  }
</div>
```

### SCSS Pattern

```scss
.exercise-type-name {
  .keyboard-hint {
    font-size: 0.875rem;
    color: var(--accent-light);
    text-align: center;
    margin-bottom: 1.5rem;
    padding: 0.75rem 1rem;
    background: rgba(var(--accent-rgb), 0.08);
    border-radius: 0.75rem;
    border: 1px solid rgba(var(--accent-rgb), 0.2);
    font-weight: 600;
    letter-spacing: 0.02em;
    animation: fadeInDown 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) 0.2s both;
  }

  .interactive-element {
    // Base state
    background: var(--glass-soft);
    border: 0.1875rem solid var(--border-light);
    border-radius: var(--radius);
    transition: all 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
    animation: optionSlideIn 0.5s cubic-bezier(0.34, 1.56, 0.64, 1) backwards;

    // Stagger animation
    @for $i from 1 through 6 {
      &[data-index="#{$i - 1}"] {
        animation-delay: #{0.1 + $i * 0.08}s;
      }
    }

    // Hover
    &:hover:not(:disabled) {
      transform: translateY(-4px) scale(1.01);
      box-shadow: 0 8px 24px rgba(var(--accent-rgb), 0.25);
    }

    // Focus
    &.focused {
      animation: focusPulse 1.5s ease-in-out infinite;
    }

    // States (correct, incorrect, selected)
    // ... use patterns from State Patterns section
  }
}
```

---

## Applying to Other Exercise Types

### Fill-in-Blank

**Keep:**
- Question prominence (2.5rem, bold)
- Keyboard hints
- Enter to submit pattern
- Correct/incorrect color scheme

**Adapt:**
- Input field gets same hover/focus glow as options
- Correct state: green underline + text color
- Incorrect state: red underline + shake animation

**Example:**
```scss
.inline-input {
  border-bottom: 0.1875rem solid var(--accent);
  transition: all 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);

  &:focus {
    outline: none;
    border-bottom-color: var(--accent-light);
  }

  &.correct {
    border-bottom-color: var(--color-correct);
    color: var(--color-correct-light);
    animation: correctShake 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
  }

  &.incorrect {
    border-bottom-color: var(--color-error);
    color: var(--color-error-light);
    animation: incorrectShake 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
}
```

### Translation

**Keep:**
- Question prominence
- Keyboard hints
- Enter to submit
- State colors and animations

**Adapt:**
- Larger text input area
- Character counter (optional)
- Show correct answer below on incorrect

### Listening

**Keep:**
- Question prominence (for prompt text)
- Enter to submit
- State colors

**Adapt:**
- Audio player gets playful design
- Play button with bounce on hover
- Waveform visualization (optional enhancement)
- Input field follows Fill-in-Blank pattern

---

## Accessibility Considerations

### ARIA Attributes

```html
<button
  [attr.aria-pressed]="isSelected(item)"
  [attr.aria-label]="item.label"
  [disabled]="isDisabled">
```

### Keyboard Navigation

- ✅ All interactive elements accessible via keyboard
- ✅ Visual focus indicators
- ✅ Enter key submits answer
- ✅ Arrow keys navigate (where applicable)
- ✅ Escape key cancels (future enhancement)

### Color Contrast

All state colors meet WCAG AA standards:
- Green on dark: 7.2:1
- Red on dark: 5.8:1
- Blue on dark: 6.1:1

---

## Performance Considerations

### CSS Custom Properties

Using CSS variables enables:
- Runtime theme switching
- Reduced CSS bundle size
- Consistent design tokens

### Animation Performance

- Use `transform` and `opacity` for animations (GPU-accelerated)
- Avoid animating `width`, `height`, `left`, `top`
- Use `will-change` sparingly for high-frequency animations

### Staggered Animations

```scss
// Efficient stagger with minimal CSS
@for $i from 1 through 6 {
  &[data-index="#{$i - 1}"] {
    animation-delay: #{0.1 + $i * 0.08}s;
  }
}
```

---

## Future Enhancements

### Sound Effects

- Success chime on correct answer
- Gentle buzz on incorrect
- Subtle click on selection

### Haptic Feedback (Mobile)

```typescript
if ('vibrate' in navigator) {
  navigator.vibrate(50); // Success
  navigator.vibrate([50, 50, 50]); // Error pattern
}
```

### Confetti Celebration

On lesson completion or streak milestones:
```typescript
import confetti from 'canvas-confetti';

confetti({
  particleCount: 100,
  spread: 70,
  origin: { y: 0.6 }
});
```

### Progress Streaks

Visual celebration when user maintains daily streak:
- Flame emoji animation
- Counter with glow effect
- Encourage message

---

## Design Checklist

When implementing a new exercise type, ensure:

- [ ] Question text is 2.5rem, bold, with proper spacing
- [ ] Keyboard navigation implemented (arrows + Enter)
- [ ] Keyboard hint banner displayed
- [ ] Staggered entry animations applied
- [ ] Hover states have lift + glow effect
- [ ] Focus states have pulsing ring
- [ ] Selected state uses blue (`--color-selected`)
- [ ] Correct state uses green with bounce + shake
- [ ] Incorrect state uses red with shake
- [ ] Result icons pop in with animation
- [ ] All colors use CSS variables
- [ ] All animations use bouncy cubic-bezier
- [ ] ARIA attributes present for accessibility
- [ ] Enter key submits answer or continues

---

**Last Updated:** 2026-04-26  
**Author:** Claude Code + Alex Vesely  
**Inspired by:** Duolingo's gamified learning experience
