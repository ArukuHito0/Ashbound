# 構成

Ashbound.Domain は Unity 非依存の戦闘状態を扱う。Ashbound.Infrastructure は JSONの読み込みと参照検証を担当する。Editor はマスター読込の実行入口とする。Application のゲーム進行、Presentation の uGUI と TextMeshPro、素材参照用 ScriptableObject は必要な機能を移植する段階で追加する。
