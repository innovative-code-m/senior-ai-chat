# backend

ASP.NET Core Web API による Phase 3 の最小バックエンドです。

この段階では、起動確認用の `/health`、将来の SignalR 利用を見据えた `/hubs/chat` の接続口、仮登録、開発環境限定の管理者承認、Passkey / WebAuthn 登録・ログイン、HttpOnly Cookie セッションを用意しています。

## 前提

- .NET SDK 9

## restore / build

```powershell
cd src/backend/SeniorAiChat.Api
dotnet restore
dotnet build
```

## 起動

```powershell
cd src/backend/SeniorAiChat.Api
dotnet run
```

標準では `http://localhost:5086` で起動します。

## 起動確認

```powershell
Invoke-RestMethod http://localhost:5086/health
```

`status` が `ok`、`phase` が `Phase 3` の JSON が返れば、バックエンド起動確認は完了です。

## Phase 3 API

| API | 用途 |
| --- | --- |
| `POST /api/registrations` | 仮登録。成功時は `PendingApproval` |
| `GET /api/registrations/status?email=...` | 入力メールアドレスの状態確認 |
| `GET /api/admin/users/pending` | 承認待ち一覧。`Development` 環境限定 |
| `POST /api/admin/users/{id}/approve` | 承認。`PasskeyRegistrationPending` へ遷移。`Development` 環境限定 |
| `POST /api/admin/users/{id}/reject` | 否認。`Rejected` へ遷移。`Development` 環境限定 |
| `POST /api/passkeys/register/options` | WebAuthn 登録チャレンジ発行 |
| `POST /api/passkeys/register/complete` | WebAuthn 登録結果検証。成功時は `Active` へ遷移 |
| `POST /api/auth/passkey/options` | WebAuthn ログインチャレンジ発行 |
| `POST /api/auth/passkey/complete` | WebAuthn ログイン結果検証。成功時は `sac_session` Cookie を発行 |
| `GET /api/auth/me` | Cookie セッションによるログイン状態確認 |
| `POST /api/auth/logout` | サーバー側セッション削除と Cookie 失効 |

Phase 3 のデータはインメモリ保持です。アプリ再起動で仮登録、承認状態、パスキー情報、チャレンジ、セッションは失われます。

## WebAuthn 設定

ローカル既定値:

- RP ID: `localhost`
- RP Name: `senior-ai-chat`
- 許可 Origin: `http://localhost:5086`、`http://localhost:5173`、`http://127.0.0.1:5173`
- チャレンジ有効期限: 5分
- Cookie セッション名: `sac_session`
- Cookie 有効期限: 8時間

本番環境では HTTPS と実際の公開ドメインに合わせて RP ID、Origin、Cookie 設定を見直します。

## Phase 3 の範囲外

- チャット投稿、閲覧、反応、お知らせ、投稿削除
- データベース接続、接続文字列、マイグレーション
- 本番向け管理者認証、初期管理者投入手順
- パスキー複数登録の管理画面、パスキー無効化、管理者による再登録許可 UI
- 画像投稿、ファイル添付、個別DM、通知、AI自動要約
