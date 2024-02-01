using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Samples.ClosedCaptionsEditor
{
    [EditorWindowTitle(title = "Subtitle Editor", useTypeNameAsIconName = true)]
    public sealed class SubtitleEditorWindow : EditorWindow
    {
        [MenuItem("Assets/Create/Accessibility/Subtitle File", false, 500)]
        public static void CreateSrtFile()
        {
            if (CommandService.Exists(nameof(CreateSrtFile)))
            {
                CommandService.Execute(nameof(CreateSrtFile), CommandHint.Menu);
            }
            else
            {
                const string contents = "1\n00:00:00,000 --> 00:00:10,000\nHello World.";
                ProjectWindowUtil.CreateAssetWithContent("Subtitle.srt", contents);
            }
        }
    }
}
