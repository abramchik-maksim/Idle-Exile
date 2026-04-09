using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Game.Infrastructure.ItemAffixes;

namespace Game.Infrastructure.ItemAffixes.Editor
{
    public static class ItemAffixDatabaseImporter
    {
        private const string DatabaseAssetPath = "Assets/_Game/Infrastructure/ItemAffixes/ItemAffixDatabase.asset";
        private const string PoolCsvPath = "Assets/_Game/Infrastructure/ItemAffixes/ResolvedItemAffixPool.csv";
        private const string SlotsCsvPath = "Assets/_Game/Infrastructure/ItemAffixes/AffixAllowedSlots.csv";
        private const string ModCatalogCsvPath = "Assets/_Game/Infrastructure/ItemAffixes/ModCatalog.csv";

        [MenuItem("Tools/Idle Exile/Import Item Affix CSVs → Database", priority = 10)]
        public static void ImportFromCsv()
        {
            if (!File.Exists(PoolCsvPath))
            {
                Debug.LogError($"[ItemAffixImporter] Missing file: {PoolCsvPath}");
                return;
            }

            if (!File.Exists(SlotsCsvPath))
            {
                Debug.LogError($"[ItemAffixImporter] Missing file: {SlotsCsvPath}");
                return;
            }

            if (!File.Exists(ModCatalogCsvPath))
            {
                Debug.LogError($"[ItemAffixImporter] Missing file: {ModCatalogCsvPath}");
                return;
            }

            var db = AssetDatabase.LoadAssetAtPath<ItemAffixDatabaseSO>(DatabaseAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemAffixDatabaseSO>();
                AssetDatabase.CreateAsset(db, DatabaseAssetPath);
            }

            db.poolRows = ParsePool(File.ReadAllText(PoolCsvPath, Encoding.UTF8));
            db.slotRows = ParseSlots(File.ReadAllText(SlotsCsvPath, Encoding.UTF8));
            db.modCatalogRows = ParseModCatalog(File.ReadAllText(ModCatalogCsvPath, Encoding.UTF8));

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log($"[ItemAffixImporter] Imported {db.poolRows.Count} pool rows, {db.slotRows.Count} slot rows, {db.modCatalogRows.Count} mod catalog rows → {DatabaseAssetPath}");
        }

        private static List<AffixPoolSerializedRow> ParsePool(string text)
        {
            var list = new List<AffixPoolSerializedRow>();
            var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return list;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var p = line.Split(',');
                if (p.Length < 11) continue;

                var row = new AffixPoolSerializedRow
                {
                    affixId = p[0].Trim(),
                    modId = p[1].Trim(),
                    itemSlots = p[2].Trim(),
                    classSpecific = p[3].Trim(),
                    tier = TryInt(p[4], 1),
                    weight = TryInt(p[5], 100),
                    min = TryFloat(p[6], 0f),
                    max = TryFloat(p[7], 0f),
                    valueFormat = p[8].Trim(),
                    templateId = p[9].Trim(),
                    progressBand = p[10].Trim()
                };
                list.Add(row);
            }

            return list;
        }

        private static List<AffixSlotSerializedRow> ParseSlots(string text)
        {
            var list = new List<AffixSlotSerializedRow>();
            var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return list;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var p = line.Split(',');
                if (p.Length < 5) continue;

                string notes = p.Length > 5
                    ? string.Join(",", p, 4, p.Length - 4)
                    : p[4];

                var row = new AffixSlotSerializedRow
                {
                    modId = p[0].Trim(),
                    slotId = p[1].Trim(),
                    weightMultiplier = TryFloat(p[2], 1f),
                    enabled = string.Equals(p[3].Trim(), "TRUE", System.StringComparison.OrdinalIgnoreCase),
                    notes = notes.Trim()
                };
                list.Add(row);
            }

            return list;
        }

        private static List<ModCatalogSerializedRow> ParseModCatalog(string text)
        {
            var list = new List<ModCatalogSerializedRow>();
            var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return list;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var p = line.Split(',');
                if (p.Length < 6) continue;

                string modId = p[0].Trim();
                if (string.IsNullOrEmpty(modId)) continue;

                var row = new ModCatalogSerializedRow
                {
                    modId = modId,
                    valueType = p[3].Trim(),
                    textTemplate = p[5].Trim()
                };
                list.Add(row);
            }

            return list;
        }

        private static int TryInt(string s, int fallback)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return fallback;
        }

        private static float TryFloat(string s, float fallback)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;
            return fallback;
        }
    }
}
