using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ashbound.Domain;
using Ashbound.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Ashbound.BattleScene
{
    public sealed class StarterBattleSetup
    {
        public const string MasterPath = "Packages/com.ashbound.migration/Reference/master.json";
        public string EnemyName { get; private set; }
        public int EnemyMaxHp { get; private set; }

        public StarterBattle Create(string json, int seed) => Create(json, seed, null);

        public StarterBattle CreateEncounter(string json, int seed) => CreateEncounter(json, seed, "1:ash+shade");

        public StarterBattle CreateEncounter(string json, int seed, string encounterId) => Create(json, seed, encounterId);

        public StarterBattle CreateEncounter(string json, int seed, string encounterId, string explorer) =>
            Create(json, seed, encounterId, explorer);

        private StarterBattle Create(string json, int seed, string encounterId, string explorer = "wanderer")
        {
            var multipleEnemies = encounterId != null;
            var catalog = new MasterCatalog(json);
            var root = JObject.Parse(json);
            var keys = ResolveKeys(root, encounterId);
            var enemies = keys.Select((key, offset) => ReadEnemy(root, key, offset, multipleEnemies)).ToArray();
            EnemyName = enemies[0].Name;
            EnemyMaxHp = enemies[0].MaxHp;

            var deck = new List<StarterBattle.Card>();
            AddCards(deck, catalog, 0, multipleEnemies ? 2 : 3);
            AddCards(deck, catalog, 1, multipleEnemies ? 2 : 3);
            if (explorer == "hunter")
            {
                AddCards(deck, catalog, 121, 1);
                AddCards(deck, catalog, 99, 2);
                AddCards(deck, catalog, 145, 1);
                AddCards(deck, catalog, 133, 1);
                AddCards(deck, catalog, 7, 1);
            }
            else if (explorer == "lucky")
            {
                AddCards(deck, catalog, 171, 3);
                AddCards(deck, catalog, 195, 2);
                AddCards(deck, catalog, 7, 1);
            }
            else if (multipleEnemies)
            {
                AddCards(deck, catalog, 2, 1);
                AddCards(deck, catalog, 4, 1);
                AddCards(deck, catalog, 5, 1);
                AddCards(deck, catalog, 13, 1);
                AddCards(deck, catalog, 7, 1);
                AddCards(deck, catalog, 3, 1);
            }
            else
            {
                AddCards(deck, catalog, 6, 2);
                AddCards(deck, catalog, 8, 2);
            }
            var rewards = explorer == "lucky"
                ? Enumerable.Range(223, 5).Select(id => ReadCard(catalog, id, new HashSet<int>())).ToArray()
                : null;
            return new StarterBattle(deck, enemies, seed, explorer == "lucky", rewards);
        }

        private static string[] ResolveKeys(JObject root, string id)
        {
            switch (id)
            {
                case null: return new[] { "ash" };
                case "1:ravager": return new[] { "ravager" };
                case "1:plated": return new[] { "plated" };
                case "1:boss": return new[] { "boss" };
                case "1:trio": return new[] { "trio", "trio", "trio" };
                default:
                    return root["normalEncounterWeights"].Single(row => row.Value<string>("act") == "1" &&
                        row.Value<string>("id") == id)["keys"].Values<string>().ToArray();
            }
        }

        private static StarterBattle.Enemy ReadEnemy(JObject root, string key, int offset, bool useWebPattern)
        {
            var id = $"1:{key}";
            var enemy = root["enemies"].Single(row => row.Value<string>("id") == id);
            var fields = root["enemyActionFields"];
            System.Func<int, string, int> Value = (phase, field) => ReadAction(fields, $"{id}:{phase}:{field}");
            var name = enemy.Value<string>("name");
            var hp = enemy.Value<int>("effectiveBaseHp");
            if (!useWebPattern)
                return new StarterBattle.Enemy(name, hp, Value(1, "d"), Value(2, "b"));

            System.Func<int, StarterBattle.EnemyMove> pattern;
            switch (key)
            {
                case "ash":
                case "shade":
                    var basicAttack = Attack(Value(1, "d"));
                    var basicGuard = Guard(Value(2, "b"));
                    pattern = turn => turn % 3 == 0 ? basicGuard : basicAttack;
                    break;
                case "soldier":
                    var heavyAttack = Attack(Value(1, "d"));
                    var heavyGuard = Guard(Value(2, "b"));
                    pattern = turn => turn % 3 == 2 ? heavyGuard : heavyAttack;
                    break;
                case "hunter":
                    var doubleHit = Attack(Value(1, "d"), Value(1, "h"));
                    var strongerDoubleHit = Attack(Value(2, "d"), Value(2, "h"));
                    pattern = turn => turn % 3 == 0 ? strongerDoubleHit : doubleHit;
                    break;
                case "priest":
                    var priestAttack = Value(1, "d");
                    var priestGrowth = Value(1, "s");
                    var priestGuard = Guard(Value(2, "b"));
                    pattern = turn => turn % 3 == 1 ? priestGuard : Attack(priestAttack + (turn - 1) / 2 * priestGrowth);
                    break;
                case "ravager":
                    var ravagerAttack = Value(1, "d");
                    var ravagerGrowth = Value(1, "s");
                    var ravagerHits = Value(1, "h");
                    pattern = turn => Attack(ravagerAttack + (turn - 1) * ravagerGrowth, turn >= 5 ? ravagerHits : 1);
                    break;
                case "plated":
                    var platedMoves = new[] { Attack(Value(1, "d")), Attack(Value(2, "d")),
                        Attack(Value(3, "d"), Value(3, "h")), Guard(Value(4, "b"), Value(4, "s")) };
                    return new StarterBattle.Enemy(name, hp, 0, 0, turn => platedMoves[(turn - 1) % 4],
                        ReadAction(fields, $"{id}:passive:initialBlock"), true);
                case "boss":
                    var opening = new[] { Attack(Value(1, "d")), Attack(Value(2, "d")),
                        Attack(Value(3, "d")), Guard(Value(4, "b"), Value(4, "s")) };
                    var finale = new[] { Attack(Value(6, "d"), Value(6, "h")),
                        Attack(Value(7, "d"), Value(7, "h")), Attack(Value(8, "d"), Value(8, "h")) };
                    var king = new StarterBattle.KingPattern(opening, Attack(Value(5, "d")), finale,
                        Value(9, "triggerHp"), Value(9, "s"), Value(9, "stunTurns"));
                    return new StarterBattle.Enemy(name, hp, 0, 0, king: king);
                case "trio":
                    var trioMoves = new[] { Attack(Value(1, "d")), Guard(Value(2, "b")), Attack(Value(3, "d")) };
                    pattern = turn => trioMoves[(turn - 1 + offset) % 3];
                    break;
                default:
                    throw new InvalidDataException($"未対応の敵行動: {id}");
            }
            return new StarterBattle.Enemy(name, hp, 0, 0, pattern);
        }

        private static StarterBattle.EnemyMove Attack(int damage, int hits = 1) =>
            new StarterBattle.EnemyMove(true, damage, hits);

        private static StarterBattle.EnemyMove Guard(int block, int strength = 0) =>
            new StarterBattle.EnemyMove(false, block, strength: strength);

        private static int ReadAction(JToken actions, string id) =>
            actions.Single(row => row.Value<string>("id") == id).Value<int>("effectiveValue");

        private static void AddCards(List<StarterBattle.Card> deck, MasterCatalog catalog, int id, int count)
        {
            var card = ReadCard(catalog, id, new HashSet<int>());
            for (var index = 0; index < count; index++)
                deck.Add(card);
        }

        private static StarterBattle.Card ReadCard(MasterCatalog catalog, int id, HashSet<int> visited)
        {
            if (!visited.Add(id))
                throw new InvalidDataException($"循環した進化先: {id}");
            var definition = catalog.GetCard(id);
            var effect = (JObject)definition["fx"];
            if (effect == null || !effect.Properties().Any() ||
                effect.Properties().Any(field => field.Name != "d" && field.Name != "b" && field.Name != "h" && field.Name != "ex" &&
                    field.Name != "heal" && field.Name != "draw" && field.Name != "p" && field.Name != "discard" &&
                    field.Name != "bleed" && field.Name != "vulnerable" && field.Name != "slot" &&
                    field.Name != "all" && field.Name != "luckyKey" &&
                    field.Name != "xBleed" && field.Name != "xHits" && field.Name != "allVulnerableDamage" &&
                    field.Name != "evolveDeck" && field.Name != "evolveStages" &&
                    !(id >= 223 && id <= 227 && (field.Name == "energy" || field.Name == "s" || field.Name == "agility"))))
                throw new InvalidDataException($"カード ID {id} に未対応の効果があります。");

            var categories = new List<string>();
            if (effect["d"] != null)
                categories.Add("攻撃");
            if ((effect.Value<int?>("b") ?? 0) > 0)
                categories.Add("防御");
            if (categories.Count == 0 || effect["p"] != null || effect["heal"] != null)
                categories.Add("補助");
            var nextId = definition.Value<int?>("evolveTo");
            return new StarterBattle.Card(definition.Value<string>("name"), catalog.GetBaseCost(id),
                effect.Value<int?>("d") ?? 0, effect.Value<int?>("b") ?? 0,
                effect.Value<int?>("h") ?? 1, (effect.Value<int?>("ex") ?? 0) != 0,
                effect.Value<int?>("heal") ?? 0, effect.Value<int?>("draw") ?? 0, effect.Value<int?>("p") ?? 0,
                effect.Value<int?>("discard") ?? 0, effect.Value<int?>("bleed") ?? 0,
                effect.Value<int?>("vulnerable") ?? 0, effect.Value<int?>("slot") ?? 0,
                (effect.Value<int?>("all") ?? 0) != 0, definition["types"]?.First?.Value<string>(), categories,
                nextId.HasValue ? ReadCard(catalog, nextId.Value, visited) : null,
                role: id >= 223 && id <= 227 ? id - 222 : 0, xCost: definition.Value<bool?>("xCost") ?? false,
                xBleed: effect.Value<int?>("xBleed") ?? 0, xHits: (effect.Value<int?>("xHits") ?? 0) > 0,
                vulnerableScaling: effect.Value<int?>("allVulnerableDamage") ?? 0,
                evolveCount: effect.Value<int?>("evolveDeck") ?? 0, evolveStages: effect.Value<int?>("evolveStages") ?? 1);
        }

    }
}









