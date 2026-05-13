# データモデル

この文書は、MVPのデータモデル初期案です。

実名、メールアドレス、卒業時のクラス名は個人情報として扱います。実在する同期生の情報を、サンプルデータ・テストデータ・ドキュメントに含めてはなりません。

## Users

ユーザー情報と承認状態を管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | ユーザーID | 内部ID |
| FullName | 氏名 | 個人情報。実データをリポジトリに含めない |
| Email | メールアドレス | 個人情報。ログインIDとして使用 |
| GraduationClassName | 卒業時のクラス名 | 個人情報に準じて扱う |
| Status | ユーザー状態 | `PendingApproval`、`PasskeyRegistrationPending`、`Active`、`Suspended`、`PasskeyResetAllowed`、`Rejected` |
| Role | 権限 | Member、Admin |
| ApprovedAt | 承認日時 | 未承認の場合は空 |
| ApprovedByUserId | 承認した管理者ID | 管理者操作の追跡用 |
| CreatedAt | 作成日時 | 監査用 |
| UpdatedAt | 更新日時 | 監査用 |

### Users.Status の値

| Status 値 | 対応する状態 | 備考 |
| --- | --- | --- |
| `PendingApproval` | 承認待ち | 仮登録が保存され、管理者確認を待つ状態 |
| `PasskeyRegistrationPending` | パスキー未登録 | 管理者承認済みで、パスキー登録を待つ状態 |
| `Active` | 有効 | チャット利用可能な状態 |
| `Suspended` | 停止 | 管理者により利用停止された状態 |
| `PasskeyResetAllowed` | 再登録許可中 | パスキー再登録を一時的に許可した状態 |
| `Rejected` | 否認 | 管理者が参加を許可しなかった状態 |

仮登録フォームの入力中はまだ `Users` に保存されないため、DB上の Status 値はありません。

`Approved` という単独の Status 値は使いません。承認済みでパスキー未登録の状態は `PasskeyRegistrationPending` で表します。

## UserPasskeys

ユーザーに紐づくパスキー情報を管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | パスキーID | 内部ID |
| UserId | ユーザーID | `Users.Id` を参照 |
| CredentialId | WebAuthn資格情報ID | 秘密鍵そのものは保存しない |
| PublicKey | 公開鍵 | WebAuthn検証用 |
| SignCount | 署名カウンタ | リプレイ検知に利用 |
| DeviceName | 端末名または識別名 | 任意。個人情報になり得るため注意 |
| LastUsedAt | 最終利用日時 | セキュリティ確認用 |
| CreatedAt | 作成日時 | 監査用 |
| RevokedAt | 無効化日時 | 端末紛失時など |

## Messages

チャット投稿を管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | メッセージID | 内部ID |
| UserId | 投稿者ID | `Users.Id` を参照 |
| Category | 投稿カテゴリ | 固定文字列。`Question`、`HowToShare`、`Experience`、`Caution`、`Chat` |
| Body | 投稿本文 | 個人情報が含まれる可能性があるため注意 |
| IsDeleted | 削除済みフラグ | 管理者削除時に使用 |
| DeletedAt | 削除日時 | 未削除の場合は空 |
| DeletedByUserId | 削除した管理者ID | 管理者操作の追跡用 |
| CreatedAt | 投稿日時 | 表示と監査用 |
| UpdatedAt | 更新日時 | MVPでは投稿編集に使わない。管理者削除など状態変更の監査用 |

### Messages.Category の値

MVPではカテゴリをマスターテーブル化せず、固定文字列として保存します。

| Category 値 | 表示名 |
| --- | --- |
| `Question` | 質問 |
| `HowToShare` | 使い方共有 |
| `Experience` | 体験談 |
| `Caution` | 注意喚起 |
| `Chat` | 雑談 |

## Reactions

メッセージへの反応を管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | 反応ID | 内部ID |
| MessageId | メッセージID | `Messages.Id` を参照 |
| UserId | 反応したユーザーID | `Users.Id` を参照 |
| ReactionType | 反応種別 | 固定文字列。`Helpful`、`SameQuestion`、`TryLater` |
| CreatedAt | 作成日時 | 表示と集計用 |

### Reactions.ReactionType の値

MVPでは反応種別をマスターテーブル化せず、固定文字列として保存します。

| ReactionType 値 | 表示名 |
| --- | --- |
| `Helpful` | 参考になった |
| `SameQuestion` | 同じ疑問 |
| `TryLater` | あとで試す |

### Reactions の制約と取り消し

- `(MessageId, UserId, ReactionType)` にユニーク制約を設定する
- 同じ利用者が同じ投稿に同じ反応を重複登録することはできない
- 同じ反応ボタンをもう一度押した場合は取り消しとして扱う
- MVPでは取り消し履歴を保持せず、取り消し時は該当する `Reactions` 行を削除する

## Announcements

管理者からのお知らせを管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | お知らせID | 内部ID |
| Title | タイトル | 個人情報を含めない |
| Body | 本文 | 個人情報を含めない |
| CreatedByUserId | 作成した管理者ID | `Users.Id` を参照 |
| PublishedAt | 公開日時 | MVPでは作成時に即公開し、通常は `CreatedAt` と同じ値にする |
| CreatedAt | 作成日時 | 監査用 |
| UpdatedAt | 更新日時 | 監査用 |

MVPではお知らせの下書き、予約公開、未公開管理は扱いません。お知らせ作成時に公開済みとして保存し、`PublishedAt` は表示用の公開日時として使います。

## 個人情報に関する注意

- 氏名、メールアドレス、卒業時のクラス名は個人情報として扱う
- 投稿本文にも個人情報が含まれる可能性がある
- 一般メンバーにメールアドレスを表示しない
- テストデータでは実在しない値のみを使う
- リポジトリに実在する同期生情報を含めない
