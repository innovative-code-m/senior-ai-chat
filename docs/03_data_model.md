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
| Status | ユーザー状態 | Pending、Approved、Active、Suspended、PasskeyResetAllowed など |
| Role | 権限 | Member、Admin |
| ApprovedAt | 承認日時 | 未承認の場合は空 |
| ApprovedByUserId | 承認した管理者ID | 管理者操作の追跡用 |
| CreatedAt | 作成日時 | 監査用 |
| UpdatedAt | 更新日時 | 監査用 |

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
| Category | 投稿カテゴリ | 質問、使い方共有、体験談、注意喚起、雑談など |
| Body | 投稿本文 | 個人情報が含まれる可能性があるため注意 |
| IsDeleted | 削除済みフラグ | 管理者削除時に使用 |
| DeletedAt | 削除日時 | 未削除の場合は空 |
| DeletedByUserId | 削除した管理者ID | 管理者操作の追跡用 |
| CreatedAt | 投稿日時 | 表示と監査用 |
| UpdatedAt | 更新日時 | 編集機能を持つ場合に利用 |

## Reactions

メッセージへの反応を管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | 反応ID | 内部ID |
| MessageId | メッセージID | `Messages.Id` を参照 |
| UserId | 反応したユーザーID | `Users.Id` を参照 |
| ReactionType | 反応種別 | 参考になった、同じ疑問、あとで試すなど |
| CreatedAt | 作成日時 | 表示と集計用 |

## Announcements

管理者からのお知らせを管理します。

| 項目名 | 概要 | 備考 |
| --- | --- | --- |
| Id | お知らせID | 内部ID |
| Title | タイトル | 個人情報を含めない |
| Body | 本文 | 個人情報を含めない |
| CreatedByUserId | 作成した管理者ID | `Users.Id` を参照 |
| PublishedAt | 公開日時 | 未公開管理を行う場合に利用 |
| CreatedAt | 作成日時 | 監査用 |
| UpdatedAt | 更新日時 | 監査用 |

## 個人情報に関する注意

- 氏名、メールアドレス、卒業時のクラス名は個人情報として扱う
- 投稿本文にも個人情報が含まれる可能性がある
- 一般メンバーにメールアドレスを表示しない
- テストデータでは実在しない値のみを使う
- リポジトリに実在する同期生情報を含めない
