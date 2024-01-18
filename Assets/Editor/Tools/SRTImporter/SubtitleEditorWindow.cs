using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Samples.Accessibility
{
    [EditorWindowTitle(title = "Subtitle Editor", useTypeNameAsIconName = true)]
    public sealed class SubtitleEditorWindow : EditorWindow
    {
        [MenuItem("Assets/Create/Accessibility/Subtitle File", false, 500)]
        public static void CreateSRTFile()
        {
            if (CommandService.Exists(nameof(CreateSRTFile)))
            {
                CommandService.Execute(nameof(CreateSRTFile), CommandHint.Menu);
            }
            else
            {
                var contents = "1\n00:00:00,000 --> 00:00:10,000\nHello World.";
                ProjectWindowUtil.CreateAssetWithContent("Subtitle.srt", contents);
            }
        }
    }
}
