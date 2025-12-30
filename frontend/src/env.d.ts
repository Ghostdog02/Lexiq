declare interface Env {
  readonly NODE_ENV: 'development' | 'production';
  readonly BACKEND_URL: string;
  readonly NG_GOOGLE_CLIENT_ID: string;
}

declare interface ImportMeta {
  readonly env: Env;
}