using System;
using System.IO;
using System.Collections.Generic;
using Ashbound.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ashbound.BattleScene
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class StarterBattleScreen : MonoBehaviour
    {
        [SerializeField] private TextAsset master;
        [SerializeField] private ThemeStyleSheet theme;

        private readonly StarterBattleSetup setup = new StarterBattleSetup();
        private static readonly string[] EncounterIds = {
            "1:ash+shade", "1:soldier+hunter", "1:soldier+priest", "1:hunter+priest",
            "1:ravager", "1:plated", "1:trio", "1:boss"
        };
        private static readonly string[] EncounterLabels = {
            "通常：灰かぶり・灰の影", "通常：重兵・狩人", "通常：重兵・司祭", "通常：狩人・司祭",
            "強敵：破壊者", "強敵：番人", "強敵：三連の徒", "ACT1 ボス"
        };

        private static readonly string[] ExplorerIds = { "wanderer", "hunter", "lucky" };
        private static readonly string[] ExplorerLabels = { "灰の放浪者", "獣の狩人", "ラッキー・ガール" };
        private int explorer;
        private int encounter;
        private StarterBattle battle;
        private int target;
        private PanelSettings panel;
        private Font font;
        private VisualElement content;

        private void Start()
        {
            var document = GetComponent<UIDocument>();
            if (document.panelSettings == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.themeStyleSheet = theme;
                document.panelSettings = panel;
            }

            var root = document.rootVisualElement;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = (Color)new Color32(21, 19, 20, 255);
            root.style.color = (Color)new Color32(242, 230, 214, 255);
            root.style.alignItems = Align.Center;
            if (Array.Exists(Font.GetOSInstalledFontNames(), name => name == "Yu Gothic" || name == "Meiryo"))
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo" }, 18);
                root.style.unityFontDefinition = FontDefinition.FromFont(font);
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.width = new StyleLength(Length.Percent(100));
            scroll.style.maxWidth = 760;
            scroll.style.flexGrow = 1;
            root.Add(scroll);

            content = new VisualElement();
            content.style.paddingTop = 24;
            content.style.paddingLeft = 24;
            content.style.paddingRight = 24;
            content.style.paddingBottom = 24;
            scroll.Add(content);
            var explorerPicker = new DropdownField("探索者", new List<string>(ExplorerLabels), explorer);
            root.Insert(0, explorerPicker);
            explorerPicker.RegisterValueChangedCallback(change =>
            {
                explorer = Array.IndexOf(ExplorerLabels, change.newValue);
                Restart();
            });
            var picker = new DropdownField("試遊する遭遇", new List<string>(EncounterLabels), encounter);
            picker.style.marginBottom = 12;
            root.Insert(0, picker);
            picker.RegisterValueChangedCallback(change =>
            {
                encounter = Array.IndexOf(EncounterLabels, change.newValue);
                Restart();
            });
            Restart();
        }

        private void Restart()
        {
            try
            {
                if (master == null)
                    throw new InvalidDataException("戦闘マスターが設定されていません。");
                battle = setup.CreateEncounter(master.text, Environment.TickCount, EncounterIds[encounter], ExplorerIds[explorer]);
                target = 0;
                Render();
            }
            catch (Exception exception)
            {
                content.Clear();
                AddLabel(exception.Message, 18);
                Debug.LogException(exception, this);
            }
        }

        private void Render()
        {
            content.Clear();
            AddLabel($"ターン {battle.Turn}", 24);
            for (var index = 0; index < battle.Enemies.Count; index++)
            {
                var enemyIndex = index;
                var enemy = battle.Enemies[index];
                var status = enemy.Hp == 0 ? "撃破" : $"HP {enemy.Hp}/{enemy.MaxHp}   ブロック {enemy.Block}   毒 {enemy.Poison}   裂傷 {enemy.Laceration}   脆弱 {enemy.Vulnerable}";
                if (enemy.Hp == 0 || battle.Result != StarterBattle.Outcome.Playing)
                {
                    AddLabel($"{enemy.Name}   {status}", 18);
                    continue;
                }

                var move = battle.GetEnemyIntent(index);
                var intent = move.Hits == 0 ? "行動不能" : move.IsAttack ? $"攻撃 {move.Amount}" : $"ブロック {move.Amount}";
                if (move.Hits > 1)
                    intent += $" × {move.Hits}";
                if (move.Strength > 0)
                    intent += $"・筋力 +{move.Strength}";
                AddButton($"{(index == target ? "▶ " : "")}{enemy.Name}   {status}   次の行動: {intent}", () =>
                {
                    target = enemyIndex;
                    Render();
                });
            }

            AddLabel($"プレイヤー   HP {battle.PlayerHp}/{StarterBattle.MaxPlayerHp}   ブロック {battle.PlayerBlock}", 18);
            AddLabel($"エネルギー {battle.Energy}/{StarterBattle.EnergyPerTurn}   山札 {battle.DrawCount}   捨て札 {battle.DiscardCount}   除外 {battle.ExhaustCount}", 16);

            if (battle.Result != StarterBattle.Outcome.Playing)
            {
                AddLabel(battle.Result == StarterBattle.Outcome.Won ? "勝利" : "敗北", 26);
                AddButton("もう一度戦う", Restart);
                return;
            }

            if (explorer == 1)
                AddLabel($"進化回数 {battle.EvolutionCount}", 16);
            if (explorer == 2)
            {
                AddLabel($"ポーカー  {string.Join("・", battle.Poker)}", 16);
                if (battle.LastPoker.Count > 0)
                    AddLabel($"前回  {string.Join("・", battle.LastPoker)}  {(battle.LastRoles.Count > 0 ? string.Join(" / ", battle.LastRoles) : "役なし")}", 16);
                if (battle.SlotResult != null)
                    AddLabel(battle.SlotResult, 16);
            }

            if (battle.RoleChoices.Count > 0)
            {
                AddLabel("成立した役から1つ選択", 18);
                AddChoices(battle.RoleChoices, battle.TryChooseRole);
                return;
            }

            if (battle.RecoveryChoices.Count > 0)
            {
                AddLabel($"{battle.RecoverySource}から手札に加えるカードを選択", 18);
                AddChoices(battle.RecoveryChoices, battle.TryRecover);
                return;
            }

            if (battle.EvolutionChoices.Count > 0)
            {
                AddLabel("進化させるカードを選択", 18);
                AddChoices(battle.EvolutionChoices, battle.TryEvolve);
                AddButton("選択を終了", () => { battle.SkipEvolution(); Render(); });
                return;
            }

            var choosingDiscard = battle.PendingDiscard > 0;
            AddLabel(choosingDiscard ? $"捨てるカードを選択（残り {battle.PendingDiscard} 枚）" : "手札", 18);
            for (var index = 0; index < battle.Hand.Count; index++)
            {
                var handIndex = index;
                var card = battle.Hand[index];
                var effects = new List<string>();
                if (card.Damage > 0)
                    effects.Add(card.Hits > 1 ? $"{card.Damage} ダメージ × {card.Hits}" : $"{card.Damage} ダメージ");
                if (card.Block > 0)
                    effects.Add($"{card.Block} ブロック");
                if (card.Heal > 0)
                    effects.Add($"HP {card.Heal} 回復");
                if (card.Draw > 0)
                    effects.Add($"{card.Draw} 枚ドロー");
                if (card.Discard > 0)
                    effects.Add($"手札を {card.Discard} 枚捨てる");
                if (card.Poison > 0)
                    effects.Add($"毒 {card.Poison}");
                if (card.Exhaust)
                    effects.Add("除外");
                if (card.Laceration > 0)
                    effects.Add($"裂傷 {card.Laceration}");
                if (card.Vulnerable > 0)
                    effects.Add($"脆弱 {card.Vulnerable}");
                if (card.Slot > 0)
                    effects.Insert(0, $"スロット {card.Slot}");
                if (card.EvolvesInto != null)
                    effects.Add($"使用後に {card.EvolvesInto.Name} へ進化");
                if (card.AllEnemies)
                    effects.Add("敵全体");
                if (card.XBleed > 0)
                    effects.Add($"裂傷 X+{card.XBleed}");
                if (card.EvolveCount > 0)
                    effects.Add($"最大 {card.EvolveCount} 枚を {card.EvolveStages} 段階進化");
                if (card.Role == 2 || card.Role == 5)
                    effects.Add("効果の移行待ち");
                var effect = string.Join("・", effects);
                var button = AddButton($"{card.Name}   {(card.XCost ? "X" : card.Cost.ToString())} エネルギー   {effect}", () =>
                {
                    if (choosingDiscard ? battle.TryDiscard(handIndex) : battle.TryPlay(handIndex, target))
                    {
                        SelectLivingTarget();
                        Render();
                    }
                });
                var enabled = choosingDiscard || battle.CanPlay(index, target);
                if (!enabled)
                    button.style.opacity = 0.45f;
                button.SetEnabled(enabled);
            }
            AddButton("ターン終了", () =>
            {
                battle.EndTurn();
                Render();
            }).SetEnabled(!choosingDiscard);
        }

        private void SelectLivingTarget()
        {
            if (battle.Enemies[target].Hp > 0)
                return;

            for (var index = 0; index < battle.Enemies.Count; index++)
            {
                if (battle.Enemies[index].Hp > 0)
                {
                    target = index;
                    return;
                }
            }
        }

        private void AddChoices(IReadOnlyList<StarterBattle.Card> cards, Func<int, bool> choose)
        {
            for (var index = 0; index < cards.Count; index++)
            {
                var choice = index;
                AddButton(cards[index].Name, () =>
                {
                    if (choose(choice))
                        Render();
                });
            }
        }

        private void AddLabel(string text, int fontSize)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8;
            content.Add(label);
        }

        private Button AddButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.minHeight = 40;
            button.style.marginBottom = 8;
            button.style.backgroundColor = (Color)new Color32(61, 43, 36, 255);
            button.style.color = (Color)new Color32(255, 239, 222, 255);
            button.style.fontSize = 16;
            button.style.whiteSpace = WhiteSpace.Normal;
            content.Add(button);
            return button;
        }

        private void OnDestroy()
        {
            if (panel != null)
                Destroy(panel);
            if (font != null)
                Destroy(font);
        }
    }
}














