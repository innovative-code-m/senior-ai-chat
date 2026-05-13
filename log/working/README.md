# working ログのファイル名規則

`log/working/` には、作業ログや実装中の検討ログを Markdown 形式で保存します。

## ファイル名形式

```text
YYYY-MM-DD_NN_タイトル.md
```

例:

```text
2026-05-14_01_phase2-registration-admin-approval.md
```

## 命名ルール

- `YYYY-MM-DD` には、ログを作成した日付を入れます。
- `NN` には、同一日付内で `01` から始まる2桁の連番を入れます。
- `タイトル` には、ログ内容を表す短い表題を入れます。
- タイトルは小文字英数字とハイフン区切りを基本とします。
- 実在する参加者の氏名、メールアドレス、クラス名、旧姓、居住地などの個人情報は、ファイル名にも本文にも含めません。

## 連番の付け方

同じ日に複数のログを作成する場合は、既存ファイルの最大連番に 1 を足します。

例:

```text
2026-05-14_01_phase2-registration-admin-approval.md
2026-05-14_02_readme-filename-rule.md
2026-05-14_03_local-verification-notes.md
```

## タイトルの付け方

タイトルは、後から一覧したときに内容が分かる名前にします。

良い例:

```text
2026-05-14_01_phase2-registration-admin-approval.md
2026-05-14_02_admin-approval-test-notes.md
```

避ける例:

```text
2026-05-14_01_memo.md
2026-05-14_02_temp.md
2026-05-14_03_personal-name@example.com.md
```
