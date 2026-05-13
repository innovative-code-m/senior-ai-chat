import { FormEvent, useState } from 'react';

type ViewMode =
  | 'register'
  | 'status'
  | 'passkeyRegister'
  | 'login'
  | 'session'
  | 'admin';

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

type CredentialDescriptorJson = {
  type: PublicKeyCredentialType;
  id: string;
  transports?: AuthenticatorTransport[];
};

type CredentialCreationOptionsJson = Omit<
  PublicKeyCredentialCreationOptions,
  'challenge' | 'excludeCredentials' | 'user'
> & {
  challenge: string;
  excludeCredentials?: CredentialDescriptorJson[];
  user: Omit<PublicKeyCredentialUserEntity, 'id'> & { id: string };
};

type CredentialRequestOptionsJson = Omit<
  PublicKeyCredentialRequestOptions,
  'allowCredentials' | 'challenge'
> & {
  allowCredentials?: CredentialDescriptorJson[];
  challenge: string;
};

type WebAuthnOptionsResponse<TOptions> = {
  challengeId: string;
  publicKey: TOptions;
};

type PasskeyActionResponse = {
  userId: string;
  status: string;
  message: string;
};

type LoginCompleteResponse = {
  userId: string;
  status: string;
  role: string;
  message: string;
};

type CurrentUserResponse = {
  userId: string;
  status: string;
  role: string;
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
  const [passkeyEmail, setPasskeyEmail] = useState('');
  const [passkeyState, setPasskeyState] = useState<RequestState>({
    kind: 'idle'
  });
  const [loginEmail, setLoginEmail] = useState('');
  const [loginState, setLoginState] = useState<RequestState>({ kind: 'idle' });
  const [currentUser, setCurrentUser] = useState<CurrentUserResponse | null>(
    null
  );
  const [sessionState, setSessionState] = useState<RequestState>({
    kind: 'idle'
  });
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

      if (result.status === 'PasskeyRegistrationPending') {
        setPasskeyEmail(statusEmail);
      }
    } catch (error) {
      setStatusState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const registerPasskey = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPasskeyState({
      kind: 'loading',
      message: '端末の安全な本人確認を始めます。'
    });

    try {
      ensureWebAuthnSupport();

      const optionsResponse = await postJson<
        WebAuthnOptionsResponse<CredentialCreationOptionsJson>
      >('/api/passkeys/register/options', { email: passkeyEmail });

      const credential = await navigator.credentials.create({
        publicKey: toCredentialCreationOptions(optionsResponse.publicKey)
      });

      if (!(credential instanceof PublicKeyCredential)) {
        throw new Error('パスキー登録が中断されました。もう一度お試しください。');
      }

      const result = await postJson<PasskeyActionResponse>(
        '/api/passkeys/register/complete',
        {
          challengeId: optionsResponse.challengeId,
          credential: serializeRegistrationCredential(credential)
        }
      );

      setPasskeyState({
        kind: 'success',
        message: `${result.message} 現在の状態: ${toStatusLabel(result.status)}`
      });
      setLoginEmail(passkeyEmail);
    } catch (error) {
      setPasskeyState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const loginWithPasskey = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoginState({
      kind: 'loading',
      message: '端末の本人確認でログインします。'
    });

    try {
      ensureWebAuthnSupport();

      const optionsResponse = await postJson<
        WebAuthnOptionsResponse<CredentialRequestOptionsJson>
      >('/api/auth/passkey/options', { email: loginEmail });

      const credential = await navigator.credentials.get({
        publicKey: toCredentialRequestOptions(optionsResponse.publicKey)
      });

      if (!(credential instanceof PublicKeyCredential)) {
        throw new Error('ログインが中断されました。もう一度お試しください。');
      }

      const result = await postJson<LoginCompleteResponse>(
        '/api/auth/passkey/complete',
        {
          challengeId: optionsResponse.challengeId,
          credential: serializeLoginCredential(credential)
        },
        true
      );

      setCurrentUser({
        userId: result.userId,
        status: result.status,
        role: result.role
      });
      setLoginState({
        kind: 'success',
        message: result.message
      });
      setSessionState({
        kind: 'success',
        message: 'ログイン中です。'
      });
    } catch (error) {
      setLoginState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const loadCurrentUser = async () => {
    setSessionState({
      kind: 'loading',
      message: 'ログイン状態を確認しています。'
    });

    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/me`, {
        credentials: 'include',
        headers: { Accept: 'application/json' }
      });

      if (response.status === 401) {
        setCurrentUser(null);
        setSessionState({
          kind: 'error',
          message: '現在はログインしていません。'
        });
        return;
      }

      if (!response.ok) {
        throw new Error(await readProblemMessage(response));
      }

      const result = (await response.json()) as CurrentUserResponse;
      setCurrentUser(result);
      setSessionState({
        kind: 'success',
        message: 'ログイン中です。'
      });
    } catch (error) {
      setCurrentUser(null);
      setSessionState({
        kind: 'error',
        message: getErrorMessage(error)
      });
    }
  };

  const logout = async () => {
    setSessionState({
      kind: 'loading',
      message: 'ログアウトしています。'
    });

    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: { Accept: 'application/json' }
      });

      if (!response.ok) {
        throw new Error(await readProblemMessage(response));
      }

      setCurrentUser(null);
      setSessionState({
        kind: 'success',
        message: 'ログアウトしました。'
      });
    } catch (error) {
      setSessionState({
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
    const targetEmail = pendingUsers.find((user) => user.id === userId)?.email;
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
      if (action === 'approve' && targetEmail) {
        setPasskeyEmail(targetEmail);
      }
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
        <p className="phase-label">Phase 3 ローカル検証</p>
        <h1>senior-ai-chat</h1>
        <p className="lead">
          仮登録、管理者承認、パスキー登録、パスキーログインの最小の流れを確認します。
        </p>
      </header>

      <nav className="mode-tabs" aria-label="作業の切り替え">
        <ModeButton
          active={viewMode === 'register'}
          onClick={() => setViewMode('register')}
        >
          仮登録
        </ModeButton>
        <ModeButton
          active={viewMode === 'status'}
          onClick={() => setViewMode('status')}
        >
          状態確認
        </ModeButton>
        <ModeButton
          active={viewMode === 'passkeyRegister'}
          onClick={() => setViewMode('passkeyRegister')}
        >
          パスキー登録
        </ModeButton>
        <ModeButton
          active={viewMode === 'login'}
          onClick={() => setViewMode('login')}
        >
          ログイン
        </ModeButton>
        <ModeButton
          active={viewMode === 'session'}
          onClick={() => {
            setViewMode('session');
            void loadCurrentUser();
          }}
        >
          ログイン状態
        </ModeButton>
        {isDevelopment && (
          <ModeButton
            active={viewMode === 'admin'}
            onClick={() => {
              setViewMode('admin');
              void loadPendingUsers();
            }}
          >
            開発用管理
          </ModeButton>
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
            送信後は管理者の確認待ちになります。承認後はパスキー登録へ進みます。
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

      {viewMode === 'passkeyRegister' && (
        <section className="work-panel" aria-labelledby="passkey-title">
          <div className="panel-heading">
            <p className="section-label">端末の安全な本人確認</p>
            <h2 id="passkey-title">パスキー登録</h2>
          </div>
          <form className="form-grid compact" onSubmit={registerPasskey}>
            <label>
              <span>メールアドレス</span>
              <input
                value={passkeyEmail}
                onChange={(event) => setPasskeyEmail(event.target.value)}
                type="email"
                autoComplete="email"
              />
            </label>
            <button type="submit">パスキーを登録</button>
          </form>
          <p className="support-text">
            承認済みの方だけが登録できます。画面の案内に従って、顔認証、指紋認証、端末のロック解除などを行います。
          </p>
          <StateMessage state={passkeyState} />
        </section>
      )}

      {viewMode === 'login' && (
        <section className="work-panel" aria-labelledby="login-title">
          <div className="panel-heading">
            <p className="section-label">パスワードなし</p>
            <h2 id="login-title">ログイン</h2>
          </div>
          <form className="form-grid compact" onSubmit={loginWithPasskey}>
            <label>
              <span>メールアドレス</span>
              <input
                value={loginEmail}
                onChange={(event) => setLoginEmail(event.target.value)}
                type="email"
                autoComplete="email"
              />
            </label>
            <button type="submit">パスキーでログイン</button>
          </form>
          <p className="support-text">
            パスワードは使いません。登録した端末の本人確認でログインします。
          </p>
          <StateMessage state={loginState} />
        </section>
      )}

      {viewMode === 'session' && (
        <section className="work-panel" aria-labelledby="session-title">
          <div className="panel-heading admin-heading">
            <div>
              <p className="section-label">現在の状態</p>
              <h2 id="session-title">ログイン状態</h2>
            </div>
            <button
              type="button"
              className="secondary"
              onClick={() => void loadCurrentUser()}
            >
              再確認
            </button>
          </div>
          <StateMessage state={sessionState} />
          {currentUser && (
            <div className="session-summary">
              <dl>
                <div>
                  <dt>状態</dt>
                  <dd>{toStatusLabel(currentUser.status)}</dd>
                </div>
                <div>
                  <dt>権限</dt>
                  <dd>{toRoleLabel(currentUser.role)}</dd>
                </div>
              </dl>
              <button type="button" className="danger" onClick={() => void logout()}>
                ログアウト
              </button>
            </div>
          )}
          <p className="support-text">
            Phase 3 ではログイン確認までを扱います。チャット画面は次の Phase で実装します。
          </p>
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
            この管理画面はローカル検証用です。本番運用には使いません。
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

function ModeButton({
  active,
  children,
  onClick
}: {
  active: boolean;
  children: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      className={active ? 'active' : ''}
      onClick={onClick}
    >
      {children}
    </button>
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

async function postJson<TResponse>(
  path: string,
  body: unknown,
  includeCredentials = false
) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    credentials: includeCredentials ? 'include' : 'same-origin',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw new Error(await readProblemMessage(response));
  }

  return (await response.json()) as TResponse;
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

function ensureWebAuthnSupport() {
  if (!window.PublicKeyCredential || !navigator.credentials) {
    throw new Error(
      'このブラウザではパスキーを利用できません。別のブラウザまたは端末でお試しください。'
    );
  }
}

function toCredentialCreationOptions(
  json: CredentialCreationOptionsJson
): PublicKeyCredentialCreationOptions {
  return {
    ...json,
    challenge: base64UrlToArrayBuffer(json.challenge),
    excludeCredentials: json.excludeCredentials?.map(toCredentialDescriptor),
    user: {
      ...json.user,
      id: base64UrlToArrayBuffer(json.user.id)
    }
  };
}

function toCredentialRequestOptions(
  json: CredentialRequestOptionsJson
): PublicKeyCredentialRequestOptions {
  return {
    ...json,
    allowCredentials: json.allowCredentials?.map(toCredentialDescriptor),
    challenge: base64UrlToArrayBuffer(json.challenge)
  };
}

function toCredentialDescriptor(
  descriptor: CredentialDescriptorJson
): PublicKeyCredentialDescriptor {
  return {
    ...descriptor,
    id: base64UrlToArrayBuffer(descriptor.id)
  };
}

function serializeRegistrationCredential(credential: PublicKeyCredential) {
  const response = credential.response as AuthenticatorAttestationResponse;
  const responseWithTransports = response as AuthenticatorAttestationResponse & {
    getTransports?: () => AuthenticatorTransport[];
  };

  return {
    id: credential.id,
    rawId: arrayBufferToBase64Url(credential.rawId),
    type: credential.type,
    response: {
      attestationObject: arrayBufferToBase64Url(response.attestationObject),
      clientDataJson: arrayBufferToBase64Url(response.clientDataJSON),
      transports: responseWithTransports.getTransports?.()
    },
    extensions: credential.getClientExtensionResults(),
    clientExtensionResults: credential.getClientExtensionResults()
  };
}

function serializeLoginCredential(credential: PublicKeyCredential) {
  const response = credential.response as AuthenticatorAssertionResponse;

  return {
    id: credential.id,
    rawId: arrayBufferToBase64Url(credential.rawId),
    type: credential.type,
    response: {
      authenticatorData: arrayBufferToBase64Url(response.authenticatorData),
      clientDataJson: arrayBufferToBase64Url(response.clientDataJSON),
      signature: arrayBufferToBase64Url(response.signature),
      userHandle: response.userHandle
        ? arrayBufferToBase64Url(response.userHandle)
        : null
    },
    extensions: credential.getClientExtensionResults(),
    clientExtensionResults: credential.getClientExtensionResults()
  };
}

function base64UrlToArrayBuffer(value: string) {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
  const binary = window.atob(padded);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  const output = new ArrayBuffer(bytes.byteLength);
  new Uint8Array(output).set(bytes);

  return output;
}

function arrayBufferToBase64Url(buffer: ArrayBuffer) {
  const bytes = new Uint8Array(buffer);
  let binary = '';

  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return window
    .btoa(binary)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
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

function toRoleLabel(role: string) {
  switch (role) {
    case 'Admin':
      return '管理者';
    case 'Member':
      return 'メンバー';
    default:
      return role;
  }
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('ja-JP', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}
