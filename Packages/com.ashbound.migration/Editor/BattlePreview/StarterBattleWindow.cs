using System;
using Ashbound.BattleScene;
using System.IO;
using Ashbound.Domain;
using UnityEditor;
using UnityEngine;

namespace Ashbound.Editor
{
    internal sealed class StarterBattleWindow : EditorWindow
    {
        private readonly StarterBattleSetup setup = new StarterBattleSetup();

        private StarterBattle battle;
        private string error;
        private Vector2 scroll;

        [MenuItem("ASHBOUND/戦闘プレビュー")]
        private static void Open()
        {
            var window = GetWindow<StarterBattleWindow>("ASHBOUND 戦闘");
            window.minSize = new Vector2(360, 420);
        }

        private void OnEnable() => StartBattle();

        private void StartBattle()
        {
            battle = null;
            error = null;
            try
            {
                var master = AssetDatabase.LoadAssetAtPath<TextAsset>(StarterBattleSetup.MasterPath);
                if (master == null)
                    throw new InvalidDataException("戦闘マスターを読み込めません。");

                battle = setup.Create(master.text, Environment.TickCount);
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }
        }

        private void OnGUI()
        {
            if (battle == null)
            {
                EditorGUILayout.HelpBox(error ?? "戦闘を読み込んでいます。", MessageType.Error);
                if (GUILayout.Button("再読み込み"))
                    StartBattle();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField($"ターン {battle.Turn}", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(setup.EnemyName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"HP {battle.EnemyHp}/{setup.EnemyMaxHp}　ブロック {battle.EnemyBlock}");
                if (battle.Result == StarterBattle.Outcome.Playing)
                    EditorGUILayout.LabelField($"次の行動　{(battle.EnemyWillAttack ? "攻撃" : "ブロック")} {battle.EnemyIntentValue}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"HP {battle.PlayerHp}/{StarterBattle.MaxPlayerHp}　ブロック {battle.PlayerBlock}");
            EditorGUILayout.LabelField($"エネルギー {battle.Energy}/{StarterBattle.EnergyPerTurn}　山札 {battle.DrawCount}　捨て札 {battle.DiscardCount}");
            EditorGUILayout.Space();

            if (battle.Result == StarterBattle.Outcome.Playing)
                DrawHand();
            else
                EditorGUILayout.HelpBox(battle.Result == StarterBattle.Outcome.Won ? "勝利" : "敗北", MessageType.Info);

            EditorGUILayout.EndScrollView();
            if (battle.Result == StarterBattle.Outcome.Playing)
            {
                if (GUILayout.Button("ターン終了", GUILayout.Height(30)))
                {
                    battle.EndTurn();
                    GUIUtility.ExitGUI();
                }
            }
            else if (GUILayout.Button("もう一度戦う", GUILayout.Height(30)))
            {
                StartBattle();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawHand()
        {
            EditorGUILayout.LabelField("手札", EditorStyles.boldLabel);
            for (var index = 0; index < battle.Hand.Count; index++)
            {
                var card = battle.Hand[index];
                var effect = card.Damage > 0 ? $"{card.Damage} ダメージ" : $"{card.Block} ブロック";
                using (new EditorGUI.DisabledScope(!battle.CanPlay(index)))
                {
                    if (GUILayout.Button($"{card.Name}　{card.Cost} エネルギー　{effect}", GUILayout.Height(30)))
                    {
                        battle.TryPlay(index);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }
}


