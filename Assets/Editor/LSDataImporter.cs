using System.IO;
using Unity.Samples.LetterSpell;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Samples.LetterSpellEditor
{
    
    [ScriptedImporter(1, "lsdata")]
    public class LSDataImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var wordDatabase = ScriptableObject.CreateInstance<WordDatabase>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(ctx.assetPath), wordDatabase);
            
            ctx.AddObjectToAsset("main obj", wordDatabase);
            ctx.SetMainObject(wordDatabase);
        }
    } 
}
