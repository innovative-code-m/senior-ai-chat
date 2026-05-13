# MVP開発計画

## 現在の位置づけ

Phase 1「フロントエンドとバックエンドの最小雛形作成」は完了扱いです。

現在は、Phase 2「ユーザー仮登録と管理者承認」に入る前に、ユーザー状態、初期管理者作成、承認後案内、認証セッション、チャット履歴取得などの仕様を整理しています。

## Phase 0: リポジトリ初期化、README、AGENTS、docs作成

| 項目 | 内容 |
| --- | --- |
| 目的 | 実装前に目的、要件、設計、作業方針を明文化する |
| 成果物 | `README.md`、`AGENTS.md`、`docs/`、`src/`、`tests/`、`scripts/` の初期構成 |
| 完了条件 | 実装コードなしでGitHub公開に耐える説明と設計文書が揃っている |

## Phase 1: フロントエンドとバックエンドの最小雛形作成

| 項目 | 内容 |
| --- | --- |
| 目的 | React + TypeScript と ASP.NET Core Web API の最小構成を作る |
| 成果物 | `src/frontend/` の Vite + React + TypeScript 雛形、`src/backend/SeniorAiChat.Api/` の ASP.NET Core Web API 雛形、ローカル起動手順 |
| 完了条件 | フロントエンドとバックエンドをローカルで起動し、バックエンドは `/health` の応答を確認できる |
| Phase 1 の範囲外 | 仮登録、管理者承認、Passkey / WebAuthn 本実装、チャット投稿、データベース接続、本番デプロイ設定 |

### Phase 1 構成メモ

- フロントエンドは `src/frontend/` に作成する
- バックエンドは `src/backend/SeniorAiChat.Api/` に作成する
- バックエンドの起動確認 API は `GET /health` とする
- 将来の SignalR 利用を見据え、`/hubs/chat` の接続口だけを用意する
- Phase 1 ではパスワード欄、実データ、秘密情報、データベース接続を追加しない

### Phase 1 起動手順

フロントエンド:

```powershell
cd src/frontend
npm install
npm run dev
```

標準 URL は `http://127.0.0.1:5173`。

バックエンド:

```powershell
cd src/backend/SeniorAiChat.Api
dotnet restore
dotnet build
dotnet run
```

標準 URL は `http://localhost:5086`。起動後、`Invoke-RestMethod http://localhost:5086/health` で `status: ok` を確認する。

### Phase 1 検証記録

- 2026-05-14: `.NET SDK 9.0.310` が利用可能であることを確認
- 2026-05-14: `dotnet restore src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj` が成功
- 2026-05-14: `dotnet build src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj --no-restore` が警告 0、エラー 0 で成功
- 2026-05-14: `http://localhost:5086/health` が `status: ok` を返すことを確認
- 2026-05-14: この作業環境では `node` と `npm` が PATH 上に見つからないため、フロントエンドの `npm install`、`npm run build`、`npm run dev` は未実行
- 2026-05-14: Phase 1 の最小構成作成は完了扱いとし、フロントエンドのローカル検証は Node.js / npm が利用できる環境で別途確認する

## Phase 2: ユーザー仮登録と管理者承認

| 項目 | 内容 |
| --- | --- |
| 目的 | 参加希望者を仮登録し、管理者が承認できるようにする |
| 成果物 | 仮登録画面、承認待ち画面、管理者承認画面、ユーザー状態管理 |
| 完了条件 | 仮登録から管理者承認までの流れが動作する |
| 着手前提 | `Users.Status` の正式値、初期管理者作成方法、承認後の案内方法が文書化されている |

### Phase 2 着手前の決定事項

- `Users.Status` は `PendingApproval`、`PasskeyRegistrationPending`、`Active`、`Suspended`、`PasskeyResetAllowed`、`Rejected` とする
- `Approved` という単独の Status 値は使わない
- 初期管理者は、管理用DBへの一回限りの手動投入で作成する
- 初期管理者は `Role = Admin`、`Status = PasskeyRegistrationPending` とし、パスキー登録完了後に参加者承認を開始する
- MVPでは承認後の自動メール送信を必須にしない
- 承認後の案内は、管理者による個別連絡とパスキー登録画面での状態確認で扱う

### Phase 2 実装前提

- Phase 2 ではデータベース接続、接続文字列、マイグレーション、固定シードデータを追加しない
- Phase 2 のユーザー情報はバックエンドプロセス内のインメモリストアで保持し、ローカル検証だけに使う
- アプリ再起動で Phase 2 の登録データは失われる
- 仮登録ユーザーの `Role` は `Member` とする
- メールアドレスは前後空白を除去し、小文字化した値で重複判定する
- `PendingApproval`、`PasskeyRegistrationPending`、`Active`、`Suspended`、`PasskeyResetAllowed`、`Rejected` の既存メールアドレスでは、Phase 2 の再申請を許可しない
- 管理者 API と管理者画面は `Development` 環境限定のローカル検証機能とし、本番運用可能な無認証管理機能として残さない
- 承認後の自動メール送信は行わない
- Passkey / WebAuthn の登録、認証、検証処理は Phase 3 で扱う

### Phase 2 API

| API | 用途 | Phase 2 の扱い |
| --- | --- | --- |
| `POST /api/registrations` | 仮登録 | 入力検証後、`PendingApproval` で保存する |
| `GET /api/registrations/status?email=...` | 状態確認 | 入力メールアドレスに対応する状態だけを返す |
| `GET /api/admin/users/pending` | 承認待ち一覧 | `Development` 環境限定 |
| `POST /api/admin/users/{id}/approve` | 承認 | `PendingApproval` から `PasskeyRegistrationPending` に変更する。`Development` 環境限定 |
| `POST /api/admin/users/{id}/reject` | 否認 | `PendingApproval` から `Rejected` に変更する。`Development` 環境限定 |

### Phase 2 検証記録

- 2026-05-14: `dotnet restore src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj` が成功
- 2026-05-14: `dotnet build src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj --no-restore` が警告 0、エラー 0 で成功
- 2026-05-14: `GET /health` が `phase: Phase 2` を返すことを確認
- 2026-05-14: `POST /api/registrations` で検証用利用者Aが `PendingApproval` として登録されることを確認
- 2026-05-14: `GET /api/registrations/status?email=phase2-user-a%40example.invalid` が承認前に `PendingApproval` を返すことを確認
- 2026-05-14: `GET /api/admin/users/pending` が `Development` 環境で承認待ち 1 件を返すことを確認
- 2026-05-14: `POST /api/admin/users/{id}/approve` で `PasskeyRegistrationPending` に遷移し、状態確認 API でも同じ状態を返すことを確認
- 2026-05-14: 検証用利用者Bを仮登録し、`POST /api/admin/users/{id}/reject` で `Rejected` に遷移し、状態確認 API でも同じ状態を返すことを確認
- 2026-05-14: `Production` 環境では `GET /api/admin/users/pending` が 404 となり、無認証の管理者 API が起動しないことを確認
- 2026-05-14: この作業環境では `node` と `npm` が PATH 上に見つからないため、フロントエンドの依存関係確認、`npm run build`、`npm run dev` は未実行
- 2026-05-14: `tests/` 配下に実行可能なテストプロジェクトはまだないため、バックエンド自動テストは未実行

## Phase 3: パスキー登録とログイン

| 項目 | 内容 |
| --- | --- |
| 目的 | 承認済みユーザーがパスキーを登録し、ログインできるようにする |
| 成果物 | パスキー登録画面、ログイン画面、WebAuthn連携、再登録許可の基本設計 |
| 完了条件 | パスワードなしでパスキー登録とログインができる |
| 着手前提 | WebAuthn後のセッション方式を HttpOnly Cookie セッションとして文書化している |

## Phase 4: チャット投稿・閲覧

| 項目 | 内容 |
| --- | --- |
| 目的 | 共通チャットルームでテキスト投稿と閲覧を可能にする |
| 成果物 | チャット画面、メッセージ保存、SignalR配信 |
| 完了条件 | ログイン済みユーザーが投稿し、他の参加者が閲覧できる |
| 着手前提 | SignalR接続の認証方式と、チャット履歴のページネーション方針が文書化されている |

## Phase 5: 投稿カテゴリと反応ボタン

| 項目 | 内容 |
| --- | --- |
| 目的 | 投稿内容を分類し、短い反応を返せるようにする |
| 成果物 | カテゴリ選択、カテゴリ表示、反応ボタン、反応集計 |
| 完了条件 | 投稿にカテゴリを付けられ、参加者が反応を登録できる |
| 着手前提 | カテゴリと反応種別の固定値、反応取り消し、ユニーク制約が文書化されている |

## Phase 6: メンバー一覧と管理者機能

| 項目 | 内容 |
| --- | --- |
| 目的 | 小規模運用に必要なメンバー確認と管理操作を提供する |
| 成果物 | メンバー一覧、投稿削除、お知らせ投稿、ユーザー停止、パスキー再登録許可 |
| 完了条件 | 管理者がMVP運用に必要な基本操作を行える |

## Phase 7: 小規模試験運用

| 項目 | 内容 |
| --- | --- |
| 目的 | 実際の小規模グループで、使いやすさと運用課題を検証する |
| 成果物 | 試験運用メモ、改善点一覧、次期開発判断 |
| 完了条件 | 利用状況、困りごと、改善項目が整理されている |
