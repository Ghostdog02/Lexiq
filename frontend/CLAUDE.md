# Frontend CLAUDE.md

Angular 21 SPA, standalone components.

> Cross-cutting bug patterns: [`.claude/rules/common-gotchas.md`](../.claude/rules/common-gotchas.md).
> Debugging playbooks: [`.claude/rules/troubleshooting.md`](../.claude/rules/troubleshooting.md).
> Visual catalog (colors, mixins, button styles, checklist): [`/docs/frontend/design-system.md`](../docs/frontend/design-system.md). Open it when you're building UI.

## Commands (from `frontend/`)

```bash
npm install
npm start                 # http://localhost:4200
npm run build
npm run watch
npm test
ng generate component <name>
ng generate service   <name>
```

## Layout

```
frontend/src/app/
├── auth/                # AuthService (BehaviorSubject), Google login
├── features/
│   ├── lessons/         # home, lesson-editor (lazy), lesson-viewer (lazy)
│   └── users/           # leaderboard, profile
├── help/
├── nav-bar/
├── not-found/           # lazy
├── shared/
│   ├── _buttons.scss        # buttons.system
│   ├── _cards.scss          # cards.system
│   ├── _mixins.scss         # glass-card
│   ├── _state-feedback.scss # correct/incorrect/warning
│   ├── components/editor/   # EditorJS ControlValueAccessor
│   └── services/
├── app.routes.ts
└── app.config.ts
```

## Conventions (must-follow)

- **Standalone components only** — no `NgModule`.
- **`inject()`** — never constructor injection.
- **Lazy load** dynamic routes (`lesson/:id`, `create-lesson`, `**`).
- **Subscriptions** — `takeUntilDestroyed(this.destroyRef)`.
- **State** — service-based with RxJS Observables. `AuthService` uses `BehaviorSubject`. `LessonService` uses `Subject` for events. No NgRx.
- **HTTP** — always `withCredentials: true`. Convert to promise with `firstValueFrom()`.
- **Forms** — Reactive Forms with typed `FormGroup<T>`. Factory services for complex forms (e.g. `lesson-form.service.ts`). `NonNullableFormBuilder`.
- **Functional `CanActivateFn`** — class-based `CanActivate` is deprecated. `inject()` works inside functional guards. `returnUrl`: pass `state.url`, validate with `startsWith('/')` to block open redirects. Stack guards: `canActivate: [authGuard, contentGuard]`.
- **Roles** — `isAdmin` is strictly Admin role. `isContentCreator` is separate. Both come from the `roles: string[]` array in `/api/auth/is-admin` (the boolean `isAdmin` field in the DTO is ignored).
- **Env vars** via `@ngx-env/builder` — prefix `NG_` or `BACKEND_`. `BACKEND_API_URL=/api` (relative — nginx proxies).
- **ngx-toastr v20** — `provideAnimationsAsync()` + `provideToastr()` in `app.config.ts`. Theme in `shared/_toastr.scss`, scoped to `.toast-auth`.

### Performance anti-patterns

- ❌ Methods in template bindings — re-runs on every CD cycle. Pre-compute in `ngOnInit`. `[innerHTML]="parsedContent"` not `[innerHTML]="parseContent(lesson.content)"`.
- ❌ `transition: all` — enumerate properties.
- ✅ `Promise.all` for independent page-load fetches.
- ✅ Pass parent-fetched data as `@Input` instead of refetching in child `ngOnInit`.

### Service init

Services have no lifecycle hooks. Use `APP_INITIALIZER`:

```typescript
function initializeAuth(authService: AuthService) {
  return () => authService.initializeAuthState();
}

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: APP_INITIALIZER, useFactory: initializeAuth, deps: [AuthService], multi: true }
  ]
};
```

`APP_INITIALIZER` resolves auth state before routing → guards can be **synchronous** and `authService.getIsAuth()` is reliable.

## Auth flow

1. Google sign-in → frontend gets ID token.
2. `AuthService.loginUserWithGoogle()` POSTs to `/api/auth/google-login`.
3. Backend validates → JWT in HttpOnly `AuthToken` cookie. **Nothing in localStorage.**
4. `AuthService` emits `true` via `BehaviorSubject`.
5. Components subscribe to `getAuthStatusListener()` for reactive updates.

## Form ↔ API mapping

Form names diverge from backend DTOs:

- `exerciseType` (form) ↔ `type` (DTO discriminator).
- `question` (FillInBlank form) ↔ `text` (DTO).
- No `orderIndex` in form; backend auto-calculates from array index.

Architecture in `exercise.interface.ts`:

- `ExerciseFormValue` — discriminated union keyed on `exerciseType`.
- `CreateExerciseDto` — discriminated union keyed on `type`.
- `CreateExerciseBase` — shared base.
- Mapping in `LessonService.mapFormToCreateDto()` (exhaustive switch).
- `buildLessonPayload()` in `lesson-editor` calls `getRawValue()` per form.

When restoring from backend submissions: backend returns a response for **every** exercise; distinguish "never attempted" from "attempted incorrectly" via `correctAnswer !== null` (see [`troubleshooting.md`](../.claude/rules/troubleshooting.md)).

## Editor.js

- Implements `ControlValueAccessor` for Reactive Forms.
- `uploadByFile` returns `blob:` immediately; actual upload runs later in `uploadPendingFiles(contentJson)` from `LessonEditorComponent.onSubmit()`.
- Custom uploader's FormData field name MUST equal backend `IFormFile` parameter name (`"file"`).
- File type travels via URL route (`/uploads/image`), NOT FormData field name.
- Editor `onChange` fires on every interaction. Debounce 300ms + diff `JSON.stringify(content.blocks)` (NOT full `save()` — `time` always changes). Clear timeout in `ngOnDestroy()`. Revoke pending blob URLs in `ngOnDestroy()`.
- Dark theme + container styles live in `editor.component.scss` (`::ng-deep` overrides with `!important`). Consuming components MUST NOT add their own Editor.js overrides.

## Routes

| Path | Component | Lazy |
|------|-----------|:----:|
| `/` | HomeComponent | — |
| `/google-login` | GoogleLoginComponent | — |
| `/create-lesson` | LessonEditorComponent | ✓ |
| `/lesson/:id` | LessonViewerComponent | ✓ |
| `/profile` | ProfileComponent | — |
| `/leaderboard` | LeaderboardComponent | — |
| `/help` | HelpComponent | — |
| `/**` | NotFoundComponent | ✓ |

## Component layout convention

```
feature/
├── feature.component.ts        # standalone: true
├── feature.component.html
├── feature.component.scss
├── feature.service.ts          # if backend
├── feature.interface.ts
└── feature-form.service.ts     # if complex forms
```

## Design system

Quick rules — full catalog in [`/docs/frontend/design-system.md`](../docs/frontend/design-system.md):

- **No hardcoded hex/rgba** — use CSS vars (`var(--accent)`; `rgba(var(--accent-rgb), 0.4)`).
- **`rem` units only.** Base is 16px.
- **No `!important`** — increase specificity (Editor.js `::ng-deep` is the documented exception).
- **`@use` only**, never `@import`. `@use 'shared/styles' as styles;` then `@include styles.animated-background`.
- **`transition: all` is banned.**
- **No `transition` inside visual mixins** — caller owns animation.
- **Reuse mixins**: `buttons.system`, `cards.system`, `mixins.glass-card`, `state-feedback.*`.
- **A linter auto-formats `.scss` on write** — re-`Read` before any follow-up `Edit` or you'll hit "file modified since read".

## Known limitations

- Help service returns mock data (not yet backend-integrated).
- Frontend/backend property-name mismatches fail **silently** — `@switch` doesn't match, expressions return `undefined` with no error. Always verify the API response matches your interface.
- `Exercise` interface MUST include `isLocked: boolean` — `exercise-viewer` depends on it.
- `submitExerciseAnswer` returns `SubmitAnswerResponse` (includes `lessonProgress`), **not** `ExerciseSubmitResult`. Type the call and the `submissionResults` map accordingly to avoid TS2322 on `currentSubmission`.
