# frontend

React + TypeScript + Vite による Phase 2 の最小フロントエンドです。

この段階では、仮登録、メールアドレスによる状態確認、開発環境限定の管理者承認画面を用意しています。

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
npm run dev
```

標準では `http://127.0.0.1:5173` で起動します。

バックエンドの URL を変える場合は、`.env.example` を参考に `.env` を作成し、`VITE_API_BASE_URL` を設定します。

## ビルド

```powershell
npm run build
```

## Phase 2 の画面

- 仮登録
- 状態確認
- 開発用管理者承認

開発用管理者承認は `import.meta.env.DEV` のときだけ表示します。バックエンド側の管理者 API も `Development` 環境限定です。

## Phase 2 の範囲外

- パスキー / WebAuthn の本実装
- パスキーログイン
- チャット投稿
- データベース接続
- 画像投稿、ファイル添付、個別DM、通知、AI自動要約
