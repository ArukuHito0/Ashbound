using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashbound.Domain
{
    public sealed class StarterBattle
    {
        public enum Outcome { Playing, Won, Lost }

        public sealed class Card
        {
            public string Name { get; }
            public int Cost { get; }
            public int Damage { get; }
            public int Block { get; }
            public int Hits { get; }
            public bool Exhaust { get; }
            public int Heal { get; }
            public int Draw { get; }
            public int Discard { get; }
            public int Role { get; }
            public bool XCost { get; }
            public int XBleed { get; }
            public bool XHits { get; }
            public int VulnerableScaling { get; }
            public int EvolveCount { get; }
            public int EvolveStages { get; }
            public Card EvolvesInto { get; }
            public int Laceration { get; }
            public int Vulnerable { get; }
            public int Slot { get; }
            public bool AllEnemies { get; }
            public string SlotCategory { get; }
            public IReadOnlyList<string> Categories { get; }
            public int Poison { get; }

            public Card(string name, int cost, int damage, int block, int hits = 1, bool exhaust = false,
                int heal = 0, int draw = 0, int poison = 0, int discard = 0, int laceration = 0, int vulnerable = 0,
                int slot = 0, bool allEnemies = false, string slotCategory = null, IEnumerable<string> categories = null, Card evolvesInto = null,
                int role = 0, bool xCost = false, int xBleed = 0, bool xHits = false,
                int vulnerableScaling = 0, int evolveCount = 0, int evolveStages = 1)
            {
                if (cost < 0 || damage < 0 || block < 0 || hits < 1 || heal < 0 || draw < 0 || poison < 0 || discard < 0)
                    throw new ArgumentOutOfRangeException(nameof(cost), "Card values must be nonnegative.");
                Name = name;
                Cost = cost;
                Damage = damage;
                Block = block;
                Hits = hits;
                Exhaust = exhaust;
                Heal = heal;
                Draw = draw;
                Poison = poison;
                Discard = discard;
                Laceration = laceration;
                EvolvesInto = evolvesInto;
                Role = role;
                XCost = xCost;
                XBleed = xBleed;
                XHits = xHits;
                VulnerableScaling = vulnerableScaling;
                EvolveCount = evolveCount;
                EvolveStages = evolveStages;
                Vulnerable = vulnerable;
                Slot = slot;
                AllEnemies = allEnemies;
                SlotCategory = slotCategory;
                Categories = Array.AsReadOnly((categories ?? Enumerable.Empty<string>()).ToArray());
            }
        }

        public readonly struct EnemyMove
        {
            public readonly bool IsAttack;
            public readonly int Amount;
            public readonly int Hits;
            public readonly int Strength;

            public EnemyMove(bool isAttack, int amount, int hits = 1, int strength = 0)
            {
                IsAttack = isAttack;
                Amount = amount;
                Hits = hits;
                Strength = strength;
            }
        }

        public sealed class KingPattern
        {
            internal readonly EnemyMove[] Opening;
            internal readonly EnemyMove[] Finale;
            internal readonly EnemyMove TransitionAttack;
            internal readonly int Threshold;
            internal readonly int TransitionStrength;
            internal readonly int StunTurns;

            public KingPattern(EnemyMove[] opening, EnemyMove transitionAttack, EnemyMove[] finale,
                int threshold, int strength, int stunTurns)
            {
                Opening = (EnemyMove[])opening.Clone();
                Finale = (EnemyMove[])finale.Clone();
                TransitionAttack = transitionAttack;
                Threshold = threshold;
                TransitionStrength = strength;
                StunTurns = stunTurns;
            }
        }

        public sealed class Enemy
        {
            public string Name { get; }
            public int MaxHp { get; }
            public int Attack { get; }
            public int Guard { get; }
            public int Hp { get; internal set; }
            public int Block { get; internal set; }
            public int Poison { get; internal set; }

            public int Laceration { get; internal set; }
            public int Vulnerable { get; internal set; }
            private readonly Func<int, EnemyMove> pattern;
            private readonly int initialBlock;
            private readonly KingPattern king;
            private bool audience;
            private int step;
            private int cycles;
            public bool RetainBlock { get; }
            public int Strength { get; internal set; }

            public Enemy(string name, int hp, int attack, int guard,
                Func<int, EnemyMove> pattern = null, int initialBlock = 0, bool retainBlock = false, KingPattern king = null)
            {
                if (hp <= 0 || attack < 0 || guard < 0)
                    throw new ArgumentOutOfRangeException(nameof(hp), "Invalid enemy values.");
                Name = name;
                MaxHp = Hp = hp;
                Attack = attack;
                Guard = guard;
                this.pattern = pattern;
                this.initialBlock = initialBlock;
                Block = initialBlock;
                RetainBlock = retainBlock;
                this.king = king;
            }

            internal Enemy StartFresh() => new Enemy(Name, MaxHp, Attack, Guard, pattern, initialBlock, RetainBlock, king);

            internal EnemyMove Intent(int turn)
            {
                if (king == null)
                    return pattern != null ? pattern(turn) :
                        turn % 2 == 1 ? new EnemyMove(true, Attack) : new EnemyMove(false, Guard);
                if (!audience)
                    return king.Opening[step % king.Opening.Length];
                if (step < 0)
                    return new EnemyMove(true, 0, 0);
                return step == 0 ? king.TransitionAttack : king.Finale[(step - 1) % king.Finale.Length];
            }

            internal void CheckKingTransition()
            {
                if (king == null || audience || Hp <= 0 || Hp > king.Threshold)
                    return;
                audience = true;
                step = -king.StunTurns;
                Strength = king.TransitionStrength;
                Poison = 0;
                Laceration = 0;
                Vulnerable = 0;
                Block = 0;
            }

            internal void FinishAction()
            {
                if (king == null)
                    return;
                if (audience && step > 0 && step % king.Finale.Length == 0 && ++cycles % 2 == 0)
                    Strength++;
                step++;
            }
        }

        private readonly List<Card> drawPile;
        private readonly List<Card> discard = new List<Card>();
        private readonly List<Card> hand = new List<Card>();
        private readonly Random random;
        private readonly bool lucky;
        private readonly IReadOnlyList<Card> rewards;
        private readonly List<Card> roleChoices = new List<Card>();
        public IReadOnlyList<Card> RoleChoices => roleChoices.AsReadOnly();
        private readonly List<KeyValuePair<List<Card>, int>> evolutionChoices = new List<KeyValuePair<List<Card>, int>>();
        private int evolutionRemaining;
        private int evolutionStages;
        public IReadOnlyList<Card> EvolutionChoices => evolutionChoices.Select(choice => choice.Key[choice.Value]).ToArray();
        private List<Card> recoveryPile;
        public IReadOnlyList<Card> RecoveryChoices => recoveryPile == null ? Array.Empty<Card>() : recoveryPile.ToArray();
        public string RecoverySource => recoveryPile == drawPile ? "山札" : "捨て札";
        public bool Choosing => recoveryPile != null || PendingDiscard > 0 || roleChoices.Count > 0 || evolutionChoices.Count > 0;
        private readonly List<int> poker = new List<int>();
        public IReadOnlyList<int> Poker => poker.AsReadOnly();
        public IReadOnlyList<int> LastPoker { get; private set; } = Array.Empty<int>();
        public IReadOnlyList<string> LastRoles { get; private set; } = Array.Empty<string>();
        public string SlotResult { get; private set; }

        public const int MaxPlayerHp = 70;
        public const int EnergyPerTurn = 3;
        public const int DrawPerTurn = 5;
        public const int HandLimit = 10;

        public int Turn { get; private set; }
        public int PlayerHp { get; private set; } = MaxPlayerHp;
        public int EnemyHp => Enemies[0].Hp;
        public int PlayerBlock { get; private set; }
        public int EnemyBlock => Enemies[0].Block;
        public int Energy { get; private set; }
        public int DrawCount => drawPile.Count;
        public int PendingDiscard { get; private set; }
        public int DiscardCount => discard.Count;
        public int PlayerStrength { get; private set; }
        public int PlayerGuard { get; private set; }
        public int EvolutionCount { get; private set; }
        public int ExhaustCount { get; private set; }
        public bool EnemyWillAttack => GetEnemyIntent(0).IsAttack;
        public int EnemyIntentValue => GetEnemyIntent(0).Amount;

        public EnemyMove GetEnemyIntent(int index)
        {
            var enemy = Enemies[index];
            var move = enemy.Intent(Turn);
            return move.IsAttack && move.Hits > 0 ? new EnemyMove(true, move.Amount + enemy.Strength, move.Hits) : move;
        }
        public Outcome Result { get; private set; }
        public IReadOnlyList<Card> Hand { get; }
        public IReadOnlyList<Enemy> Enemies { get; }

        public StarterBattle(IEnumerable<Card> deck, int enemyHp, int enemyAttack, int enemyGuard, int seed)
            : this(deck, new[] { new Enemy(string.Empty, enemyHp, enemyAttack, enemyGuard) }, seed)
        {
        }

        public StarterBattle(IEnumerable<Card> deck, IEnumerable<Enemy> enemies, int seed, bool lucky = false, IReadOnlyList<Card> rewards = null)
        {
            var roster = enemies.Select(enemy => enemy.StartFresh()).ToList();
            if (roster.Count == 0)
                throw new ArgumentException("The encounter must have enemies.", nameof(enemies));
            Enemies = roster.AsReadOnly();

            random = new Random(seed);
            this.lucky = lucky;
            this.rewards = rewards;
            drawPile = new List<Card>(deck);
            if (drawPile.Count == 0 || drawPile.Contains(null))
                throw new ArgumentException("The deck must contain valid cards.", nameof(deck));
            Hand = hand.AsReadOnly();
            Shuffle();
            BeginTurn();
        }

        public bool CanPlay(int index) => CanPlay(index, 0);

        public bool CanPlay(int index, int target) =>
            Result == Outcome.Playing && !Choosing && index >= 0 && index < hand.Count && Energy >= hand[index].Cost && hand[index].Role != 2 && hand[index].Role != 5 &&
            ((hand[index].AllEnemies || (hand[index].Damage == 0 && hand[index].Poison == 0 &&
                hand[index].Laceration == 0 && hand[index].Vulnerable == 0 && hand[index].XBleed == 0)) || (target >= 0 && target < Enemies.Count && Enemies[target].Hp > 0));

        public bool TryPlay(int index) => TryPlay(index, 0);

        public bool TryPlay(int index, int target)
        {
            if (!CanPlay(index, target))
                return false;

            var card = hand[index];
            hand.RemoveAt(index);

            var paid = card.XCost ? Energy : card.Cost;
            Energy -= paid;
            if (card.Slot == 0 || ResolveSlot(card))
            {
                if (card.Block > 0)
                    PlayerBlock += card.Block + PlayerGuard;
                if (card.Role == 4)
                {
                    PlayerStrength += 2;
                    PlayerGuard += 2;
                }

                var bonusDamage = card.VulnerableScaling * Enemies.Where(enemy => enemy.Hp > 0).Sum(enemy => enemy.Vulnerable);
                if (card.AllEnemies)
                {
                    foreach (var enemy in Enemies)
                        ApplyToEnemy(card, enemy, paid, bonusDamage);
                }
                else if (target >= 0 && target < Enemies.Count)
                    ApplyToEnemy(card, Enemies[target], paid, bonusDamage);

                if (Enemies.All(entry => entry.Hp == 0))
                    Result = Outcome.Won;
                PlayerHp += Math.Min(card.Heal, MaxPlayerHp - PlayerHp);
                DrawCards(card.Draw);
                if (Result == Outcome.Playing)
                    PendingDiscard = Math.Min(card.Discard, hand.Count);
            }
            if (lucky && card.Role == 0 && PendingDiscard == 0)
                RecordPoker(card.Cost);
            else if (lucky && card.Role == 0)
                pendingPokerCost = card.Cost;
            if (card.EvolvesInto != null)
            {
                discard.Add(card.EvolvesInto);
                EvolutionCount++;
            }
            else if (card.Exhaust)
                ExhaustCount++;
            else
                discard.Add(card);
            if (card.Role == 1 && hand.Count < HandLimit)
                recoveryPile = drawPile.Count > 0 ? drawPile : discard.Count > 0 ? discard : null;
            if (card.EvolveCount > 0)
            {
                evolutionStages = card.EvolveStages;
                evolutionRemaining = card.EvolveCount;
                foreach (var pile in new[] { hand, drawPile, discard })
                    for (var choiceIndex = 0; choiceIndex < pile.Count; choiceIndex++)
                        if (pile[choiceIndex].EvolvesInto != null)
                            evolutionChoices.Add(new KeyValuePair<List<Card>, int>(pile, choiceIndex));
            }
            return true;
        }

        public bool TryEvolve(int index)
        {
            if (index < 0 || index >= evolutionChoices.Count)
                return false;
            var choice = evolutionChoices[index];
            for (var stage = 0; stage < evolutionStages && choice.Key[choice.Value].EvolvesInto != null; stage++)
            {
                choice.Key[choice.Value] = choice.Key[choice.Value].EvolvesInto;
                EvolutionCount++;
            }
            evolutionChoices.RemoveAt(index);
            if (--evolutionRemaining == 0)
                SkipEvolution();
            return true;
        }

        public void SkipEvolution() => evolutionChoices.Clear();

        public bool TryRecover(int index)
        {
            if (recoveryPile == null || index < 0 || index >= recoveryPile.Count || hand.Count >= HandLimit)
                return false;
            var fromDraw = recoveryPile == drawPile;
            hand.Add(recoveryPile[index]);
            recoveryPile.RemoveAt(index);
            recoveryPile = fromDraw && discard.Count > 0 && hand.Count < HandLimit ? discard : null;
            return true;
        }

        public bool TryChooseRole(int index)
        {
            if (index < 0 || index >= roleChoices.Count)
                return false;
            var reward = roleChoices[index];
            roleChoices.Clear();
            (hand.Count < HandLimit ? hand : discard).Add(reward);
            return true;
        }

        private int pendingPokerCost;

        private void ApplyToEnemy(Card card, Enemy enemy, int paid, int bonusDamage)
        {
            if (enemy.Hp == 0)
                return;
            for (var hit = 0; card.Damage > 0 && hit < (card.XHits ? paid : card.Hits) && enemy.Hp > 0; hit++)
            {
                var baseDamage = card.Damage + bonusDamage + PlayerStrength;
                var damage = enemy.Vulnerable > 0 ? (int)(baseDamage * 1.5) : baseDamage;
                var absorbed = Math.Min(enemy.Block, damage);
                enemy.Block -= absorbed;
                enemy.Hp = Math.Max(0, enemy.Hp - (damage - absorbed));
                enemy.CheckKingTransition();
                if (damage > absorbed && enemy.Hp > 0 && enemy.Laceration > 0)
                {
                    enemy.Hp = Math.Max(0, enemy.Hp - enemy.Laceration);
                    enemy.CheckKingTransition();
                }
            }
            if (enemy.Hp > 0)
            {
                enemy.Poison += card.Poison;
                enemy.Laceration += card.Laceration + (card.XBleed > 0 ? paid + card.XBleed : 0);
                enemy.Vulnerable += card.Vulnerable;
            }
        }

        private bool ResolveSlot(Card card)
        {
            var revealed = new List<Card>();
            for (var index = 0; index < 5 && TryTakeTop(out var next); index++)
                revealed.Add(next);
            drawPile.AddRange(revealed);
            Shuffle();
            var matches = revealed.Count(item => item.Categories.Contains(card.SlotCategory));
            var succeeds = matches >= card.Slot;
            SlotResult = $"{card.Name}: {(succeeds ? "成功" : "失敗")}（一致 {matches}/{card.Slot}）" +
                $"  {string.Join("、", revealed.Select(item => item.Name))}";
            return succeeds;
        }

        private void RecordPoker(int cost)
        {
            poker.Add(cost);
            if (poker.Count < 5)
                return;
            LastPoker = Array.AsReadOnly(poker.ToArray());
            LastRoles = DetectRoles(LastPoker);
            poker.Clear();
            if (rewards == null || Result != Outcome.Playing)
                return;
            var names = new[] { "ツーペア", "ストレート", "ブラックジャック", "3×5", "0×5" };
            foreach (var role in LastRoles)
                roleChoices.Add(rewards[Array.IndexOf(names, role)]);
            if (roleChoices.Count == 1)
                TryChooseRole(0);
        }

        public static IReadOnlyList<string> DetectRoles(IReadOnlyList<int> numbers)
        {
            var roles = new List<string>();
            if (numbers.Count != 5)
                return roles.AsReadOnly();
            if (numbers.GroupBy(value => value).Count(group => group.Count() >= 2) >= 2)
                roles.Add("ツーペア");
            if (Enumerable.Range(0, 4).All(numbers.Contains))
                roles.Add("ストレート");
            if (numbers.SequenceEqual(new[] { 1, 0, 1, 0, 1 }))
                roles.Add("ブラックジャック");
            if (numbers.All(value => value == 3))
                roles.Add("3×5");
            if (numbers.All(value => value == 0))
                roles.Add("0×5");
            return roles.AsReadOnly();
        }

        public bool TryDiscard(int index)
        {
            if (PendingDiscard == 0 || index < 0 || index >= hand.Count)
                return false;

            discard.Add(hand[index]);
            hand.RemoveAt(index);
            PendingDiscard--;
            if (lucky && PendingDiscard == 0)
                RecordPoker(pendingPokerCost);
            return true;
        }

        public void EndTurn()
        {
            if (Result != Outcome.Playing || Choosing)
                return;

            discard.AddRange(hand);
            hand.Clear();
            for (var index = 0; index < Enemies.Count; index++)
            {
                var enemy = Enemies[index];
                if (enemy.Hp == 0)
                    continue;
                if (!enemy.RetainBlock)
                    enemy.Block = 0;
                enemy.Hp = Math.Max(0, enemy.Hp - enemy.Poison);
                enemy.Poison = Math.Max(0, enemy.Poison - 1);
                enemy.CheckKingTransition();
                if (enemy.Hp == 0)
                    continue;

                var move = GetEnemyIntent(index);
                if (!move.IsAttack)
                {
                    enemy.Block += move.Amount;
                    enemy.Strength += move.Strength;
                    FinishEnemyAction(enemy);
                    continue;
                }

                for (var hit = 0; hit < move.Hits; hit++)
                {
                    var absorbed = Math.Min(PlayerBlock, move.Amount);
                    PlayerBlock -= absorbed;
                    PlayerHp = Math.Max(0, PlayerHp - (move.Amount - absorbed));
                    if (PlayerHp == 0)
                    {
                        Result = Outcome.Lost;
                        return;
                    }
                }
                FinishEnemyAction(enemy);
            }
            if (Enemies.All(enemy => enemy.Hp == 0))
            {
                Result = Outcome.Won;
                return;
            }
            BeginTurn();
        }

        private static void FinishEnemyAction(Enemy enemy)
        {
            enemy.Laceration = Math.Max(0, enemy.Laceration - 1);
            enemy.Vulnerable = Math.Max(0, enemy.Vulnerable - 1);
            enemy.FinishAction();
        }

        private void BeginTurn()
        {
            Turn++;
            Energy = EnergyPerTurn;
            PlayerBlock = 0;
            DrawCards(DrawPerTurn);
        }

        private void DrawCards(int count)
        {
            for (var drawn = 0; drawn < count && hand.Count < HandLimit && TryTakeTop(out var card); drawn++)
                hand.Add(card);
        }

        private bool TryTakeTop(out Card card)
        {
            if (drawPile.Count == 0)
            {
                if (discard.Count == 0)
                {
                    card = null;
                    return false;
                }
                drawPile.AddRange(discard);
                discard.Clear();
                Shuffle();
            }
            var top = drawPile.Count - 1;
            card = drawPile[top];
            drawPile.RemoveAt(top);
            return true;
        }

        private void Shuffle()
        {
            for (var index = drawPile.Count - 1; index > 0; index--)
            {
                var other = random.Next(index + 1);
                var card = drawPile[index];
                drawPile[index] = drawPile[other];
                drawPile[other] = card;
            }
        }
    }
}











