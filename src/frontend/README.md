# frontend

React + TypeScript + Vite による Phase 1 の最小フロントエンドです。

この段階では、起動確認用画面とバックエンド `/health` 確認だけを用意しています。仮登録、ログイン、チャット投稿、管理者機能はまだ実装していません。

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

## Phase 1 の範囲外

- パスキー / WebAuthn の本実装
- 仮登録、管理者承認、ログイン、チャット投稿
- データベース接続
- 画像投稿、ファイル添付、個別DM、通知、AI自動要約
