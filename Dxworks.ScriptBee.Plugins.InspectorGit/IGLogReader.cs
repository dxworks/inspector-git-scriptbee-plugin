using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dxworks.ScriptBee.Plugins.InspectorGit.DTO;
using Dxworks.ScriptBee.Plugins.InspectorGit.Model;
using File = System.IO.File;

namespace Dxworks.ScriptBee.Plugins.InspectorGit;

public class IGLogReader
{
    public GitLogDTO Read(string filePath)
    {
        using var fileStream = File.Open(filePath, FileMode.Open);
        return Read(Path.GetFileNameWithoutExtension(filePath), fileStream);
    }

    public GitLogDTO Read(string name, Stream stream)
    {
        var currentCommitLines = new Queue<string>();
        var commits = new List<CommitDTO>();

        using var sr = new StreamReader(stream);

        string igLogVersion = null;
        if (sr.Peek() >= 0)
        {
            igLogVersion = sr.ReadLine();
        }

        while (sr.Peek() >= 0)
        {
            var line = sr.ReadLine();
            if (line == null)
            {
                continue;
            }

            if (line.StartsWith(IGLogConstants.CommitIdPrefix))
            {
                if (currentCommitLines.Any())
                {
                    commits.Add(ReadCommit(currentCommitLines));
                }

                currentCommitLines = new Queue<string>();
            }

            currentCommitLines.Enqueue(line);
        }

        if (currentCommitLines.Any())
        {
            commits.Add(ReadCommit(currentCommitLines));
        }

        return new GitLogDTO(igLogVersion, name, commits);
    }

    private static CommitDTO ReadCommit(Queue<string> lines)
    {
        var id = lines.Dequeue().Remove(0, IGLogConstants.CommitIdPrefix.Length);
        var parentIds = lines.Dequeue().Split(" ");
        var authorDate = lines.Dequeue();
        var authorEmail = lines.Dequeue();
        var authorName = lines.Dequeue();
        var committerDate = authorDate;
        var committerEmail = authorEmail;
        var committerName = authorName;
        var message = "";
        if (lines.Peek().StartsWith(IGLogConstants.MessagePrefix))
        {
            message = ExtractMessage(lines);
        }
        else
        {
            committerDate = lines.Dequeue();
            committerEmail = lines.Dequeue();
            committerName = lines.Dequeue();
            message = ExtractMessage(lines);
        }

        var currentChangeLines = new Queue<string>();
        var changes = new List<ChangeDTO>();
        foreach (var line in lines)
        {
            if (line.StartsWith(IGLogConstants.ChangePrefix))
            {
                if (currentChangeLines.Any()) changes.Add(ReadChange(currentChangeLines));

                currentChangeLines = new Queue<string>();
            }

            currentChangeLines.Enqueue(line);
        }

        if (currentChangeLines.Any()) changes.Add(ReadChange(currentChangeLines));

        return new CommitDTO(id, parentIds, authorDate, authorEmail, authorName, committerDate, committerEmail,
            committerName, message, changes);
    }

    private static string ExtractMessage(Queue<string> lines)
    {
        var sb = new StringBuilder();
        while (lines.TryPeek(out var nextLine) && nextLine.StartsWith(IGLogConstants.MessagePrefix))
        {
            sb.AppendLine(nextLine.Remove(0, IGLogConstants.MessagePrefix.Length));
            lines.Dequeue();
        }

        return sb.ToString().Trim();
    }

    private static ChangeDTO ReadChange(Queue<string> lines)
    {
        var (changeType, binary) = GetChangeType(lines.Dequeue().Remove(0, 1));
        var parentCommitId = lines.Dequeue();
        var (oldFileName, newFileName) = GetFileName(lines, changeType);

        var hunks = new List<HunkDTO>();
        if (!binary)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith(IGLogConstants.HunkPrefixLine + "="))
                {
                    hunks.Add(ReadHunk(line.Remove(0, 2)));
                }
            }
        }

        return new ChangeDTO(oldFileName.Trim(), newFileName.Trim(), changeType, parentCommitId, binary, hunks);
    }

    private static (ChangeType, bool) GetChangeType(string line)
    {
        var binary = line.Length > 1;
        return line[0] switch
        {
            'A' => (ChangeType.Add, binary),
            'D' => (ChangeType.Delete, binary),
            'R' => (ChangeType.Rename, binary),
            _ => (ChangeType.Modify, binary)
        };
    }

    private static (string, string) GetFileName(Queue<string> lines, ChangeType type)
    {
        var fileName = lines.Dequeue();
        return type switch
        {
            ChangeType.Add => (IGLogConstants.DevNull, fileName),
            ChangeType.Delete => (fileName, IGLogConstants.DevNull),
            ChangeType.Rename => (fileName, lines.Dequeue()),
            ChangeType.Modify => (fileName, fileName),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static HunkDTO ReadHunk(string hunkLine)
    {
        var split = hunkLine.Split("|");
        var addedLinesRanges = split[0];
        var deletedLinesRanges = split[1];

        return new HunkDTO(ParseLineRanges(addedLinesRanges, LineOperation.Add),
            ParseLineRanges(deletedLinesRanges, LineOperation.Delete));
    }

    private static List<LineChangeDTO> ParseLineRanges(string lineRanges, LineOperation lineOperation)
    {
        var ranges = lineRanges.Split(" ").ToList();
        return ranges.SelectMany(range =>
        {
            var split = range.Split(":");
            if (split.Length == 1)
            {
                if (split[0] != "0")
                {
                    return new List<LineChangeDTO> { new(lineOperation, Int32.Parse(split[0]), null) };
                }

                return new List<LineChangeDTO>();
            }

            var start = Int32.Parse(split[0]);
            var end = Int32.Parse(split[1]);
            return Enumerable.Range(start, end - start + 1)
                .Select(number => new LineChangeDTO(lineOperation, number, null));
        }).ToList();
    }
}
