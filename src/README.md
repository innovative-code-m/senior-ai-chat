# src

このディレクトリは、アプリケーション本体の実装コードを置く場所です。

Phase 1 では、ローカル起動確認のための最小雛形だけを作成しています。仮登録、管理者承認、パスキー登録、チャット投稿などの業務機能は後続 Phase で実装します。

## ディレクトリ

- `frontend/`: React + TypeScript + Vite の最小フロントエンド
- `backend/`: ASP.NET Core Web API + SignalR 接続口の最小バックエンド

## ローカル起動

### フロントエンド

```powershell
cd src/frontend
npm install
npm run dev
```

標準では `http://127.0.0.1:5173` で起動します。

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
- Phase 1 ではデータベース、認証本実装、チャット本実装を行わない
