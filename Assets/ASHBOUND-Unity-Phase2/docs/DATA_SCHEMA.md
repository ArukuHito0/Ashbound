# データ形式

Reference/master.json に005の抽出物をそのまま収録。sourceCommit と balanceRevision で出所を識別する。cards[].id は定義ID。戦闘個体IDは実行時に発行し、定義IDと別に保持する。敵は ACT7 ではキー単体、他ACTでは「ACT番号:キー」。遭遇の @ 以降は配置等の指定として保持する。マスターに収まっていない動的行動と編成は現行ソースから別途移植する。
