# Frontend CLAUDE.md

Angular 21 single-page application with standalone components.

## Development Commands

```bash
# Install dependencies
npm install

# Start development server (http://localhost:4200)
npm start
# or
ng serve

# Build for production
npm run build
# or
ng build

# Build with watch mode
npm run watch

# Run unit tests with Karma
npm test
# or
ng test

# Generate new component/service/etc
ng generate component <name>
ng generate service <name>
```

## Project Structure

```
frontend/src/app/
├── auth/
│   ├── auth.service.ts              # Auth state (BehaviorSubject), login/logout
│   └── google-login/                # Google OAuth login component
├── features/
│   ├── lessons/
│   │   ├── components/
│   │   │   ├── home/                # Main dashboard (route: /)
│   │   │   ├── lesson-editor/       # Create/edit lessons (route: /create-lesson, lazy)
│   │   │   └── lesson-viewer/       # View lesson content (route: /lesson/:id, lazy)
│   │   ├── models/                  # course.interface, lesson.interface, exercise.interface
│   │   └── services/                # lesson.service, lesson-form.service
│   └── users/
│       ├── components/
│       │   ├── leaderboard/         # User rankings
│       │   └── profile/             # User profile & achievements
│       └── models/                  # leaderboard.interface, user.model
├── help/                            # Help & FAQ (component + service)
├── nav-bar/                         # Navigation sidebar
├── not-found/                       # 404 page (lazy loaded)
├── shared/
│   ├── _buttons.scss               # Shared button mixin (@include buttons.system)
│   ├── _cards.scss                 # Shared card mixin (@include cards.system)
│   ├── components/
│   │   └── editor/                  # EditorJS ControlValueAccessor wrapper + dark theme
│   └── services/
├── app.routes.ts                    # Route definitions
└── app.config.ts                    # App configuration & providers
```

## Key Patterns

- Uses **standalone components** (no NgModule) — Angular 21+ approach
- **Routing** is defined in `app.routes.ts`
- **Lazy loading** for dynamic routes (lesson/:id, create-lesson, 404)
- **Environment variables** via `@ngx-env/builder` (prefix: `NG_` or `BACKEND_`)
- **State management**: Service-based with RxJS Observables (no Redux/NgRx)
  - AuthService uses BehaviorSubject for auth state
  - LessonService uses Subject for event broadcasting
- **Dependency injection**: Always use `inject()` function — do NOT use constructor injection
- **Subscription cleanup** via `takeUntilDestroyed(DestroyRef)` operator
- **Component styles**: Always use SCSS (configured in `angular.json`)
- **Bootstrap 5** is included but should be used minimally (prefer custom design system)

## Environment Variables

Frontend env vars are passed as **build arguments** in `docker-compose.yml`, not via a secrets file:

```
NG_GOOGLE_CLIENT_ID=<google-oauth-client-id>
BACKEND_API_URL=/api            # proxied through nginx; not a direct backend URL
```

## Angular Patterns & Best Practices

### Authentication Flow

1. User clicks Google sign-in; frontend receives a Google ID token
2. `AuthService.loginUserWithGoogle()` POSTs the token to `/api/auth/google-login`
3. Backend validates via Google, creates/fetches user, generates a JWT via `JwtService`
4. JWT is set as an HttpOnly cookie (`AuthToken`) in the response — nothing stored in localStorage
5. `AuthService` emits `true` via its `BehaviorSubject` auth state
6. Components subscribe to `getAuthStatusListener()` for reactive updates

### HTTP Requests

- Always include `withCredentials: true` for cookie-based auth
- Use `firstValueFrom()` to convert Observables to Promises
- Handle errors with try-catch blocks

```typescript
async login(token: string): Promise<void> {
  const response = await firstValueFrom(
    this.httpClient.post<GoogleLoginDto>(
      `${this.apiUrl}/google-login`,
      { idToken: token },
      { withCredentials: true }
    )
  );
}
```

### Reactive Forms with Type Safety

Complex forms use typed FormGroups for compile-time safety:

```typescript
// Define form structure
export interface LessonFormControls {
  title: FormControl<string>;
  description: FormControl<string>;
  exercises: FormArray<FormGroup<ExerciseFormControls>>;
}

// Create typed FormGroup
type LessonForm = FormGroup<LessonFormControls>;
```

**Form Factory Pattern** (see `lesson-form.service.ts`):
- Factory methods create typed forms
- Separate factory methods per form type
- NonNullableFormBuilder ensures non-null values

### Subscription Management

Always clean up subscriptions using `takeUntilDestroyed()`:

```typescript
private destroyRef = inject(DestroyRef);

ngOnInit() {
  this.authService.getAuthStatusListener()
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(isAuthenticated => {
      // Handle auth status
    });
}
```

### Service Initialization Pattern

Services don't have lifecycle hooks — use APP_INITIALIZER for startup logic:

```typescript
// In app.config.ts
function initializeAuth(authService: AuthService) {
  return () => authService.initializeAuthState();
}

export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService],
      multi: true
    }
  ]
};
```

### Import Paths

- Auth service from features: `import { AuthService } from '../../../../auth/auth.service';`
- Services are at app root, features are nested deeper — count `../` levels carefully

### Form Restoration from Backend

When restoring component state from previous submissions:
- Backend may return data for ALL items (not just completed ones)
- Distinguish "never attempted" from "attempted incorrectly" by checking discriminator fields
- Example: `wasAttempted = response.isCorrect || response.correctAnswer !== null`
- Don't skip restoration based solely on success/failure flags

### Editor.js Integration

**Upload Gotchas**:
- When `uploader.uploadByFile` is provided, Editor.js ignores `field` and `endpoints` config entirely
- The custom uploader's FormData field name must match the backend `IFormFile` parameter name (`"file"`)
- File type is communicated via URL route (`/uploads/image`), NOT the FormData field name

**Performance Optimization**:
- Editor onChange fires on ALL interactions (focus, mouse moves, selection changes)
- Debounce with 300ms timeout + content comparison to prevent excessive saves
- Track `lastSavedContent` string and only call `onChange()` if different
- Clear timeout on `ngOnDestroy()` to prevent memory leaks

The rich text editor implements `ControlValueAccessor` for seamless Reactive Forms integration:

```typescript
@Component({
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => EditorComponent),
    multi: true
  }]
})
export class EditorComponent implements ControlValueAccessor {
  writeValue(value: string): void { }
  registerOnChange(fn: any): void { }
  registerOnTouched(fn: any): void { }
}
```

**Styling**:
- Editor component uses external `styleUrl` (not inline `styles`)
- Dark theme (`::ng-deep` overrides with `!important`) lives in `editor.component.scss`
- Container styles (glass background, border, focus state) also in `editor.component.scss`
- Consuming components (e.g. lesson-editor) should NOT add Editor.js `::ng-deep` overrides

Content is stored as JSON in Editor.js format.

### Component Organization

```
feature/
├── feature.component.ts      # Main component (standalone)
├── feature.component.html    # Template
├── feature.component.scss    # Styles (SCSS)
├── feature.service.ts        # Backend communication
├── feature.interface.ts      # TypeScript interfaces
└── feature-form.service.ts   # Form factory (if complex forms)
```

### Form → API Mapping Pattern

**Form controls use different property names than backend DTOs:**
- Form: `exerciseType` → Backend: `type` (JSON polymorphic discriminator)
- Form: `question` (FillInBlank) → Backend: `text`
- Form: no `orderIndex` → Backend: auto-calculated from array index

**Architecture:** Typed discriminated unions in `exercise.interface.ts`:
- `ExerciseFormValue` — union of 4 form value types keyed on `exerciseType`
- `CreateExerciseDto` — union of 4 backend DTO types keyed on `type`
- `CreateExerciseBase` — shared base fields (lessonId, title, points, etc.)
- Mapping done in `LessonService.mapFormToCreateDto()` with exhaustive switch
- `buildLessonPayload()` in lesson-editor just calls `getRawValue()` on each form

## Adding a New Component/Feature

1. Create component folder with files:
   - `component-name.component.ts` (standalone: true)
   - `component-name.component.html`
   - `component-name.component.scss`
   - `component-name.service.ts` (if backend communication needed)
   - `component-name.interface.ts` (for type definitions)
2. Add route to `app.routes.ts`
   - Use lazy loading for dynamic routes: `loadComponent: () => import(...)`
3. Update navigation in `nav-bar` component if needed
4. Follow design system guidelines (see Design System section below)
5. For services:
   - Use `providedIn: 'root'` for singleton services
   - Inject dependencies via `inject()` function
   - Use HttpClient with `withCredentials: true` for authenticated requests
6. For forms:
   - Use Reactive Forms with typed FormGroups
   - Create factory services for complex forms (see `lesson-form.service.ts`)
   - Implement ControlValueAccessor for custom form controls

## Routes

| Path | Component | Lazy Loaded | Description |
|------|-----------|-------------|-------------|
| `/` | HomeComponent | No | Main dashboard with learning path |
| `/google-login` | GoogleLoginComponent | No | OAuth login page |
| `/create-lesson` | LessonEditorComponent | Yes | Lesson creation form with Editor.js |
| `/lesson/:id` | LessonViewerComponent | Yes | Display lesson content |
| `/profile` | ProfileComponent | No | User profile and achievements |
| `/leaderboard` | LeaderboardComponent | No | User rankings |
| `/help` | HelpComponent | No | Help and FAQ |
| `/**` | NotFoundComponent | Yes | 404 page |

## Design System

**CRITICAL: All new components and pages MUST follow these design guidelines for visual consistency.**

### CSS Conventions

- **No `!important`** — increase specificity instead (exception: Editor.js overrides via `::ng-deep`)
- **Use `rem` units** for new styles, not `px` — base is 16px
- **Reuse CSS variables** from `src/styles.scss` (colors, radii, shadows, glass effects)
- **Reuse mixins** from `styles.scss` via `@use`: `@use '../path/styles.scss' as styles;` then `@include styles.animated-background`
- **Never use `@import`** for Sass — always use `@use` with a namespace (Dart Sass 3.0 requirement)
- **Component styles**: Always use SCSS, use `@use` for `styles.scss` only when mixins are needed
- **Shared button mixin**: `@use 'path/to/shared/buttons' as buttons;` then `@include buttons.system;` — provides `.btn` with variants (primary, secondary, small, icon-only, link-btn, success, large, no-exercises-btn)
- **Shared card mixin**: `@use 'path/to/shared/cards' as cards;` then `@include cards.system;` — provides `.card` glass morphism pattern with inner glow border and responsive breakpoint
- **Editor.js dark theme**: Lives in `shared/components/editor/editor.component.scss` with `::ng-deep` — consuming components should NOT duplicate these overrides
- **Fixing `!important`**: Nest overrides inside parent class for equal specificity + source-order win; `:has()` pseudo-class provides high specificity naturally

### Color Palette (CSS Custom Properties)

Use CSS variables defined in `src/styles.scss`:

```scss
// Backgrounds
--bg-dark: #0f1419;      // Darkest background
--bg: #1a2429;           // Main background
--panel: #1e2732;        // Panel/card backgrounds

// Accent Colors
--accent: #7c5cff;       // Primary purple accent
--accent-light: #9178ff; // Lighter purple (hover states, links)
--accent-dark: #5a3ce6;  // Darker purple (gradients, shadows)

// Admin/Privilege Colors
--admin-gold: #ffc107;  // Golden accent for admin badges/overrides
--admin-gold-dark: #ffa000; // Darker gold for borders/shadows

// Text Colors
--white: #ffffff;        // Primary text
--text-secondary: #b8c4cf; // Secondary text
--muted: #8b98a5;        // Muted/tertiary text

// Glass Effects
--glass: rgba(255,255,255,0.04);   // Subtle glass overlay
--glass-hover: rgba(255,255,255,0.08); // Glass hover state
--border: rgba(255,255,255,0.08);  // Subtle borders
```

### Typography

- **Font Family**: `"Bricolage Grotesque", sans-serif` (loaded from Google Fonts)
- **Primary Text**: `var(--white)` with font weights 600-800
- **Secondary Text**: `var(--text-secondary)` with font weight 500
- **Muted Text**: `var(--muted)` for disclaimers, terms, etc.

**Heading Styles:**
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

.subtitle {
  color: var(--text-secondary);
  font-size: 18px;
  font-weight: 500;
}
```

### Border Radius

- **Cards/Panels**: `var(--radius)` = `16px`
- **Buttons/Pills**: `var(--radius-sm)` = `100px` (fully rounded)

### Shadows

```scss
--shadow: 0 20px 60px rgba(0,0,0,0.5);  // Default shadow for cards
--shadow-hover: 0 24px 70px rgba(124, 92, 255, 0.15);  // Purple glow on hover
```

### Glass Morphism Pattern

**All cards and panels should use glassmorphic design:**

```scss
.card {
  background: linear-gradient(135deg, rgba(255,255,255,0.06) 0%, rgba(255,255,255,0.02) 100%);
  border-radius: var(--radius);
  padding: 38px 40px;
  box-shadow: var(--shadow);
  border: 1px solid var(--border);
  backdrop-filter: blur(10px);
  position: relative;

  // Inner glow border effect
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

### Button Styles

**Primary Button (Call-to-Action):**
```scss
.btn.primary {
  background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dark) 100%);
  box-shadow: 0 8px 24px rgba(124, 92, 255, 0.25), 0 16px 48px rgba(90, 60, 230, 0.15);
  border-radius: var(--radius-sm);
  padding: 14px 18px;
  height: 50px;
  font-weight: 700;
  font-size: 15px;
  color: var(--white);
  border: 1px solid rgba(255,255,255,0.1);
  cursor: pointer;
  transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 32px rgba(124, 92, 255, 0.35), 0 20px 60px rgba(90, 60, 230, 0.25);
  }

  &:active {
    transform: translateY(0);
  }
}
```

**OAuth/Secondary Buttons:**
```scss
.btn.oauth {
  background: var(--glass);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 14px 18px;
  height: 50px;
  font-weight: 600;
  font-size: 15px;
  color: var(--white);
  cursor: pointer;
  transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);

  &:hover {
    background: var(--glass-hover);
    border-color: rgba(255,255,255,0.12);
    transform: translateY(-2px);
    box-shadow: var(--shadow-hover);
  }
}
```

### Hover Effects

**Standard hover interaction:**
```scss
transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);

&:hover {
  transform: translateY(-2px);  // Subtle lift
  box-shadow: var(--shadow-hover);  // Purple glow
}

&:active {
  transform: translateY(0);  // Press down effect
}
```

### Links

```scss
.link {
  color: var(--accent-light);
  text-decoration: none;
  transition: color 150ms ease;
  border-bottom: 1px solid transparent;

  &:hover {
    color: var(--accent-light);
    border-bottom-color: var(--accent-light);
  }
}
```

### Sidebar/Navigation Pattern

**Example from nav-bar component:**
```scss
#mainAside {
  border-right: 2px solid #3b4951;
  padding: 1.5em 2.2em 1em 1.3em;
  height: 100vh;
  width: 18em;

  .sideBarLinks {
    list-style: none;

    li {
      a {
        text-transform: uppercase;
        padding: .8em 1em;
        color: var(--primary-color);
        font-weight: 700;
        letter-spacing: .08px;
        text-decoration: none;
      }

      &:hover {
        background-color: #3b4951;
        border-radius: 5%;
      }
    }
  }
}
```

### Responsive Breakpoints

```scss
// Tablet
@media (max-width: 1024px) {
  // Switch to single column, adjust padding
}

// Mobile
@media (max-width: 480px) {
  // Reduce font sizes, tighter padding
}
```

### Animations

**Background pulse effect:**
```scss
&::before {
  content: '';
  position: absolute;
  background: radial-gradient(circle, rgba(124, 92, 255, 0.1) 0%, transparent 40%);
  animation: pulse 8s ease-in-out infinite;
}
```

### Accessibility

- All interactive elements must have `aria-label` attributes
- Use semantic HTML (`<aside>`, `<nav>`, `<main>`, etc.)
- Focus states: `outline: 2px solid var(--accent); outline-offset: 3px;`
- Use `role` attributes where appropriate

### Layout Patterns

**Split-screen auth pattern (google-login component):**
```scss
.page {
  display: grid;
  grid-template-columns: 1fr 520px;  // Logo side | Form side
  min-height: 100vh;
  background: var(--bg);
}
```

### Component Styling Checklist

When creating new components, ensure:
- [ ] Uses CSS custom properties from `:root`
- [ ] Follows glassmorphism pattern for cards/panels
- [ ] Buttons use defined `.btn.primary` or `.btn.oauth` styles
- [ ] Hover effects include `transform: translateY(-2px)` and purple shadow
- [ ] Border radius uses `var(--radius)` or `var(--radius-sm)`
- [ ] Typography uses `var(--font-family)` and correct font weights
- [ ] Links use accent-light color with underline on hover
- [ ] Responsive breakpoints at 1024px and 480px
- [ ] Accessibility attributes present (aria-label, role, etc.)
- [ ] Focus states defined with purple accent outline

## Known Limitations

- Help and Leaderboard services return mock data (not yet integrated with backend)
- **Frontend/Backend property name mismatches cause silent failures** in Angular templates
  - `@switch` statements won't match if property name is wrong
  - Template expressions return undefined without error
  - Always verify API response matches TypeScript interface property names
- `Exercise` interface in `exercise.interface.ts` must include `isLocked: boolean` — backend `ExerciseDto` returns it, exercise-viewer depends on it

## Common Debugging Scenarios

### 400 Bad Request on POST/PUT

If you get 400 errors with no details:
1. **Check ModelState is enabled**: Ensure backend `SuppressModelStateInvalidFilter` is NOT set to true
2. **Check Network tab**: Response body shows which field failed validation
3. **Common causes**:
   - Enum sent as string but backend expects int (add JsonStringEnumConverter to backend enum)
   - Type discriminator missing or not first property (JSON polymorphism)
   - Required field is null or empty

### Cookie Not Being Sent

If cookies aren't being sent from frontend to backend:

1. **Verify proxy configuration**: Frontend nginx should proxy `/api` to backend
2. **Check CORS**: Must have `AllowCredentials()` with specific origin (not wildcard)
3. **Frontend requests**: Must include `withCredentials: true` in HTTP requests
4. **Cookie settings**: `SameSite=Lax` works with proxy (same-origin), otherwise needs `SameSite=None` + `Secure=true`

### Frontend/Backend Interface Mismatch

If data loads in API but not in UI:

1. **Check API response in Network tab** — Copy full JSON response
2. **Compare property names** — Backend may use different names than frontend interfaces
3. **Common mismatches**:
   - Backend: `type: "MultipleChoice"` → Frontend expects: `exerciseType`
   - Backend: `text: "..."` (FillInBlank) → Frontend expects: `question`
   - Backend: `difficultyLevel: 0` (number) → Frontend expects: `DifficultyLevel` enum
4. **Fix**: Update frontend interfaces to match API response structure
5. **Symptom**: `@switch` statements won't match, template expressions return undefined without errors
