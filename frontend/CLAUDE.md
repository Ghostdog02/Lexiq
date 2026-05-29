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
│   ├── lessons/         # home, lesson-editor (lazy), lesson-viewer (lazy), exercise-viewer
│   │   └── services/    # ExerciseViewerStateService — single source of truth for lesson session state
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

Exercise types supported in the lesson-editor form: `FillInBlank`, `Listening`, `TrueFalse`. (`ImageChoice` and `AudioMatching` are backend entity types but not yet in the lesson-editor form.)

Architecture in `exercise.interface.ts`:

- `ExerciseFormValue` — discriminated union keyed on `exerciseType`.
- `CreateExerciseDto` — discriminated union keyed on `type`.
- `CreateExerciseBase` — shared base.
- Mapping in `LessonService.mapFormToCreateDto()` (exhaustive switch).
- `buildLessonPayload()` in `lesson-editor` calls `getRawValue()` per form.

When restoring from backend submissions: backend returns a response for **every** exercise; distinguish "never attempted" from "attempted incorrectly" via `correctAnswer !== null` (see [`troubleshooting.md`](../.claude/rules/troubleshooting.md)).

## Hearts system (frontend)

- `ExerciseViewerStateService` holds the current heart count, fetched from `GET /api/user/hearts` during exercise-viewer init.
- Hearts are decremented in the state service on wrong answers (only when `hearts > 0`).
- The header badge displays the live count: `❤️ × {{ state.hearts }}`.
- `LessonSubmitResult` from `POST /api/lessons/{id}/submit` includes `heartsRemaining` — the UI reads this to show the final heart count on the results screen.
- When `hearts === 0`, the "Check" button is disabled and submission is blocked client-side (backend also enforces this).

## Audio upload flow (lesson-editor)

`lesson-editor.component.ts` manages audio uploads for `ListeningExercise` fields:

1. Hidden `<input type="file" accept=".mp3,.wav,.ogg,.m4a,.flac">` triggers on button click.
2. `onAudioFileSelected()` validates the file (max 10 MB client-side), creates a blob URL for preview, and stores the `File` in `pendingAudioFiles: Map<controlName, File>`.
3. The audio preview uses a `<audio controls>` element bound to the blob URL.
4. On form submit: `uploadPendingAudioFiles()` POSTs each pending file to `POST /api/uploads/audio` with `FormData` (field name `"file"` — must match the backend `IFormFile` parameter name).
5. The blob URL in the form control is replaced with the real server URL from the upload response before the lesson payload is sent.
6. Alternatively, users can paste a URL directly into the audio URL field (skips upload).

## Exercise viewer state service

`ExerciseViewerStateService` (`features/lessons/services/exercise-viewer-state.service.ts`) is the single source of truth for a lesson session:

- Tracks: exercises array, current index, and a `ViewModel` per exercise (`selectedOption`, `isCorrect`, `isSubmitted`, `isAccessible`).
- **Local validation**: `option.isCorrect` is used immediately on "Check" — no per-exercise backend call. Answers are validated client-side using the `isCorrect` flag from the exercise DTO.
- **Batch submit**: all answers are sent together via `POST /api/lessons/{id}/submit` when the user finishes the last exercise.
- **Hearts**: wrong answers call `decrementHearts()` on the state service, which updates the local count and the `User.Hearts` on the backend.

## Lesson completion logic

After `POST /api/lessons/{id}/submit`:

- `LessonSubmitResult.meetsCompletionThreshold` (boolean) drives the results UI: `true` → "Lesson Complete!" with a green checkmark; `false` → "Keep Practicing" with a warning icon.
- Threshold is decided **server-side** based on hearts: the lesson is complete if the user has `hearts > 0` at submission time.
- `LessonSubmitResult.nextLesson` carries the next lesson's ID and unlock state.
- Both outcomes display earned XP, correct/total exercise count, and hearts remaining.

## CSS animation pattern (exercise transitions)

Exercise transitions use a flag-based animation pattern rather than the browser View Transition API:

- `isExiting = true` triggers the exit keyframe animation (`exerciseFadeOut`) via CSS class binding.
- After `400ms`, the router navigates or the next exercise is loaded.
- Pattern: `isExiting = true` → `setTimeout(400ms)` → navigate / advance index.
- Keyframes (`exerciseFadeIn`, `exerciseFadeOut`) live in `exercise-viewer.component.scss`.

> **TODO**: Browser-native View Transition API (`document.startViewTransition`) is implemented in a separate worktree and not yet merged. Document the pattern here when it lands.

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
- `submitExerciseAnswer` returns `SubmitAnswerResponse` (includes `lessonProgress`), **not** `ExerciseSubmitResult`. Type the call and the `submissionResults` map accordingly to avoid TS2322 on `currentSubmission`.
- `ImageChoice` and `AudioMatching` exercise types exist on the backend but the lesson-editor form does not yet support creating them.
- View Transition API is implemented in a separate worktree and not yet merged into this branch.
