# ASHBOUND Unity 移行基盤

Unity 6 の2Dプロジェクトに追加するパッケージです。共通マスターの読込・検証と、戦闘中のカード個体の最小モデルを含みます。

1. Unity Hub で作成したプロジェクトの Packages フォルダーへ com.ashbound.migration をコピーします。
2. Unity を開き、メニュー ASHBOUND → Validate master JSON でパッケージ内 Reference/master.json を指定します。期待する件数はカード236、敵74、通常遭遇63です。
3. Unity Test Framework を導入し、Packages/manifest.json のトップレベルに "testables": ["com.ashbound.migration"] を追加します。既存の配列があれば要素を追加します。Test Runner の EditMode から Ashbound.Tests.Editor を実行します。

移行基準は本編 v181、調整エディター v25、調整リビジョン 72bbe94b-22c9-4d7a-a698-79fd3f177240。保存アーカイブの実測照合は保留中です。Unity でのインポート、C# コンパイル、EditMode テストは未実行です。進捗と未実装範囲は docs を参照してください。
