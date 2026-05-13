# backend

ASP.NET Core Web API による Phase 2 の最小バックエンドです。

この段階では、起動確認用の `/health`、将来の SignalR 利用を見据えた `/hubs/chat` の接続口、仮登録と管理者承認のローカル検証 API を用意しています。

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

`status` が `ok`、`phase` が `Phase 2` の JSON が返れば、バックエンド起動確認は完了です。

## Phase 2 API

| API | 用途 |
| --- | --- |
| `POST /api/registrations` | 仮登録。成功時は `PendingApproval` |
| `GET /api/registrations/status?email=...` | 入力メールアドレスの状態確認 |
| `GET /api/admin/users/pending` | 承認待ち一覧。`Development` 環境限定 |
| `POST /api/admin/users/{id}/approve` | 承認。`PasskeyRegistrationPending` へ遷移。`Development` 環境限定 |
| `POST /api/admin/users/{id}/reject` | 否認。`Rejected` へ遷移。`Development` 環境限定 |

Phase 2 のデータはインメモリ保持です。アプリ再起動で失われます。

## Phase 2 の範囲外

- パスキー / WebAuthn の本実装
- パスキーログイン
- データベース接続
- チャット投稿、反応、お知らせ、投稿削除など Phase 2 以外の管理者操作
- 本番向け管理者認証
