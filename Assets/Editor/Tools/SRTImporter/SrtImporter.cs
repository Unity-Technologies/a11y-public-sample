using UnityEditor.AssetImporters;
using System.IO;

namespace Unity.Samples.Accessibility
{
    [ScriptedImporter(1, "srt")]
    
    /// <summary>
    /// This class is used to import SRT files into Unity.
    /// </summary>
    public class SubtitleImporter : ScriptedImporter
    {
        SrtParser m_Parser;

        /// <summary>
        /// This function parses an asset file using an SrtParser and sets
        /// the parsed result as the main asset during Unity import.
        /// </summary>
        /// <param name="ctx"></param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Parser ??= new SrtParser();

            var subTitle = m_Parser.Parse(File.ReadAllText(ctx.assetPath));

            // (Only the 'Main Asset' is eligible to become a Prefab.)
            ctx.AddObjectToAsset("main obj", subTitle);
            ctx.SetMainObject(subTitle);
        }
    }
}
