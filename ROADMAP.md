# ROADMAP

このファイルは、`senior-ai-chat` のフェーズ状態を管理する一次記録です。

詳細な設計や検証内容は `docs/` と `log/working/` に記録し、このファイルでは各 Phase の状態、残作業、次に着手できる範囲を管理します。

## 状態の種類

| 状態 | 意味 |
| --- | --- |
| `完了` | 完了条件を満たし、主要な検証も完了している |
| `完了扱い` | 目的は満たしているが、環境都合などで一部検証や後続確認が残っている |
| `進行中` | 現在作業中 |
| `未着手` | まだ実装または検証を開始していない |
| `保留` | 前提条件が未確定で着手しない |

## 現在の状態

| Phase | 状態 | 概要 | 主な記録 |
| --- | --- | --- | --- |
| Phase 0 | 完了 | README、AGENTS、docs、初期ディレクトリを整備した | `log/working/2026-05-14_01_initialize-project-documentation.md` |
| Phase 1 | 完了扱い | React + TypeScript と ASP.NET Core Web API の最小雛形を作成した | `docs/decisions/0001_phase1_minimum_scaffold.md`、`log/working/2026-05-14_02_phase1-minimum-scaffold.md` |
| Phase 2 | 完了扱い | 仮登録、状態確認、Development 環境限定の管理者承認 API / 画面を追加した | `docs/decisions/0003_phase2_registration_admin_approval.md`、`log/working/2026-05-14_03_phase2-registration-admin-approval.md` |
| Phase 3 | 未着手 | Passkey / WebAuthn 登録とログイン | `docs/04_authentication_design.md` |
| Phase 4 | 未着手 | チャット投稿・閲覧 | `docs/05_realtime_chat_design.md` |
| Phase 5 | 未着手 | 投稿カテゴリと反応ボタン | `docs/01_requirements.md`、`docs/03_data_model.md` |
| Phase 6 | 未着手 | メンバー一覧と管理者機能 | `docs/06_admin_design.md` |
| Phase 7 | 未着手 | 小規模試験運用 | `log/research/README.md` |

## 残っている確認事項

### Phase 1

- Node.js / npm が利用できる環境で、フロントエンドの `npm install`、`npm run build`、`npm run dev` を確認する。

### Phase 2

- Node.js / npm が利用できる環境で、フロントエンドの依存関係確認、ビルド、ローカル起動を確認する。
- `tests/` 配下に実行可能なテストプロジェクトを追加する場合は、Phase 2 の API 状態遷移を自動テスト化する。
- Phase 2 のインメモリストアはローカル検証用であり、データベース永続化は後続 Phase で扱う。

### Phase 3 着手前

- Passkey / WebAuthn 登録フローの詳細を `docs/04_authentication_design.md` に反映する。
- 初期管理者の作成手順、管理者ログイン、管理者セッションの扱いを決める。
- Phase 2 の無認証管理者機能を本番運用可能な形で残さないことを再確認する。

## 更新ルール

- Phase の状態を変更する場合は、この `ROADMAP.md` を更新する。
- 実装や検証を行った場合は、`log/working/` に作業ログを残す。
- 設計判断が変わる場合は、関連する `docs/` または `docs/decisions/` を更新する。
- Phase 3 以降に進む場合は、先に MVP 範囲と個人情報方針に反しないことを確認する。
- 実在する参加者の個人情報は、このファイルにもログにも含めない。
