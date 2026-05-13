# backend

ASP.NET Core Web API による Phase 1 の最小バックエンドです。

この段階では、起動確認用の `/health` と、将来の SignalR 利用を見据えた `/hubs/chat` の接続口だけを用意しています。認証、データベース、ユーザー管理、チャット投稿はまだ実装していません。

## 前提

- .NET SDK 9

## restore / build

```powershell
cd src/backend/SeniorAiChat.Api
dotnet restore
dotnet build
```

## 起動

```powershell
cd src/backend/SeniorAiChat.Api
dotnet run
```

標準では `http://localhost:5086` で起動します。

## 起動確認

```powershell
Invoke-RestMethod http://localhost:5086/health
```

`status` が `ok` の JSON が返れば、Phase 1 のバックエンド起動確認は完了です。

## Phase 1 の範囲外

- パスキー / WebAuthn の本実装
- ユーザー登録、承認、ログイン
- データベース接続
- チャット投稿、反応、お知らせ、管理者操作
