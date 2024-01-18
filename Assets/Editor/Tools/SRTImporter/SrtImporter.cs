using System.IO;
using UnityEditor.AssetImporters;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// This class is used to import SRT files into Unity.
    /// </summary>
    [ScriptedImporter(1, "srt")]
    public class SubtitleImporter : ScriptedImporter
    {
        SrtParser m_Parser;

        /// <summary>
        /// Parses an asset file using an SrtParser and sets the parsed result as the main asset during Unity import.
        /// </summary>
        /// <param name="ctx"></param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Parser ??= new SrtParser();

            var subtitle = m_Parser.Parse(File.ReadAllText(ctx.assetPath));

            ctx.AddObjectToAsset("main obj", subtitle);
            ctx.SetMainObject(subtitle);
        }
    }
}
