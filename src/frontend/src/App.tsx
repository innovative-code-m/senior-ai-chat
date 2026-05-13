import { FormEvent, useState } from 'react';

type ViewMode = 'register' | 'status' | 'admin';

type RegistrationForm = {
  fullName: string;
  email: string;
  graduationClassName: string;
};

type RequestState =
  | { kind: 'idle' }
  | { kind: 'loading'; message: string }
  | { kind: 'success'; message: string }
  | { kind: 'error'; message: string };

type RegistrationResponse = {
  id: string;
  status: string;
  submittedAt: string;
  message: string;
};

type RegistrationStatusResponse = {
  status: string;
  message: string;
};

type PendingUser = {
  id: string;
  fullName: string;
  email: string;
  graduationClassName: string;
  status: string;
  createdAt: string;
};

type AdminUserActionResponse = {
  id: string;
  status: string;
  updatedAt: string;
  message: string;
};

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ??
  'http://localhost:5086';

const isDevelopment = import.meta.env.DEV;

const initialRegistrationForm: RegistrationForm = {
  fullName: '',
  email: '',
  graduationClassName: ''
};

export default function App() {
  const [viewMode, setViewMode] = useState<ViewMode>('register');
  const [registrationForm, setRegistrationForm] = useState<RegistrationForm>(
    initialRegistrationForm
  );
  const [registrationState, setRegistrationState] = useState<RequestState>({
    kind: 'idle'
  });
  const [statusEmail, setStatusEmail] = useState('');
  const [statusState, setStatusState] = useState<RequestState>({ kind: 'idle' });
  const [pendingUsers, setPendingUsers] = useState<PendingUser[]>([]);
  const [adminState, setAdminState] = useState<RequestState>({ kind: 'idle' });

  const updateRegistrationField = (
    field: keyof RegistrationForm,
    value: string
  ) => {
    setRegistrationForm((current) => ({
      ...current,
      [field]: value
    }));
  };

  const submitRegistration = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setRegistrationState({
      kind: 'loading',
      message: '仮登録を送信しています。'
    });

    try {
      const response = await fetch(`${apiBaseUrl}/api/registrations`, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(registrationForm)
      });

      if (!response.ok) {
        throw new Error(await readProblemMessage(response));
      }

      const result = (await response.json()) as RegistrationResponse;
      setRegistrationState({
        kind: 'success',
        message: `${result.message} 現在の状態: ${toStatusLabel(result.status)}`
      });
      setStatusEmail(registrationForm.email);
    } catch (error) {
      setRegistrationState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const checkStatus = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatusState({
      kind: 'loading',
      message: '状態を確認しています。'
    });

    try {
      const query = new URLSearchParams({ email: statusEmail });
      const response = await fetch(
        `${apiBaseUrl}/api/registrations/status?${query.toString()}`,
        {
          headers: { Accept: 'application/json' }
        }
      );

      if (!response.ok) {
        throw new Error(await readProblemMessage(response));
      }

      const result = (await response.json()) as RegistrationStatusResponse;
      setStatusState({
        kind: 'success',
        message: `${toStatusLabel(result.status)}: ${result.message}`
      });
    } catch (error) {
      setStatusState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const loadPendingUsers = async (successMessage?: string) => {
    setAdminState({
      kind: 'loading',
      message: '承認待ち一覧を読み込んでいます。'
    });

    try {
      const response = await fetch(`${apiBaseUrl}/api/admin/users/pending`, {
        headers: { Accept: 'application/json' }
      });

      if (!response.ok) {
        throw new Error(await readProblemMessage(response));
      }

      const result = (await response.json()) as PendingUser[];
      setPendingUsers(result);
      setAdminState({
        kind: 'success',
        message:
          successMessage ??
          (result.length === 0
            ? '承認待ちの申請はありません。'
            : `${result.length}件の承認待ち申請があります。`)
      });
    } catch (error) {
      setPendingUsers([]);
      setAdminState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const runAdminAction = async (
    userId: string,
    action: 'approve' | 'reject'
  ) => {
    const actionLabel = action === 'approve' ? '承認' : '否認';
    setAdminState({
      kind: 'loading',
      message: `${actionLabel}しています。`
    });

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/admin/users/${userId}/${action}`,
        {
          method: 'POST',
          headers: { Accept: 'application/json' }
        }
      );

      if (!response.ok) {
        throw new Error(await readProblemMessage(response));
      }

      const result = (await response.json()) as AdminUserActionResponse;
      await loadPendingUsers(
        `${result.message} 現在の状態: ${toStatusLabel(result.status)}`
      );
    } catch (error) {
      setAdminState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  return (
    <main className="app-shell">
      <header className="app-header">
        <p className="phase-label">Phase 2 ローカル検証</p>
        <h1>senior-ai-chat</h1>
        <p className="lead">
          参加希望者の仮登録と、管理者による承認・否認の流れを確認します。
        </p>
      </header>

      <nav className="mode-tabs" aria-label="作業の切り替え">
        <button
          type="button"
          className={viewMode === 'register' ? 'active' : ''}
          onClick={() => setViewMode('register')}
        >
          仮登録
        </button>
        <button
          type="button"
          className={viewMode === 'status' ? 'active' : ''}
          onClick={() => setViewMode('status')}
        >
          状態確認
        </button>
        {isDevelopment && (
          <button
            type="button"
            className={viewMode === 'admin' ? 'active' : ''}
            onClick={() => {
              setViewMode('admin');
              void loadPendingUsers();
            }}
          >
            開発用管理
          </button>
        )}
      </nav>

      {viewMode === 'register' && (
        <section className="work-panel" aria-labelledby="register-title">
          <div className="panel-heading">
            <p className="section-label">参加申請</p>
            <h2 id="register-title">仮登録</h2>
          </div>
          <form className="form-grid" onSubmit={submitRegistration}>
            <label>
              <span>氏名</span>
              <input
                value={registrationForm.fullName}
                onChange={(event) =>
                  updateRegistrationField('fullName', event.target.value)
                }
                autoComplete="name"
              />
            </label>
            <label>
              <span>メールアドレス</span>
              <input
                value={registrationForm.email}
                onChange={(event) =>
                  updateRegistrationField('email', event.target.value)
                }
                type="email"
                autoComplete="email"
              />
            </label>
            <label>
              <span>卒業時のクラス名</span>
              <input
                value={registrationForm.graduationClassName}
                onChange={(event) =>
                  updateRegistrationField(
                    'graduationClassName',
                    event.target.value
                  )
                }
              />
            </label>
            <button type="submit">仮登録を送信</button>
          </form>
          <p className="support-text">
            送信後は管理者の確認待ちになります。承認後の自動メール送信は
            Phase 2 では行いません。
          </p>
          <StateMessage state={registrationState} />
        </section>
      )}

      {viewMode === 'status' && (
        <section className="work-panel" aria-labelledby="status-title">
          <div className="panel-heading">
            <p className="section-label">申請状況</p>
            <h2 id="status-title">状態確認</h2>
          </div>
          <form className="form-grid compact" onSubmit={checkStatus}>
            <label>
              <span>メールアドレス</span>
              <input
                value={statusEmail}
                onChange={(event) => setStatusEmail(event.target.value)}
                type="email"
                autoComplete="email"
              />
            </label>
            <button type="submit">状態を確認</button>
          </form>
          <p className="support-text">
            この画面では、入力したメールアドレスの申請状態だけを表示します。
          </p>
          <StateMessage state={statusState} />
        </section>
      )}

      {viewMode === 'admin' && isDevelopment && (
        <section className="work-panel" aria-labelledby="admin-title">
          <div className="panel-heading admin-heading">
            <div>
              <p className="section-label">Development 限定</p>
              <h2 id="admin-title">管理者承認</h2>
            </div>
            <button
              type="button"
              className="secondary"
              onClick={() => void loadPendingUsers()}
            >
              再読み込み
            </button>
          </div>
          <p className="warning-text">
            この管理画面は Phase 2 のローカル検証用です。本番運用には使いません。
          </p>
          <StateMessage state={adminState} />

          <div className="pending-list" aria-label="承認待ち一覧">
            {pendingUsers.map((user) => (
              <article className="pending-item" key={user.id}>
                <div>
                  <p className="person-name">{user.fullName}</p>
                  <dl>
                    <div>
                      <dt>メール</dt>
                      <dd>{user.email}</dd>
                    </div>
                    <div>
                      <dt>卒業時のクラス名</dt>
                      <dd>{user.graduationClassName}</dd>
                    </div>
                    <div>
                      <dt>登録日時</dt>
                      <dd>{formatDateTime(user.createdAt)}</dd>
                    </div>
                    <div>
                      <dt>状態</dt>
                      <dd>{toStatusLabel(user.status)}</dd>
                    </div>
                  </dl>
                </div>
                <div className="action-row">
                  <button
                    type="button"
                    onClick={() => void runAdminAction(user.id, 'approve')}
                  >
                    承認
                  </button>
                  <button
                    type="button"
                    className="danger"
                    onClick={() => void runAdminAction(user.id, 'reject')}
                  >
                    否認
                  </button>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}
    </main>
  );
}

function StateMessage({ state }: { state: RequestState }) {
  if (state.kind === 'idle') {
    return null;
  }

  return (
    <p className={`state-message ${state.kind}`} role="status">
      {state.message}
    </p>
  );
}

async function readProblemMessage(response: Response) {
  try {
    const body = (await response.json()) as {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    };

    if (body.errors) {
      return Object.values(body.errors).flat().join(' ');
    }

    return body.detail ?? body.title ?? `HTTP ${response.status}`;
  } catch {
    return `HTTP ${response.status}`;
  }
}

function getErrorMessage(error: unknown) {
  return error instanceof Error
    ? error.message
    : '処理中に問題が発生しました。';
}

function toStatusLabel(status: string) {
  switch (status) {
    case 'PendingApproval':
      return '承認待ち';
    case 'PasskeyRegistrationPending':
      return 'パスキー登録待ち';
    case 'Active':
      return '有効';
    case 'Suspended':
      return '停止';
    case 'PasskeyResetAllowed':
      return '再登録許可中';
    case 'Rejected':
      return '否認';
    default:
      return status;
  }
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('ja-JP', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}
