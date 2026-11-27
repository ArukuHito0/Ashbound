using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Ashbound.Infrastructure
{
    public sealed class MasterCatalog
    {
        public string SourceCommit { get; }
        public string BalanceRevision { get; }
        public int CardCount => cards.Count;
        public int EnemyCount => enemies.Count;
        public int NormalEncounterCount { get; }

        private readonly Dictionary<int, JObject> cards;
        private readonly Dictionary<string, JObject> enemies;

        public MasterCatalog(string json)
        {
            var root = JObject.Parse(json);
            SourceCommit = Required<string>(root, "sourceCommit", JTokenType.String);
            BalanceRevision = Required<string>(root, "balanceRevision", JTokenType.String);
            cards = Index(Rows(root, "cards"), row => Required<int>(row, "id", JTokenType.Integer));
            enemies = Index(Rows(root, "enemies"), row => Required<string>(row, "id", JTokenType.String));

            foreach (var card in cards.Values)
            {
                CheckCardReference(card, "evolveTo");
                CheckCardReference(card, "devolveTo");
            }

            foreach (var encounter in Rows(root, "normalEncounterWeights"))
            {
                ValidateEncounter(encounter);
                NormalEncounterCount++;
            }
        }

        public int GetBaseCost(int definitionId) =>
            Required<int>(cards[definitionId], "cost", JTokenType.Integer);

        public JObject GetCard(int definitionId) => (JObject)cards[definitionId].DeepClone();

        private void CheckCardReference(JObject card, string field)
        {
            var value = card[field];
            if (value != null && value.Type != JTokenType.Null &&
                (value.Type != JTokenType.Integer || !cards.ContainsKey(value.Value<int>())))
                throw new InvalidDataException($"Invalid card reference {card["id"]}.{field}: {value}");
        }

        private void ValidateEncounter(JObject encounter)
        {
            var act = Required<int>(encounter, "act", JTokenType.Integer);
            if (!(encounter["keys"] is JArray keys) || keys.Count == 0)
                throw new InvalidDataException("Encounter has no enemies.");
            foreach (var entry in keys)
            {
                if (entry.Type != JTokenType.String)
                    throw new InvalidDataException("Encounter enemy key must be a string.");
                var key = entry.Value<string>().Split('@')[0];
                var enemyId = act == 7 ? key : $"{act}:{key}";
                if (!enemies.ContainsKey(enemyId))
                    throw new InvalidDataException($"Missing encounter enemy: {enemyId}");
            }
        }

        private static Dictionary<TKey, JObject> Index<TKey>(
            IEnumerable<JObject> rows, Func<JObject, TKey> readKey)
        {
            var index = new Dictionary<TKey, JObject>();
            foreach (var row in rows)
            {
                var key = readKey(row);
                if (!index.TryAdd(key, row))
                    throw new InvalidDataException($"Duplicate master ID: {key}");
            }
            return index;
        }

        private static IEnumerable<JObject> Rows(JObject root, string field)
        {
            if (!(root[field] is JArray rows) || rows.Any(row => row.Type != JTokenType.Object))
                throw new InvalidDataException($"Expected an array of objects: {field}");
            return rows.Cast<JObject>();
        }

        private static T Required<T>(JObject row, string field, JTokenType type)
        {
            var value = row[field];
            if (value == null || value.Type != type)
                throw new InvalidDataException($"Invalid master field: {field}");
            return value.Value<T>();
        }
    }
}
