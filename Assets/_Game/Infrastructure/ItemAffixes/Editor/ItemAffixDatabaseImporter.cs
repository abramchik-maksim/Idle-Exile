using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Domain.Items;
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

            if (!ValidatePoolModIdsAgainstCatalog(db))
            {
                Debug.LogError("[ItemAffixImporter] Import aborted: pool contains modIds missing in ModCatalog.csv.");
                return;
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log($"[ItemAffixImporter] Imported {db.poolRows.Count} pool rows, {db.slotRows.Count} slot rows, {db.modCatalogRows.Count} mod catalog rows → {DatabaseAssetPath}");
        }

        [MenuItem("Tools/Idle Exile/Validate Item Affix CSVs (pool vs ModCatalog)", priority = 11)]
        public static void ValidateFromCsvFiles()
        {
            if (!File.Exists(PoolCsvPath) || !File.Exists(ModCatalogCsvPath))
            {
                Debug.LogError("[ItemAffixImporter] Missing ResolvedItemAffixPool.csv or ModCatalog.csv");
                return;
            }

            var pool = ParsePool(File.ReadAllText(PoolCsvPath, Encoding.UTF8));
            var catalog = ParseModCatalog(File.ReadAllText(ModCatalogCsvPath, Encoding.UTF8));
            var db = ScriptableObject.CreateInstance<ItemAffixDatabaseSO>();
            db.poolRows = pool;
            db.modCatalogRows = catalog;
            var ok = ValidatePoolModIdsAgainstCatalog(db);
            if (ok)
                Debug.Log("[ItemAffixImporter] Validation passed: all pool modIds exist in ModCatalog.csv.");
            UnityEngine.Object.DestroyImmediate(db);
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

                var p = ParseCsvLine(line);
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

                var p = ParseCsvLine(line);
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

                var p = ParseCsvLine(line);
                if (p.Length < 6) continue;

                string modId = p[0].Trim();
                if (string.IsNullOrEmpty(modId)) continue;

                string elementStr = p.Length > 2 ? p[2].Trim() : string.Empty;
                if (!ModCatalogElementExtensions.TryParse(elementStr, out _))
                    Debug.LogWarning($"[ItemAffixImporter] Unknown element '{elementStr}' for modId={modId}, using NonSpecific.");

                var row = new ModCatalogSerializedRow
                {
                    modId = modId,
                    family = p.Length > 1 ? p[1].Trim() : string.Empty,
                    element = elementStr,
                    valueType = p[3].Trim(),
                    textTemplate = p[5].Trim()
                };
                list.Add(row);
            }

            return list;
        }

        private static bool ValidatePoolModIdsAgainstCatalog(ItemAffixDatabaseSO db)
        {
            var catalogIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in db.modCatalogRows)
            {
                if (!string.IsNullOrWhiteSpace(r.modId))
                    catalogIds.Add(r.modId.Trim());
            }

            var missing = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in db.poolRows)
            {
                if (string.IsNullOrWhiteSpace(r.modId)) continue;
                var id = r.modId.Trim();
                if (!catalogIds.Contains(id))
                    missing.Add(id);
            }

            if (missing.Count > 0)
            {
                foreach (var id in missing)
                    Debug.LogError($"[ItemAffixImporter] Pool references modId '{id}' missing from ModCatalog.csv");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lightweight CSV parser with quoted comma support (RFC4180-like).
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>(16);
            var sb = new StringBuilder(line.Length);
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            fields.Add(sb.ToString());
            return fields.ToArray();
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
