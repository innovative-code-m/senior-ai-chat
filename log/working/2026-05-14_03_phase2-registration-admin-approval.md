# Phase 2 作業ログ

## 日付

2026-05-14

## 対象

- プロンプト: `prompts/exec/02_phase2_registration_admin_approval.md`
- 関連コミット: `06e1987 Implement phase 2 registration approval flow`
- 関連決定記録: `docs/decisions/0003_phase2_registration_admin_approval.md`

## 目的

参加希望者が仮登録し、管理者が承認または否認できる最小のローカル検証フローを作成する。

## 実施内容

- `docs/02_screen_design.md`、`docs/03_data_model.md`、`docs/04_authentication_design.md`、`docs/06_admin_design.md`、`docs/07_mvp_development_plan.md` に Phase 2 の前提と実装内容を追記した。
- `docs/decisions/0003_phase2_registration_admin_approval.md` に、インメモリストア、Development 環境限定の管理者 API、状態遷移、重複メールアドレスの扱いを記録した。
- バックエンドに仮登録、状態確認、承認待ち一覧、承認、否認の API を追加した。
- ユーザー状態として `PendingApproval`、`PasskeyRegistrationPending`、`Rejected` への遷移を実装した。
- メールアドレスは前後空白を除去し、小文字化した値で重複判定する方針とした。
- 管理者 API は `Development` 環境限定とし、本番環境では無認証の管理者 API が起動しないようにした。
- フロントエンドに仮登録フォーム、状態確認、開発用の管理者承認画面を追加した。
- `README.md`、`src/frontend/README.md`、`src/backend/README.md` に Phase 2 の起動手順と API を反映した。

## 制約と判断

- Phase 2 のデータ保持はバックエンドプロセス内のインメモリストアに限定した。
- データベース接続、接続文字列、マイグレーション、固定シードデータは追加していない。
- 初期管理者レコードは Phase 2 では作成していない。
- 承認後の自動メール送信は行わず、管理者の個別連絡と状態確認画面で扱う前提とした。
- Passkey / WebAuthn の登録、認証、検証、パスキーログイン、チャット機能は実装していない。
- `Approved` という Status 値は使わず、承認後は `PasskeyRegistrationPending` で止める。

## 検証

- `dotnet restore src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj` が成功した。
- `dotnet build src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj --no-restore` が警告 0、エラー 0 で成功した。
- `GET /health` が `phase: Phase 2` を返すことを確認した。
- `POST /api/registrations` で検証用利用者が `PendingApproval` として登録されることを確認した。
- `GET /api/registrations/status?email=...` が承認前に `PendingApproval` を返すことを確認した。
- `GET /api/admin/users/pending` が `Development` 環境で承認待ちを返すことを確認した。
- `POST /api/admin/users/{id}/approve` で `PasskeyRegistrationPending` に遷移することを確認した。
- `POST /api/admin/users/{id}/reject` で `Rejected` に遷移することを確認した。
- `Production` 環境では `GET /api/admin/users/pending` が 404 となり、無認証の管理者 API が起動しないことを確認した。
- この作業環境では `node` と `npm` が PATH 上に見つからなかったため、フロントエンドの依存関係確認、`npm run build`、`npm run dev` は未実行とした。
- `tests/` 配下に実行可能なテストプロジェクトはまだないため、バックエンド自動テストは未実行とした。

## 後続作業

- Phase 3 で Passkey / WebAuthn の登録、ログイン、管理者認証を扱う。
- データベース導入時に、メールアドレスのユニーク制約、初期管理者投入手順、管理操作ログを設計する。
- 否認済みまたは停止済みユーザーの再申請を許可する場合は、運用方針を先に文書化する。
