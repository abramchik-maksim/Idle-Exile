using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Game.Infrastructure.ItemAffixes;

namespace Game.Infrastructure.ItemAffixes.Editor
{
    public static class DropQualityDatabaseImporter
    {
        private const string DatabaseAssetPath = "Assets/_Game/Infrastructure/ItemAffixes/DropQualityDatabase.asset";
        private const string CsvPath = "Assets/_Game/Infrastructure/ItemAffixes/DropQualityProgression.csv";

        [MenuItem("Tools/Idle Exile/Import Drop Quality CSV → Database", priority = 11)]
        public static void ImportFromCsv()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[DropQualityImporter] Missing file: {CsvPath}");
                return;
            }

            var db = AssetDatabase.LoadAssetAtPath<DropQualityDatabaseSO>(DatabaseAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<DropQualityDatabaseSO>();
                AssetDatabase.CreateAsset(db, DatabaseAssetPath);
            }

            db.bands = ParseBands(File.ReadAllText(CsvPath, Encoding.UTF8));

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DropQualityImporter] Imported {db.bands.Count} bands.");
        }

        private static List<DropQualityBandRow> ParseBands(string csv)
        {
            var result = new List<DropQualityBandRow>();
            var lines = csv.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var cols = line.Split(',');
                if (cols.Length < 13) continue;

                result.Add(new DropQualityBandRow
                {
                    progressBand = ParseInt(cols[0]),
                    idleStageMin = ParseInt(cols[1]),
                    idleStageMax = ParseInt(cols[2]),
                    allowedTierMin = ParseInt(cols[5]),
                    allowedTierMax = ParseInt(cols[6]),
                    tierBias = ParseFloat(cols[7]),
                    qualityMultiplier = ParseFloat(cols[8]),
                    weightNormal = ParseInt(cols[9]),
                    weightMagic = ParseInt(cols[10]),
                    weightRare = ParseInt(cols[11]),
                    weightMythic = ParseInt(cols[12])
                });
            }

            return result;
        }

        private static int ParseInt(string s) =>
            int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

        private static float ParseFloat(string s) =>
            float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
    }
}
