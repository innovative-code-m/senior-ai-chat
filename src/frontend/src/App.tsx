import { useState } from 'react';

type HealthState =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'success'; message: string }
  | { kind: 'error'; message: string };

type HealthResponse = {
  status: string;
  service: string;
  phase: string;
  signalRHub: string;
};

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ??
  'http://localhost:5086';

export default function App() {
  const [healthState, setHealthState] = useState<HealthState>({ kind: 'idle' });

  const checkBackend = async () => {
    setHealthState({ kind: 'loading' });

    try {
      const response = await fetch(`${apiBaseUrl}/health`, {
        headers: { Accept: 'application/json' }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const health = (await response.json()) as HealthResponse;
      setHealthState({
        kind: 'success',
        message: `${health.service} ${health.phase}: ${health.status}`
      });
    } catch (error) {
      const detail = error instanceof Error ? error.message : 'unknown error';
      setHealthState({
        kind: 'error',
        message: `バックエンドに接続できませんでした。${detail}`
      });
    }
  };

  return (
    <main className="app-shell">
      <section className="intro-panel" aria-labelledby="page-title">
        <p className="phase-label">Phase 1 最小雛形</p>
        <h1 id="page-title">senior-ai-chat</h1>
        <p className="lead">
          信頼できる小さなグループで、生成AIの使い方を学び合うための
          closed チャット試作です。
        </p>
      </section>

      <section className="status-grid" aria-label="起動確認">
        <article className="status-card">
          <div>
            <p className="card-label">フロントエンド</p>
            <h2>React + TypeScript</h2>
          </div>
          <p>
            この画面は Phase 1 の起動確認用です。登録、ログイン、チャット投稿は
            後続 Phase で扱います。
          </p>
          <span className="status-pill ready">表示中</span>
        </article>

        <article className="status-card">
          <div>
            <p className="card-label">バックエンド</p>
            <h2>ASP.NET Core Web API</h2>
          </div>
          <p>
            `/health` の応答を確認します。認証、データベース、業務機能はまだ
            実装していません。
          </p>
          <button type="button" onClick={checkBackend}>
            API を確認
          </button>
          <p className={`health-message ${healthState.kind}`} role="status">
            {healthState.kind === 'idle' && `確認先: ${apiBaseUrl}/health`}
            {healthState.kind === 'loading' && '確認しています'}
            {healthState.kind === 'success' && healthState.message}
            {healthState.kind === 'error' && healthState.message}
          </p>
        </article>
      </section>
    </main>
  );
}
