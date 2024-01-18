using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// This class is used to parse a srt file content to a list of subtitle items.
    /// </summary>
    public class SrtParser
    {
        readonly string[] k_Delimiters = {"-->", "- >", "->"};
        readonly string k_CannotReadErrorFormat = "Stream must be readable and seekable: readable: {0} - seekable: {1}";
        readonly string k_InvalidSrtFormatError = "Stream is not in a valid Srt format";
        readonly string k_NoSrtPartFoundError = "Parsing as srt returned no srt part.";

        /// <summary>
        /// Parse the specified stream to a srt file content and returns a list of subtitle items
        /// </summary>
        /// <param name="srtStream">Stream to srt file content</param>
        /// <param name="encoding">Encoding of the srt file</param>
        /// <returns>The list of subtitle items read from file</returns>
        /// <exception cref="ArgumentException">Thrown if the stream is cannot be read or is not seekable</exception>
        /// <exception cref="FormatException">The file content is not a proper Srt format</exception>
        List<SubtitleItem> ParseStream(Stream srtStream, Encoding encoding)
        {
            // test if stream if readable and seekable (just a check, should be good)
            if (!srtStream.CanRead || !srtStream.CanSeek)
            {
                var message = string.Format(k_CannotReadErrorFormat, srtStream.CanSeek, srtStream.CanSeek);
                throw new ArgumentException(message);
            }

            srtStream.Position = 0; // seek the beginning of the stream

            var reader = new StreamReader(srtStream, encoding, true);
            var items = new List<SubtitleItem>();
            var srtSubParts = GetSrtSubtitleParts(reader).ToList();

            if (srtSubParts.Any())
            {
                foreach (var srtSubPart in srtSubParts)
                {
                    var lines =
                        srtSubPart.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                            .Select(s => s.Trim())
                            .Where(l => !string.IsNullOrEmpty(l))
                            .ToList();
                    var item = new SubtitleItem();
                    var text = "";
                    var timecodeRead = false;

                    for (var i = 1 /*skip the item number*/; i < lines.Count; ++i)
                    {
                        var line = lines[i];

                        if (!timecodeRead)
                        {
                            var success = TryParseTimecodeLine(line, out var startTc, out var endTc);

                            if (!success)
                            {
                                continue;
                            }

                            item.startTime = UnityTimeSpan.FromMilliseconds(startTc);
                            item.endTime = UnityTimeSpan.FromMilliseconds(endTc);
                            timecodeRead = true;
                        }
                        else
                        {
                            // Add new line after each line read
                            if (!string.IsNullOrEmpty(text))
                            {
                                text += "\n";
                            }

                            text += line;
                        }
                    }

                    item.text = text;

                    if ((item.startTime.Milliseconds != 0 || item.endTime.Milliseconds != 0) && !string.IsNullOrEmpty(item.text))
                    {
                        // parsing succeeded
                        items.Add(item);
                    }
                }

                if (items.Any())
                {
                    return items;
                }

                throw new ArgumentException(k_InvalidSrtFormatError);
            }

            throw new FormatException(k_NoSrtPartFoundError);
        }

        bool TryParseTimecodeLine(string line, out int startTc, out int endTc)
        {
            var parts = line.Split(k_Delimiters, StringSplitOptions.None);
            
            if (parts.Length != 2)
            {
                startTc = -1;
                endTc = -1;
                return false;
            }

            startTc = ParseSrtTimecode(parts[0]);
            endTc = ParseSrtTimecode(parts[1]);
            return true;
        }

        IEnumerable<string> GetSrtSubtitleParts(TextReader reader)
        {
            string line;
            var sb = new StringBuilder();

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line.Trim()))
                {
                    // return only if not empty
                    var res = sb.ToString().TrimEnd();

                    if (!string.IsNullOrEmpty(res))
                    {
                        yield return res;
                    }

                    sb = new StringBuilder();
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            if (sb.Length > 0)
            {
                yield return sb.ToString();
            }
        }

        /// <summary>
        /// Takes an Srt timecode as a string and parses it into a double (in seconds). A Srt timecode reads as follows:
        /// 00:00:20,000
        /// </summary>
        /// <param name="timecodeToParse">The timecode to parse</param>
        /// <returns>The parsed timecode as a TimeSpan instance. If the parsing was unsuccessful, -1 is returned (subtitles should never show)</returns>
         static int ParseSrtTimecode(string timecodeToParse)
        {
            var match = Regex.Match(timecodeToParse, "[0-9]+:[0-9]+:[0-9]+([,\\.][0-9]+)?");

            if (!match.Success)
            {
                return -1;
            }
            
            timecodeToParse = match.Value;
                
            if (TimeSpan.TryParse(timecodeToParse.Replace(',', '.'), out var result))
            {
                return (int)result.TotalMilliseconds;
            }

            return -1;
        }

        public Subtitle Parse(string content)
        {
            var subtitle = ScriptableObject.CreateInstance<Subtitle>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content ?? ""));
            
            subtitle.items = ParseStream(stream, Encoding.UTF8);
            return subtitle;
        }
    }
}
