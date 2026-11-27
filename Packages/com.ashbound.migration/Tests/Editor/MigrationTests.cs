using System.IO;
using Ashbound.Domain;
using Ashbound.Infrastructure;
using NUnit.Framework;
using UnityEditor;

namespace Ashbound.Tests
{
    public sealed class MigrationTests
    {
        [Test]
        public void LoadPublishedCatalogAndKeepSporeAttachmentOnSelectedInstances()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                "Packages/com.ashbound.migration/Reference/master.json");
            Assert.That(asset, Is.Not.Null);
            var catalog = new MasterCatalog(asset.text);
            Assert.That(catalog.CardCount, Is.EqualTo(236));
            Assert.That(catalog.EnemyCount, Is.EqualTo(74));
            Assert.That(catalog.NormalEncounterCount, Is.EqualTo(63));

            var first = new BattleCard(1, 0);
            var second = new BattleCard(2, 0);
            var unselected = new BattleCard(3, 0);
            var baseCost = catalog.GetBaseCost(0);
            first.AttachSpore();
            second.AttachSpore();

            Assert.That(first.GetFixedCost(baseCost), Is.EqualTo(baseCost + 1));
            Assert.That(second.GetFixedCost(baseCost), Is.EqualTo(baseCost + 1));
            Assert.That(unselected.GetFixedCost(baseCost), Is.EqualTo(baseCost));

            first.OnPlayedSuccessfully();
            Assert.That(first.GetFixedCost(baseCost), Is.EqualTo(baseCost));
            Assert.That(second.GetFixedCost(baseCost), Is.EqualTo(baseCost + 1));
        }

        [Test]
        public void RejectDuplicateDefinitions()
        {
            const string json = "{\"sourceCommit\":\"source\",\"balanceRevision\":\"balance\"," +
                                "\"cards\":[{\"id\":0},{\"id\":0}],\"enemies\":[]," +
                                "\"normalEncounterWeights\":[]}";
            Assert.Throws<InvalidDataException>(() => new MasterCatalog(json));
        }
    }
}
