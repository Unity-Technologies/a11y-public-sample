using System.IO;
using Unity.Samples.ClosedCaptions;
using UnityEditor.AssetImporters;

namespace Unity.Samples.ClosedCaptionsEditor
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
        /// <param name="context"></param>
        public override void OnImportAsset(AssetImportContext context)
        {
            m_Parser ??= new SrtParser();

            var subtitle = m_Parser.Parse(File.ReadAllText(context.assetPath));

            context.AddObjectToAsset("main obj", subtitle);
            context.SetMainObject(subtitle);
        }
    }
}
