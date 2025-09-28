using System.IO;
using Unity.Samples.LetterSpell;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Samples.LetterSpellEditor
{
    [ScriptedImporter(1, "lsdata")]
    public class LSDataImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var wordDatabase = ScriptableObject.CreateInstance<WordDatabase>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(context.assetPath), wordDatabase);

            context.AddObjectToAsset("main obj", wordDatabase);
            context.SetMainObject(wordDatabase);
        }
    }
}
