# src

このディレクトリは、アプリケーション本体の実装コードを置く場所です。

Phase 3 では、仮登録、管理者承認、パスキー登録、パスキーログイン、Cookie セッションのローカル検証実装までを追加しています。チャット投稿、データベース接続、本番向け管理者認証は後続 Phase で実装します。

## ディレクトリ

- `frontend/`: React + TypeScript + Vite の Phase 3 最小フロントエンド
- `backend/`: ASP.NET Core Web API + SignalR 接続口 + Phase 3 認証 API

## ローカル起動

### フロントエンド

```powershell
cd src/frontend
npm install
npm run dev -- --host localhost
```

WebAuthn のローカル検証では、まず `http://localhost:5173` で起動します。

### バックエンド

```powershell
cd src/backend/SeniorAiChat.Api
dotnet restore
dotnet build
dotnet run
```

標準では `http://localhost:5086` で起動します。

起動確認:

```powershell
Invoke-RestMethod http://localhost:5086/health
```

## 注意事項

- 実装範囲を広げる前に関連する `docs/` を更新する
- パスワード認証を追加しない
- MVP範囲外の機能を勝手に追加しない
- 実在の個人情報をサンプルデータとして含めない
- Phase 3 ではデータベース、本番向け管理者認証、チャット本実装を行わない
