# senior-ai-chat

`senior-ai-chat` は、高齢者が信頼できる小規模コミュニティの中で、生成AIの使い方を学び合い、疑問や体験を共有するための closed グループチャット試作です。

初期想定は、高校同期生のみが参加する closed なグループチャットです。主な話題は、ChatGPT などの生成AIの使い方に関する情報交換です。

このプロジェクトの本質的な目的は、高齢者がAIをどのように受け止め、どのような場面で活用できるかを観察・検証することです。

## 主要ファイル

- `README.md`: プロジェクト概要、目的、起動手順、主要構成
- `ROADMAP.md`: Phaseごとの進行状態、残作業、次に着手できる範囲
- `AGENTS.md`: AIエージェント向けの作業方針、制約、個人情報方針

## 位置づけ

このリポジトリは、AI活用実験系のリポジトリとして `innovative-code-m` 配下で管理します。

COBOL to C# 移行設計・構造解析・移行保証を主軸とする `innovativecodejp` 配下のリポジトリ群とは分離します。

```text
innovativecodejp:
  COBOL to C# 移行設計
  COBOL構造解析
  移行保証
  業務ロジック抽出

innovative-code-m:
  AI活用実験
  小規模Webアプリ試作
  AIエージェント活用開発
  高齢者向けAI利用支援
```

## 背景

生成AIは日常利用に広がりつつありますが、高齢者にとっては用語、操作、ログイン、リスク理解が障壁になりやすいです。

一方で、信頼できる同期生のような小規模コミュニティであれば、初歩的な疑問や失敗体験を共有しやすく、安心して学び続けられる可能性があります。

## 目的

- 高齢者が生成AIをどのように理解し、受け止めるかを観察する
- 生成AIの使い方を共有できる closed な場を試作する
- 高齢者向けAI利用支援に必要なUI、運用、注意喚起を検証する
- 小規模試験運用に耐えるMVPの要件を整理する

## 現在のフェーズ

フェーズごとの完了、完了扱い、未着手の状態は `ROADMAP.md` で管理します。

現在は Phase 3「パスキー登録とログイン」のローカル検証実装まで完了扱いです。仮登録、状態確認、開発環境限定の管理者承認 API / 画面、Passkey / WebAuthn 登録・ログイン API、HttpOnly Cookie セッション、ログイン状態確認、ログアウトを追加しています。

チャット投稿、データベース接続、本番向け管理者認証は後続 Phase で扱います。Phase 3 のデータはインメモリ保持のため、アプリ再起動で仮登録、承認状態、パスキー情報、セッションが失われます。

## 初期段階の重要方針

1. プロジェクトの目的を README に明確化する
2. `AGENTS.md` にAIエージェント向け作業方針を書く
3. `docs/` 配下に要件定義・設計文書の置き場を作る
4. 実装用ディレクトリは空、または `.gitkeep` で準備する
5. GitHub公開に耐える最低限の説明を整える
6. 実在の同期生情報・個人情報は絶対に入れない

## 想定利用者

- 高校同期生
- 今年77歳となる世代を主に想定
- AI初心者を含む
- スマートフォン利用者を主に想定
- PCブラウザ利用も許容

## 参加資格

- 高校の同期生であること
- 実名で登録すること
- メールアドレスを登録すること
- 卒業時のクラス名を登録すること
- 管理者によって承認されること

実名、メールアドレス、卒業時のクラス名は個人情報として扱います。このリポジトリには、実在する同期生の氏名、メールアドレス、クラス名、旧姓、居住地などを含めません。

## MVP範囲

MVPで実装対象とする機能候補は以下です。

- 仮登録
- 管理者承認
- パスキー登録
- パスキーログイン
- チャット閲覧
- テキスト投稿
- 投稿カテゴリ
- 反応ボタン
- メンバー一覧
- 管理者による投稿削除
- 管理者からのお知らせ

MVPで実装しない機能は以下です。

- 画像投稿
- ファイル添付
- 個別DM
- 既読表示
- スタンプ
- プッシュ通知
- AI自動要約
- 外部SNS連携

## 認証方針

- パスワードは使用しない
- パスキー / WebAuthn を採用する
- メールアドレスはログインIDとして使用する
- 管理者承認後にパスキーを登録する
- 端末変更時は管理者による再登録許可で復旧する

## 技術構成予定

```text
frontend:
  React + TypeScript

backend:
  ASP.NET Core Web API
  SignalR

authentication:
  Passkey / WebAuthn

notification:
  MVPでは承認後の自動メール送信を必須にしない

database:
  MySQL 8.4

development environment:
  VS Code
  dotnet CLI
  npm

deployment:
  さくらインターネット
```

## Phase 3 ローカル起動

### 前提ツール

- Node.js 20.19 以降
- npm
- .NET SDK 9

### バックエンド

```powershell
cd src/backend/SeniorAiChat.Api
dotnet restore
dotnet build
dotnet run
```

標準の起動 URL は `http://localhost:5086` です。

起動確認:

```powershell
Invoke-RestMethod http://localhost:5086/health
```

`status` が `ok`、`phase` が `Phase 3` の JSON が返れば、バックエンドの起動確認は完了です。SignalR の接続口は `/hubs/chat` に用意していますが、Phase 3 ではチャット配信処理は実装していません。

Phase 3 の主な API:

| API | 用途 |
| --- | --- |
| `POST /api/registrations` | 氏名、メールアドレス、卒業時のクラス名を受け付け、`PendingApproval` で仮登録する |
| `GET /api/registrations/status?email=...` | 入力メールアドレスの申請状態を確認する |
| `GET /api/admin/users/pending` | 承認待ち一覧を取得する。`Development` 環境限定 |
| `POST /api/admin/users/{id}/approve` | 承認し、`PasskeyRegistrationPending` に変更する。`Development` 環境限定 |
| `POST /api/admin/users/{id}/reject` | 否認し、`Rejected` に変更する。`Development` 環境限定 |
| `POST /api/passkeys/register/options` | `PasskeyRegistrationPending` または `PasskeyResetAllowed` の利用者に WebAuthn 登録チャレンジを発行する |
| `POST /api/passkeys/register/complete` | WebAuthn 登録結果を検証し、成功時に状態を `Active` にする |
| `POST /api/auth/passkey/options` | `Active` の利用者に WebAuthn ログインチャレンジを発行する |
| `POST /api/auth/passkey/complete` | WebAuthn ログイン結果を検証し、HttpOnly Cookie セッションを発行する |
| `GET /api/auth/me` | Cookie セッションからログイン中利用者の最小情報を返す |
| `POST /api/auth/logout` | サーバー側セッションを削除し、Cookie を失効させる |

Phase 3 のデータはバックエンドプロセス内のインメモリストアに保持します。アプリを再起動すると登録データ、パスキー情報、チャレンジ、セッションは失われます。ブラウザやOS側に残ったパスキーも、サーバー側の資格情報が失われるとログインに使えません。データベース接続、接続文字列、固定シードデータは追加していません。

### フロントエンド

```powershell
cd src/frontend
npm install
npm run dev -- --host localhost
```

WebAuthn のローカル検証では、RP ID を `localhost` としているため、まず `http://localhost:5173` から確認します。

バックエンド URL を変える場合は、`src/frontend/.env.example` を参考に `.env` を作成し、`VITE_API_BASE_URL` を設定します。

## 主なディレクトリ構成

```text
.
├── AGENTS.md
├── README.md
├── ROADMAP.md
├── .editorconfig
├── .gitignore
├── docs/
│   ├── README.md
│   ├── 01_requirements.md
│   ├── 02_screen_design.md
│   ├── 03_data_model.md
│   ├── 04_authentication_design.md
│   ├── 05_realtime_chat_design.md
│   ├── 06_admin_design.md
│   ├── 07_mvp_development_plan.md
│   ├── decisions/
│   │   ├── README.md
│   │   ├── 0001_phase1_minimum_scaffold.md
│   │   ├── 0002_spec_review_followup.md
│   │   ├── 0003_phase2_registration_admin_approval.md
│   │   └── 0004_phase3_passkey_registration_login.md
│   └── reviews/
│       └── 0001_spec_review_2026-05-13.md
├── log/
│   ├── research/
│   │   └── README.md
│   └── working/
│       ├── README.md
│       ├── 2026-05-14_01_initialize-project-documentation.md
│       ├── 2026-05-14_02_phase1-minimum-scaffold.md
│       ├── 2026-05-14_03_phase2-registration-admin-approval.md
│       └── 2026-05-14_04_phase3-passkey-registration-login.md
├── prompts/
│   ├── README.md
│   ├── init/
│   │   ├── README.md
│   │   └── 00_initialize.md
│   └── exec/
│       ├── README.md
│       ├── 01_phase1_minimum_scaffold.md
│       ├── 02_phase2_registration_admin_approval.md
│       └── 03_phase3_passkey_registration_login.md
├── src/
│   ├── README.md
│   ├── frontend/
│   │   ├── README.md
│   │   ├── package.json
│   │   ├── index.html
│   │   └── src/
│   └── backend/
│       ├── README.md
│       └── SeniorAiChat.Api/
├── tests/
│   ├── README.md
│   ├── frontend/
│   │   └── .gitkeep
│   └── backend/
│       └── .gitkeep
└── scripts/
    ├── README.md
    └── .gitkeep
```

## ドキュメント

- `docs/01_requirements.md`: MVP要件定義の初期版
- `docs/02_screen_design.md`: 画面設計の初期メモ
- `docs/03_data_model.md`: データモデル初期案
- `docs/04_authentication_design.md`: パスキー認証設計の初期メモ
- `docs/05_realtime_chat_design.md`: リアルタイムチャット設計の初期メモ
- `docs/06_admin_design.md`: 管理者機能設計の初期メモ
- `docs/07_mvp_development_plan.md`: MVP開発計画
- `docs/README.md`: 要件定義・設計文書の置き場に関する説明
- `AGENTS.md`: AIエージェントがこのリポジトリで作業する際の方針

## 初期方針

- closed な小規模コミュニティを前提にする
- 高齢者にとって読みやすく、誤操作しにくい体験を優先する
- 実名、連絡先、会話内容などの個人情報を慎重に扱う
- 生成AIの回答を無条件に信用させる設計を避ける
- 参加者の学習、安心感、疑問の変化を観察できるようにする
- 実在の同期生情報、個人名、連絡先、会話内容、属性情報をリポジトリに入れない

## 実装について

Phase 3 では、ローカル検証用に以下を実装しています。

- 仮登録フォームと `POST /api/registrations`
- メールアドレスによる状態確認と `GET /api/registrations/status`
- 開発環境限定の管理者承認画面
- `Development` 環境限定の管理者 API
- `PendingApproval`、`PasskeyRegistrationPending`、`Active`、`Rejected` の状態遷移
- パスキー登録画面と WebAuthn 登録 API
- パスキーログイン画面と WebAuthn ログイン API
- HttpOnly Cookie セッション、ログイン状態確認、ログアウト

現時点で実装していない範囲:

- チャット投稿、閲覧、反応、お知らせ
- データベース作成、接続文字列、実データ投入
- 本番向け管理者認証、初期管理者作成手順
- 本番デプロイ設定

後続 Phase に入る前に、以下を決めます。

- Passkey / WebAuthn を前提にした参加者登録・招待方式
- チャット履歴と個人情報の保存方針
- 生成AI連携の有無と利用目的
- 運用者・管理者の責任範囲
- さくらインターネット上での配置方式、運用、バックアップ方針

## 注意事項

- 実在の高校名、同期生名、メールアドレス、クラス名、旧姓、居住地などの個人情報は書かない
- このリポジトリ内のサンプルデータ、テストデータ、ドキュメントにも個人情報を含めない
- パスワード認証は採用しない
- MVP範囲外の機能を実装前に追加しない
- 医療、法律、金融などの専門助言サービスとして扱わない

## ライセンス

ライセンスは未定です。
