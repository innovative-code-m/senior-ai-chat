# Phase 3 実行プロンプト: パスキー登録とログイン

あなたは `senior-ai-chat` リポジトリの Phase 3 を実行する開発エージェントです。

このプロンプトの目的は、`docs/07_mvp_development_plan.md` の Phase 3「パスキー登録とログイン」を、既存ドキュメントの方針に沿って実行することです。

Phase 3 では、管理者に承認された利用者がパスキーを登録し、パスワードなしでログインできる最小の流れを作ってください。ただし、チャット投稿、メンバー一覧、お知らせ、投稿削除などの機能は Phase 3 の範囲外です。

---

## 1. 作業前に必ず読む文書

作業を始める前に、少なくとも以下を確認してください。

- `AGENTS.md`
- `README.md`
- `ROADMAP.md`
- `docs/README.md`
- `docs/01_requirements.md`
- `docs/02_screen_design.md`
- `docs/03_data_model.md`
- `docs/04_authentication_design.md`
- `docs/05_realtime_chat_design.md`
- `docs/06_admin_design.md`
- `docs/07_mvp_development_plan.md`
- `docs/decisions/0001_phase1_minimum_scaffold.md`
- `docs/decisions/0002_spec_review_followup.md`
- `docs/decisions/0003_phase2_registration_admin_approval.md`
- `docs/reviews/0001_spec_review_2026-05-13.md`
- `prompts/exec/README.md`

確認後、Phase 3 に関係する制約を自分の作業計画に反映してください。

---

## 2. Phase 3 の目的

Phase 3 の目的は、承認済み利用者がパスキーを登録し、そのパスキーでログインできるようにすることです。

完了時点で満たすこと:

- `PasskeyRegistrationPending` の利用者がパスキー登録を開始できる
- WebAuthn の登録チャレンジをサーバーが発行できる
- ブラウザの WebAuthn API で作成された資格情報をサーバーが検証できる
- パスキー登録完了後、利用者の状態が `Active` になる
- `Active` の利用者がパスキーでログインできる
- WebAuthn 検証成功後、バックエンドが HttpOnly Cookie セッションを発行する
- ログイン中の利用者を確認できる最小 API がある
- ログアウトできる
- パスワード認証、仮パスワード、共通パスワードを追加しない
- 個人情報、実在の同期生情報、秘密情報をリポジトリに含めない

---

## 3. Phase 3 着手前に必ず設計更新すること

実装を始める前に、関連する `docs/` を確認し、Phase 3 の実装前提を文書化してください。

最低限、以下を確認または更新してください。

- `docs/02_screen_design.md`
  - パスキー登録画面、ログイン画面、ログイン後の最小状態表示を Phase 3 実装に合わせる
- `docs/03_data_model.md`
  - Phase 3 で使う `UserPasskeys` の項目、チャレンジの保持方法、インメモリ実装の制限を明記する
- `docs/04_authentication_design.md`
  - WebAuthn 登録フロー、ログインフロー、チャレンジの有効期限、一回限り利用、RP ID、Origin、Cookie セッション、CORS の扱いを具体化する
- `docs/06_admin_design.md`
  - 初期管理者作成、管理者ログイン、管理者セッションを Phase 3 でどこまで扱うかを明記する
- `docs/07_mvp_development_plan.md`
  - Phase 3 の着手前提、完了条件、検証記録の追記先を確認する

設計判断を新たに行った場合は、`docs/decisions/0004_phase3_passkey_registration_login.md` を追加してください。

特に、以下は曖昧なまま実装しないでください。

- WebAuthn 実装ライブラリを使うか、どのライブラリを使うか
- パスキー情報を Phase 3 でどこに保存するか
- WebAuthn チャレンジをどこに、どれくらいの時間、どの単位で保存するか
- RP ID と Origin をローカル開発環境でどう設定するか
- フロントエンドとバックエンドが別オリジンの場合の Cookie 送信方法
- `PasskeyRegistrationPending`、`PasskeyResetAllowed`、`Active`、`Suspended`、`Rejected` の扱い
- 初期管理者と管理者承認 API を Phase 3 時点でどう扱うか

---

## 4. 重要な制約

### 4.1 プロジェクト方針

- 高齢者が安心して使える closed グループチャットの試作であることを前提にする
- 実在する同期生の氏名、メールアドレス、クラス名、旧姓、居住地などを一切入れない
- サンプル値やテスト値は、実在人物に見えない値にする
- メールアドレスの例は `example.invalid` など実配送されないドメインを使う
- 日本語ドキュメントを基本とする
- コードコメントを書く場合は日本語を優先する

### 4.2 認証方針

- パスワード認証を追加しない
- 管理者用の仮パスワード、共通パスワード、秘密の固定文字列を追加しない
- ログイン画面やサンプル UI にパスワード入力欄を作らない
- 認証は Passkey / WebAuthn を前提にする
- メールアドレスはログイン ID として扱い、本人確認の秘密情報として扱わない
- WebAuthn 検証後のセッションは HttpOnly Cookie セッションを基本とする
- JWT を MVP の第一候補にしない

### 4.3 Phase 3 のデータ永続化

Phase 2 の実装はインメモリストアです。Phase 3 でデータベースを導入するかどうかは、実装前に必ず文書化してください。

推奨は、Phase 3 ではローカル検証用のインメモリストアを拡張してパスキー登録とログインの流れを確認し、MySQL 接続、マイグレーション、初期管理者の本番投入手順は別 Phase または別作業として扱うことです。

インメモリ実装にする場合は、以下を明記してください。

- アプリ再起動で登録データとパスキー情報が失われる
- ブラウザ側に残ったパスキーは、サーバー側の資格情報が失われるとログインに使えない
- 本番運用可能な永続化方式ではない

データベースを導入する場合は、以下を実装前に文書化してください。

- 接続文字列や秘密情報をリポジトリに含めない方法
- MySQL 8.4 を前提にしたテーブル、制約、マイグレーション方針
- 初期管理者を実データなしでどう作成・検証するか
- バックアップ、ログ、個人情報の扱い

### 4.4 管理者操作の扱い

Phase 2 の管理者 API と管理者画面は `Development` 環境限定のローカル検証機能です。

Phase 3 では、管理者認証をどこまで実装するかを先に決めてください。

選択肢:

- Phase 3 では一般利用者のパスキー登録・ログインを優先し、管理者 API は Phase 2 と同じく `Development` 環境限定に留める
- Phase 3 で `Role = Admin` かつ `Status = Active` の管理者セッションだけが管理者 API を利用できる形に進める

どちらを選んでも、無認証の管理者機能を本番利用可能な形で残してはいけません。

初期管理者については、実在する氏名、メールアドレス、クラス名をリポジトリに含めないでください。固定シードデータや固定アカウントも、MVP方針に反しないか文書で確認してから判断してください。

### 4.5 MVP範囲

Phase 3 で扱う機能:

- パスキー登録画面
- ログイン画面
- WebAuthn 登録チャレンジ発行
- WebAuthn 登録結果検証
- WebAuthn ログインチャレンジ発行
- WebAuthn ログイン結果検証
- HttpOnly Cookie セッションの発行
- ログイン中利用者の確認
- ログアウト
- `PasskeyRegistrationPending` から `Active` への状態遷移
- `PasskeyResetAllowed` の再登録許可は、設計または最小土台まで

Phase 3 で扱わない機能:

- チャット閲覧
- テキスト投稿
- 投稿カテゴリ
- 反応ボタン
- メンバー一覧
- 管理者による投稿削除
- 管理者からのお知らせ
- 画像投稿
- ファイル添付
- 個別DM
- 既読表示
- スタンプ
- プッシュ通知
- AI自動要約
- 外部SNS連携
- 自動メール送信

---

## 5. 推奨する作業手順

### 5.1 現状確認

最初に以下を確認してください。

- `git status --short`
- `src/frontend/`
- `src/backend/SeniorAiChat.Api/`
- `tests/`
- `docs/`
- `prompts/`

既存変更がある場合は、ユーザーの変更として扱い、勝手に戻さないでください。

### 5.2 実装方針の決定

Phase 3 のコード変更前に、次を決めて文書に残してください。

- WebAuthn 実装ライブラリと選定理由
- パスキー情報の保存場所
- チャレンジの保存場所、有効期限、失敗時の扱い
- RP ID と Origin の設定方法
- Cookie セッションの名前、有効期限、SameSite、Secure、HttpOnly
- CORS と `credentials` の扱い
- API の入力検証とエラーメッセージ方針
- 個人情報を含む値をログや画面にどこまで表示するか
- 管理者認証を Phase 3 でどこまで扱うか

パッケージ追加が必要な場合は、目的、影響範囲、バージョン方針を確認してから追加してください。秘密情報や本番接続文字列をリポジトリに入れてはいけません。

### 5.3 バックエンド実装

`src/backend/SeniorAiChat.Api/` に、Phase 3 の最小バックエンド機能を追加してください。

推奨する構成:

- `UserPasskey` 相当のモデル
- WebAuthn チャレンジを保持するモデルまたはサービス
- パスキー登録開始 API
- パスキー登録完了 API
- パスキーログイン開始 API
- パスキーログイン完了 API
- ログイン中利用者確認 API
- ログアウト API
- Cookie セッション設定
- ユーザー状態遷移を集約するサービス

API の例:

- `POST /api/passkeys/register/options`
- `POST /api/passkeys/register/complete`
- `POST /api/auth/passkey/options`
- `POST /api/auth/passkey/complete`
- `GET /api/auth/me`
- `POST /api/auth/logout`

実際の URL は既存コードとの整合を優先して調整して構いません。ただし、README または関連 docs に記録してください。

状態遷移の基本:

| 操作 | 変更前 | 変更後 |
| --- | --- | --- |
| パスキー登録成功 | `PasskeyRegistrationPending` | `Active` |
| パスキー再登録成功 | `PasskeyResetAllowed` | `Active` |
| パスキーログイン成功 | `Active` | `Active` |

注意:

- `Approved` という Status 値は使わない
- `PendingApproval`、`Rejected`、`Suspended` の利用者にパスキー登録やログインを許可しない
- 秘密鍵そのものを保存しない
- WebAuthn 資格情報 ID、公開鍵、署名カウンタを保存する
- チャレンジは一回限りで扱い、期限切れや不一致を拒否する
- 個人情報をサーバーログへ不用意に出さない
- エラー応答で他者の存在確認につながる情報を返しすぎない

### 5.4 フロントエンド実装

`src/frontend/` に、Phase 3 の最小 UI を追加してください。

推奨する画面または表示状態:

- パスキー登録画面
- ログイン画面
- ログイン後の最小状態表示
- ログアウト操作
- WebAuthn 非対応ブラウザ向けの案内

UI 方針:

- 高齢者向けに文字サイズ、余白、ボタンサイズ、説明の分かりやすさを優先する
- 「パスキー」は「端末の安全な本人確認」と説明する
- 操作手順を短くし、次に押すボタンが分かるようにする
- エラー文は具体的な次の行動が分かる日本語にする
- パスワード欄を作らない
- 失敗時は再試行または管理者への相談に誘導する
- 一般利用者向け画面に、他者の氏名、メールアドレス、卒業時のクラス名を表示しない

WebAuthn のブラウザ処理では、サーバーとブラウザ間で `ArrayBuffer` と base64url 文字列の変換が必要になる可能性があります。独自変換を実装する場合は、小さく閉じ込めてテストしやすくしてください。

### 5.5 ドキュメント更新

実装内容に合わせて、関連ドキュメントを更新してください。

最低限の候補:

- `README.md`
- `docs/02_screen_design.md`
- `docs/03_data_model.md`
- `docs/04_authentication_design.md`
- `docs/06_admin_design.md`
- `docs/07_mvp_development_plan.md`
- `docs/decisions/0004_phase3_passkey_registration_login.md`
- `log/working/`

更新内容には、以下を含めてください。

- Phase 3 で作った画面と API
- WebAuthn 登録とログインの流れ
- Cookie セッションの扱い
- ユーザー状態遷移の扱い
- Phase 3 時点のデータ永続化の制限
- 管理者機能の Phase 3 時点の制限
- ローカル起動、検証手順
- Phase 3 で未実装の範囲

---

## 6. 検証

可能な範囲で以下を実行してください。

- `dotnet restore`
- `dotnet build`
- バックエンドのテスト
- バックエンドのローカル起動確認
- 仮登録 API の手動確認
- 管理者承認 API の手動確認
- パスキー登録開始 API の手動確認
- パスキー登録完了 API のブラウザ確認
- パスキーログイン開始 API の手動確認
- パスキーログイン完了 API のブラウザ確認
- `GET /api/auth/me` の確認
- `POST /api/auth/logout` の確認
- `Production` 環境で無認証の管理者 API が利用できないことの確認
- フロントエンドの依存関係確認
- フロントエンドのビルド
- フロントエンドのローカル起動確認
- ブラウザで仮登録、承認、パスキー登録、ログイン、ログアウトの一連の確認

WebAuthn はブラウザの実装、OS、端末、Origin、HTTPS または localhost 条件に依存します。ローカル検証では、どの URL、ブラウザ、確認手順で成功または失敗したかを記録してください。

ネットワーク制限などで依存関係取得に失敗した場合は、失敗内容を明記し、必要なら承認を求めて再実行してください。

Node.js / npm が利用できない環境では、フロントエンド検証を未実行として記録し、バックエンド検証を優先してください。

---

## 7. 完了報告に含める内容

作業完了時は、以下を簡潔に報告してください。

- 更新した主なドキュメント
- 作成・更新した主なコードファイル
- 追加または変更した API
- 追加または変更した画面
- WebAuthn 実装ライブラリと選定理由
- Cookie セッションの扱い
- ユーザー状態遷移の扱い
- 管理者機能の Phase 3 時点の制限
- 実行した検証コマンド
- ブラウザで確認した WebAuthn の流れ
- 未実行または失敗した検証
- Phase 4 に進む前の注意点

---

## 8. 完了条件

このプロンプトによる作業は、以下を満たしたら完了です。

- Phase 3 の実装前提が `docs/` または `docs/decisions/` に記録されている
- `PasskeyRegistrationPending` の利用者がパスキー登録できる
- パスキー登録完了後、利用者状態が `Active` になる
- `Active` の利用者がパスキーでログインできる
- WebAuthn 検証成功後、HttpOnly Cookie セッションが発行される
- ログイン中利用者を確認できる
- ログアウトできる
- `PendingApproval`、`Rejected`、`Suspended` の利用者がパスキー登録やログインをできない
- パスワード認証が追加されていない
- チャット投稿、メンバー一覧、お知らせなど Phase 4 以降の機能が混入していない
- 実在の個人情報や秘密情報が含まれていない
- ローカル起動手順と検証結果が報告されている
