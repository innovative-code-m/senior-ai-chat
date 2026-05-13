# frontend

React + TypeScript + Vite による Phase 3 の最小フロントエンドです。

この段階では、仮登録、メールアドレスによる状態確認、開発環境限定の管理者承認画面、パスキー登録画面、パスキーログイン画面、ログイン状態確認、ログアウトを用意しています。

## 前提

- Node.js 20.19 以降
- npm

## セットアップ

```powershell
cd src/frontend
npm install
```

## 起動

```powershell
npm run dev -- --host localhost
```

WebAuthn のローカル検証では RP ID を `localhost` としているため、まず `http://localhost:5173` で確認します。

バックエンドの URL を変える場合は、`.env.example` を参考に `.env` を作成し、`VITE_API_BASE_URL` を設定します。標準のバックエンド URL は `http://localhost:5086` です。

## ビルド

```powershell
npm run build
```

## Phase 3 の画面

- 仮登録
- 状態確認
- パスキー登録
- パスキーログイン
- ログイン状態確認
- ログアウト
- 開発用管理者承認

開発用管理者承認は `import.meta.env.DEV` のときだけ表示します。バックエンド側の管理者 API も `Development` 環境限定です。

## WebAuthn の注意

- パスワード入力欄はありません
- パスキー登録とログインには、WebAuthn 対応ブラウザと localhost または HTTPS の安全なコンテキストが必要です
- フロントエンドとバックエンドが別オリジンのため、ログイン完了、ログイン状態確認、ログアウトでは Cookie 送信用に `credentials: 'include'` を使います
- `http://127.0.0.1:5173` でも CORS は許可していますが、パスキー検証では `http://localhost:5173` を優先します

## Phase 3 の範囲外

- チャット投稿、閲覧、反応、お知らせ
- メンバー一覧
- データベース接続
- 本番向け管理者認証
- 画像投稿、ファイル添付、個別DM、通知、AI自動要約
