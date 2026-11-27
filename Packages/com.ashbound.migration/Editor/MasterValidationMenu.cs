using System.IO;
using Ashbound.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Ashbound.Editor
{
    internal static class MasterValidationMenu
    {
        [MenuItem("ASHBOUND/Validate master JSON")]
        private static void Validate()
        {
            var path = EditorUtility.OpenFilePanel("ASHBOUND master JSON", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path))
                return;

            var catalog = new MasterCatalog(File.ReadAllText(path));
            Debug.Log($"ASHBOUND {catalog.SourceCommit} / {catalog.BalanceRevision}: " +
                      $"cards={catalog.CardCount}, enemies={catalog.EnemyCount}, " +
                      $"normal encounters={catalog.NormalEncounterCount}");
        }
    }
}
