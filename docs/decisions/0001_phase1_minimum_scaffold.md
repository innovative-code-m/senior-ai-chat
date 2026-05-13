# 0001 Phase 1 最小雛形の構成

## 決定日

2026-05-14

## 決定

Phase 1 では、以下の最小構成を採用します。

- フロントエンド: `src/frontend/` に React + TypeScript + Vite
- バックエンド: `src/backend/SeniorAiChat.Api/` に ASP.NET Core Web API
- バックエンドの確認用 API: `GET /health`
- SignalR の接続口: `/hubs/chat`

## 理由

- 既存ドキュメントの想定技術構成に沿う
- 本格的な業務機能を入れず、ローカル起動確認に集中できる
- 後続 Phase で仮登録、パスキー認証、チャット機能を段階的に追加できる

## Phase 1 で行わないこと

- パスワード認証の追加
- Passkey / WebAuthn の本実装
- データベース作成、接続文字列、実データ投入
- 仮登録、管理者承認、チャット投稿などの業務機能実装
- MVP範囲外の画像投稿、ファイル添付、個別DM、通知、AI自動要約
