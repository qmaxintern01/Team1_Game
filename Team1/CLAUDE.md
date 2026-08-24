# CLAUDE.md

このファイルは、このUnityプロジェクトで開発・レビュー・自動化を行う際の共通方針です。プロジェクト固有の決定事項が増えた場合は、ここを更新してください。

## 概要

- エンジン: Unity 6 (`6000.5.8f1`)
- レンダリング: Universal Render Pipeline (URP 17.6.0)
- 入力: Unity Input System (1.20.0)、`Assets/Settings/InputSystem_Actions.inputactions` を使用
- テスト: Unity Test Framework (1.7.0)
- UI: `com.unity.ugui` を導入済み。UI Toolkit関連モジュールも組み込み済みだが、採用方針は[未決定事項](#未決定事項開発開始時に記入)を参照
- 主な対象: 2Dゲームを想定した初期プロジェクト。2D関連パッケージ（2D Animation、2D Aseprite、2D PSD Importer、2D SpriteShape、2D Tilemap / Extras）を導入済み
- そのほか導入済みパッケージ: Timeline、Visual Scripting、Collab Proxy
- Unity HubまたはUnity Editorでプロジェクトを開き、エディター上でシーン、アセット、設定を確認する。
- `Library/`、`Temp/`、`Logs/`、`UserSettings/` などのUnity生成物は、原則として編集・コミットしない。ルート直下に `.gitignore`（Unity公式テンプレート準拠）を追加済み。既に生成物がgit管理下にある場合は `git rm -r --cached` で追跡を外すこと。
- Unityのバージョン、パッケージ、プロジェクト設定を変更した場合は、チームに共有し、この文書にも必要に応じて反映する。

### 開発時の基本方針

1. 変更前に関連するシーン、Prefab、ScriptableObject、入力アクションの依存関係を確認する。
2. 1つの変更の目的を小さく保ち、動作確認できる単位でコミットする。
3. 目視確認だけでなく、可能な範囲でEdit Mode / Play Modeテストを追加する。
4. 変更後はConsoleのエラーと警告を確認し、対象シーンを実際に再生する。
5. アセットの移動・改名では、Unity Editor上で参照切れがないことを確認する。

## フォルダ構成

### 現状（2026-08-24時点）

初期状態のため、実フォルダはまだ最小構成。

```text
Assets/
├── Art/                    # 空（未使用）
├── Scenes/
│   └── SampleScene.unity   # Unity標準テンプレートのサンプルシーン
├── Scripts/
│   └── Stage.cs            # namespace未設定・空実装のMonoBehaviour
└── Settings/                # URP・Input System・シーンテンプレート関連の生成物
```

- `Scripts/` はまだ `Runtime/Editor/Tests` に分割されておらず、Assembly Definition（`.asmdef`）も未作成（デフォルトの `Assembly-CSharp` を使用）。
- `Audio/`、`Data/`、`Materials/`、`Prefabs/`、`Shaders/`、`StreamingAssets/`、`UI/` は未作成。

機能追加が進むにつれて、以下の目標構成へ段階的に移行する。

### 目標構成

```text
Assets/
├── Art/                    # 画像、スプライト、アニメーション、タイル、フォント
│   ├── Characters/
│   ├── Environments/
│   ├── UI/
│   └── VFX/
├── Audio/                  # BGM、SE、Audio Mixer
│   ├── BGM/
│   ├── SFX/
│   └── Mixers/
├── Data/                   # ScriptableObjectなどのゲームデータ
├── Materials/              # マテリアル、Shader Graph
├── Prefabs/                # 再利用するGameObjectのPrefab
│   ├── Characters/
│   ├── Gameplay/
│   ├── UI/
│   └── Systems/
├── Scenes/                 # シーン。用途・画面単位で整理
│   ├── Bootstrap/           # 初期化・共通システム
│   ├── Gameplay/
│   └── UI/
├── Scripts/                # C#コード。asmdef単位で依存を管理
│   ├── Runtime/
│   │   ├── Core/            # 共通基盤、ライフサイクル、サービス
│   │   ├── Gameplay/        # ゲームプレイのルール
│   │   ├── UI/              # 画面・表示・UI入力
│   │   └── Infrastructure/  # セーブ、ロード、外部連携
│   ├── Editor/              # Unity Editor専用コード
│   └── Tests/
│       ├── EditMode/
│       └── PlayMode/
├── Settings/               # Input Actions、Volume Profileなどの設定
├── Shaders/                # Shader Graph以外のシェーダー
├── StreamingAssets/        # 実行時にそのまま読み込む必要があるファイル
└── UI/                     # UI ToolkitのUXML、USS、UI共通素材（採用時）
```

### 配置ルール

- 新しいアセットは、種類だけでなく用途が分かる場所に置く。迷った場合は機能単位のサブフォルダを作る。
- 共有Prefab・共有Material・共有ScriptableObjectは、特定シーンの配下に置かない。
- シーン固有のアセットは、そのシーンまたは機能の近くに置き、再利用可能なものと区別する。
- `Resources/` は暗黙的な依存とメモリ管理上の問題を避けるため、原則使用しない。必要なら理由を記録する。
- 大量アセットの実行時ロードには、用途に応じてAddressablesの採用を検討する。
- 各フォルダ内で命名規則を統一し、Unityが生成する `.meta` ファイルは必ずアセットと一緒に扱う。

## コーディングルール

### C#の基本

- C#の命名はMicrosoftの一般的な規約に従う。
  - `PascalCase`: namespace、class、struct、interface、enum、public member、method
  - `camelCase`: local variable、parameter
  - private field: `camelCase` または既存コードに合わせた `_camelCase`。プロジェクト全体で統一する
  - interface: `I` プレフィックス（例: `ISaveService`）
  - asyncメソッド: `Async` サフィックス
- 1ファイル1主要型を基本とし、ファイル名は主要型名と一致させる。
- namespaceを使用し、フォルダ構成と概ね対応させる。グローバルnamespaceを増やさない（現状 `Assets/Scripts/Stage.cs` などnamespace未設定のファイルがあるため、今後の追加・修正時に順次揃える）。
- `var` は型が右辺から明確な場合に限って使用し、公開APIの型は省略しない。
- nullable参照型を導入する場合は、プロジェクト全体で方針を決めてから有効化する。
- マジックナンバーや文字列リテラルを処理ロジックに散在させず、定数、設定値、またはScriptableObjectに集約する。
- `public` フィールドで状態を公開せず、原則として `[SerializeField] private` とプロパティまたはメソッドを使う。
- `MonoBehaviour` はUnityライフサイクルと外部連携に寄せ、ゲームルールは通常のC#クラスに分離する。
- コメントは日本語で記述してよい。ただし「WHYが非自明な箇所」に限定し、コードを読めば分かることは書かない。

### Unity固有のルール

- `Awake` は自身の初期化、`OnEnable` / `OnDisable` は購読の開始・解除、`Start` は依存先が初期化された後の処理に使う。
- `Update` / `FixedUpdate` / `LateUpdate` は必要なコンポーネントだけで使い、毎フレームの処理量を意識する。
- Inspectorで設定する参照は `[SerializeField] private` にし、`Awake` などで必須参照を検証する。
- `GetComponent`、`Find`、`FindObjectOfType`、タグ検索を毎フレーム実行しない。必要なら初期化時にキャッシュする。
- イベントを購読したら、所有期間が終わる箇所で必ず解除する。staticイベントやSingletonの利用は依存関係を明示する。
- Inspector上の設定を壊す可能性があるフィールド名変更には、`FormerlySerializedAs` の使用を検討する。
- Coroutine、async処理、Tweenなどは、破棄済みオブジェクトへのアクセスとキャンセルを考慮する。
- 入力は `InputAction` / `PlayerInput` 経由で扱い、旧Input Manager APIと混在させない。
- 物理処理は `FixedUpdate`、表示やカメラ追従は必要に応じて `LateUpdate` に置く。
- `Debug.Log` を製品コードに残す場合は、ログレベルや条件付きコンパイルなどの方針に従う。

### 設計・アセット

- シーン間で共有する状態は、シーン上の参照に依存せず、明示的なサービスまたはScriptableObjectで管理する。
- ScriptableObjectは設定・定義データに使い、実行中の一時状態を不用意にアセットへ書き戻さない。
- Prefabの変更は意図したPrefabアセットに対して行い、意図しないOverridesを残さない。
- UIは、既存のUI方式（uGUIまたはUI Toolkit）を機能ごとに混在させず、採用方針に従う。
- 依存方向は `Core` が具体的なGameplayやUIを参照しない形を基本とする。循環依存はasmdefで早期に検出する。

## テストと確認

- 純粋な計算・状態遷移・ルールはEdit Modeテストを優先する。
- シーン、Prefab、入力、物理、アニメーションを含む動作はPlay Modeテストまたは手動確認で検証する。
- バグ修正では、再発条件を表すテストを可能な範囲で追加する。
- 最低限、変更対象のシーンを再生し、Consoleに新しいエラーがないことを確認する。
- ビルド対象プラットフォームと品質設定を変更した場合は、対象プラットフォーム向けのビルド確認を行う。

## Gitとレビュー

- `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`obj/`、`Build/` などの生成物をコミットしない。
- `.meta` ファイルを単独で削除・無視せず、対応するアセットと一緒に管理する。
- シーン、Prefab、Project Settingsを変更したコミットでは、変更理由と確認方法を記載する。
- 大きなバイナリアセットは、リポジトリの容量と履歴を確認してから追加する。
- 変更前後で参照切れ、重複Prefab、不要なAssetDatabase変更がないか確認する。

## 未決定事項（開発開始時に記入）

- [ ] プロジェクト名とゲームの概要（現状 `ProjectSettings` 上は `productName: Team1` / `companyName: DefaultCompany` のプレースホルダーのまま）
- [ ] 対象プラットフォームと最低動作環境
- [ ] 画面解像度、アスペクト比、入力デバイス（`InputSystem_Actions.inputactions` は導入済みだが、アクション内容は未編集）
- [ ] uGUI / UI Toolkitの採用方針（`com.unity.ugui` パッケージは導入済みだが、正式採用の意思決定はまだ）
- [ ] シーン遷移とBootstrapの方式（現状シーンは `SampleScene` のみでBootstrapシーン未作成）
- [ ] Addressables、Localization、セーブ方式の要否（現状Addressablesパッケージは未導入）
- [ ] アセンブリ定義（asmdef）の分割方針（現状 `.asmdef` は1つも作成されておらず、デフォルトの `Assembly-CSharp` を使用）
- [ ] コードフォーマッター、静的解析、CIの利用方針（現状 `.editorconfig` やCI設定は未整備）
- [ ] ブランチ、コミットメッセージ、レビュー運用
- [x] `.gitignore` の追加（Unity公式テンプレート準拠で追加済み。既存の追跡済み生成物があれば `git rm -r --cached` での対応が別途必要）
