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
        readonly string[] k_Delimiters = { "-->", "- >", "->" };
        const string k_CannotReadErrorFormat = "Stream must be readable and seekable: readable: {0} - seekable: {1}";
        const string k_InvalidSrtFormatError = "Stream is not in a valid Srt format";
        const string k_NoSrtPartFoundError = "Parsing as srt returned no srt part.";

        /// <summary>
        /// Parses the specified stream to a srt file content and returns a list of subtitle items.
        /// </summary>
        /// <param name="srtStream">Stream to srt file content</param>
        /// <param name="encoding">Encoding of the srt file</param>
        /// <returns>The list of subtitle items read from the file</returns>
        /// <exception cref="ArgumentException">Thrown if the stream cannot be read or is not seekable</exception>
        /// <exception cref="FormatException">Thrown if the file content is not a proper srt format</exception>
        List<SubtitleItem> ParseStream(Stream srtStream, Encoding encoding)
        {
            // Test if the stream is readable and seekable (just a check, should be good).
            if (!srtStream.CanRead || !srtStream.CanSeek)
            {
                var message = string.Format(k_CannotReadErrorFormat, srtStream.CanSeek, srtStream.CanSeek);
                throw new ArgumentException(message);
            }

            // Seek the beginning of the stream.
            srtStream.Position = 0;

            var streamReader = new StreamReader(srtStream, encoding, true);
            var subtitleItems = new List<SubtitleItem>();
            var subtitleParts = GetSrtSubtitleParts(streamReader).ToList();

            if (subtitleParts.Any())
            {
                foreach (var srtSubtitlePart in subtitleParts)
                {
                    var lines =
                        srtSubtitlePart.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                            .Select(s => s.Trim())
                            .Where(l => !string.IsNullOrEmpty(l))
                            .ToList();
                    var subtitleItem = new SubtitleItem();
                    var text = "";
                    var isTimecodeRead = false;

                    for (var i = 1 /* skip the item number */; i < lines.Count; ++i)
                    {
                        var line = lines[i];

                        if (!isTimecodeRead)
                        {
                            var success = TryParseTimecodeLine(line, out var startTime, out var endTime);

                            if (!success)
                            {
                                continue;
                            }

                            subtitleItem.startTime = UnityTimeSpan.FromMilliseconds(startTime);
                            subtitleItem.endTime = UnityTimeSpan.FromMilliseconds(endTime);
                            isTimecodeRead = true;
                        }
                        else
                        {
                            // Add a new line after each line read.
                            if (!string.IsNullOrEmpty(text))
                            {
                                text += "\n";
                            }

                            text += line;
                        }
                    }

                    subtitleItem.text = text;

                    if ((subtitleItem.startTime.Milliseconds != 0 || subtitleItem.endTime.Milliseconds != 0) && !string.IsNullOrEmpty(subtitleItem.text))
                    {
                        // Parsing succeeded.
                        subtitleItems.Add(subtitleItem);
                    }
                }

                if (subtitleItems.Any())
                {
                    return subtitleItems;
                }

                throw new ArgumentException(k_InvalidSrtFormatError);
            }

            throw new FormatException(k_NoSrtPartFoundError);
        }

        bool TryParseTimecodeLine(string line, out int startTime, out int endTime)
        {
            var parts = line.Split(k_Delimiters, StringSplitOptions.None);
            
            if (parts.Length != 2)
            {
                startTime = -1;
                endTime = -1;
                return false;
            }

            startTime = ParseSrtTimecode(parts[0]);
            endTime = ParseSrtTimecode(parts[1]);
            return true;
        }

        static IEnumerable<string> GetSrtSubtitleParts(TextReader textReader)
        {
            string line;
            var stringBuilder = new StringBuilder();

            while ((line = textReader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line.Trim()))
                {
                    // Return only if not empty.
                    var subtitlePart = stringBuilder.ToString().TrimEnd();

                    if (!string.IsNullOrEmpty(subtitlePart))
                    {
                        yield return subtitlePart;
                    }

                    stringBuilder = new StringBuilder();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            }

            if (stringBuilder.Length > 0)
            {
                yield return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Takes an srt timecode as a string and parses it into a double (in seconds). A srt timecode reads as follows:
        /// 00:00:20,000
        /// </summary>
        /// <param name="timecode">The timecode to parse</param>
        /// <returns>The parsed timecode as a TimeSpan instance if the parsing was successful and -1 otherwise
        /// (subtitles should never show).</returns>
        static int ParseSrtTimecode(string timecode)
        {
            var match = Regex.Match(timecode, "[0-9]+:[0-9]+:[0-9]+([,\\.][0-9]+)?");

            if (!match.Success)
            {
                return -1;
            }
            
            timecode = match.Value;
                
            if (TimeSpan.TryParse(timecode.Replace(',', '.'), out var result))
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
