# Phase 3 作業ログ

## 日付

2026-05-14

## 対象

- プロンプト: `prompts/exec/03_phase3_passkey_registration_login.md`
- 関連決定記録: `docs/decisions/0004_phase3_passkey_registration_login.md`

## 目的

承認済み利用者がパスキーを登録し、パスワードなしでログインできる最小のローカル検証フローを作成する。

## 実施内容

- `docs/02_screen_design.md`、`docs/03_data_model.md`、`docs/04_authentication_design.md`、`docs/06_admin_design.md`、`docs/07_mvp_development_plan.md` に Phase 3 の前提と実装内容を追記した。
- `docs/decisions/0004_phase3_passkey_registration_login.md` に、`Fido2.AspNet` 4.0.1、インメモリ保存、チャレンジ、Cookie セッション、RP ID / Origin、管理者 API の扱いを記録した。
- バックエンドに WebAuthn 登録開始・完了、ログイン開始・完了、ログイン状態確認、ログアウト API を追加した。
- `PasskeyRegistrationPending` または `PasskeyResetAllowed` の利用者だけがパスキー登録でき、登録成功後は `Active` へ遷移する形にした。
- `Active` で登録済みパスキーを持つ利用者だけがパスキーログインできる形にした。
- WebAuthn 検証成功後に `sac_session` の HttpOnly Cookie セッションを発行する形にした。
- フロントエンドにパスキー登録画面、ログイン画面、ログイン状態確認、ログアウト操作を追加した。
- WebAuthn の `ArrayBuffer` と base64url 文字列の変換をフロントエンド内の小さな関数に閉じ込めた。
- `README.md`、`ROADMAP.md`、`AGENTS.md`、`src/README.md`、`src/frontend/README.md`、`src/backend/README.md` を Phase 3 の状態に更新した。

## 制約と判断

- Phase 3 のデータ保持はバックエンドプロセス内のインメモリストアに限定した。
- データベース接続、接続文字列、マイグレーション、固定シードデータは追加していない。
- アプリ再起動で仮登録、承認状態、パスキー情報、チャレンジ、セッションは失われる。
- ブラウザやOS側に残ったパスキーは、サーバー側の資格情報が失われるとログインに使えない。
- 管理者 API と管理者画面は Phase 2 と同じく `Development` 環境限定に留めた。
- 本番向け管理者認証、初期管理者作成手順、パスキー再登録許可 UI は後続 Phase または別作業で扱う。
- パスワード認証、仮パスワード、共通パスワードは追加していない。
- チャット閲覧、投稿、反応、メンバー一覧、お知らせ、投稿削除は実装していない。

## 検証

- `dotnet add src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj package Fido2.AspNet --version 4.0.1` が成功した。
- `dotnet build src/backend/SeniorAiChat.Api/SeniorAiChat.Api.csproj --no-restore` が警告 0、エラー 0 で成功した。
- `GET /health` が `phase: Phase 3` を返すことを確認した。
- `POST /api/registrations` で検証用利用者が `PendingApproval` として登録されることを確認した。
- `GET /api/registrations/status?email=...` が承認前に `PendingApproval` を返すことを確認した。
- `GET /api/admin/users/pending` が `Development` 環境で承認待ちを返すことを確認した。
- `POST /api/admin/users/{id}/approve` で `PasskeyRegistrationPending` に遷移することを確認した。
- `POST /api/passkeys/register/options` が `PasskeyRegistrationPending` の利用者に WebAuthn 登録チャレンジを返すことを確認した。
- 登録チャレンジの RP ID が `localhost`、RP Name が `senior-ai-chat`、attestation が `none` であることを確認した。
- `PendingApproval` の利用者が `POST /api/passkeys/register/options` で 409 となることを確認した。
- パスキー未登録の利用者が `POST /api/auth/passkey/options` で 409 となることを確認した。
- セッションなしの `GET /api/auth/me` が 401 となることを確認した。
- `POST /api/auth/logout` が 200 を返し、`sac_session` Cookie を失効させることを確認した。
- `ASPNETCORE_ENVIRONMENT=Production` かつ `--no-launch-profile` で起動し、`GET /api/admin/users/pending` が 404 となることを確認した。

## 未実行

- この作業環境では `node` と `npm` が PATH 上に見つからなかったため、フロントエンドの依存関係確認、`npm run build`、`npm run dev` は未実行。
- フロントエンドを起動できないため、ブラウザでの `navigator.credentials.create` によるパスキー登録完了、`navigator.credentials.get` によるパスキーログイン完了、`GET /api/auth/me`、ログアウトの通し確認は未実行。
- `tests/` 配下に実行可能なテストプロジェクトはまだないため、バックエンド自動テストは未実行。

## 後続作業

- Node.js / npm が利用できる環境で `http://localhost:5173` からブラウザ WebAuthn の通し確認を行う。
- Phase 4 に進む前に、Cookie セッションで `Active` ユーザーだけがチャット API と SignalR Hub を利用できることを実装で確認する。
- データベース導入時に、`UserPasskeys`、チャレンジ、セッション、初期管理者投入手順、管理操作ログを設計する。
- 本番環境では HTTPS、公開ドメインに合わせた RP ID / Origin、Cookie Secure、CORS、ログ保存方針を環境別に確定する。
