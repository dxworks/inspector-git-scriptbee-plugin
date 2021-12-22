using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dxworks.ScriptBee.Plugins.InspectorGit.DTO;
using Microsoft.VisualBasic.CompilerServices;

namespace Dxworks.ScriptBee.Plugins.InspectorGit;

public class IGLogReader
{
    public GitLogDTO Read(string filePath)
    {
        var currentCommitLines = new Queue<string>();
        var commits = new List<CommitDTO>();
        using var sr = new StreamReader(filePath);

        string igLogVersion = null;
        if (sr.Peek() >= 0)
        {
            igLogVersion = sr.ReadLine();
        }

        while (sr.Peek() >= 0)
        {
            var line = sr.ReadLine();
            if (line.StartsWith(IGLogConstants.CommitIdPrefix))
            {
                if (currentCommitLines.Any()) commits.Add(ReadCommit(currentCommitLines));
                currentCommitLines = new Queue<string>();
            }

            currentCommitLines.Enqueue(line);
        }

        if (currentCommitLines.Any()) commits.Add(ReadCommit(currentCommitLines));
        return new GitLogDTO(igLogVersion, commits);
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
            sb.AppendLine(nextLine.Remove(0, IGLogConstants.CommitIdPrefix.Length));
            lines.Dequeue();
        }

        return sb.ToString().Trim();
    }

    private static ChangeDTO ReadChange(Queue<string> lines)
    {
        var (changeType, binary) = GetChangeType(lines.Dequeue());
        var parentCommitId = lines.Dequeue();
        var (oldFileName, newFileName) = GetFileName(lines, changeType);

        var currentHunkLines = new Queue<string>();
        var hunks = new List<HunkDTO>();
        if (!binary)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith(IGLogConstants.HunkPrefixLine))
                {
                    if (currentHunkLines.Any()) hunks.Add(ReadHunk(currentHunkLines));
                    currentHunkLines = new Queue<string>();
                }

                currentHunkLines.Enqueue(line);
            }
            if (currentHunkLines.Any()) hunks.Add(ReadHunk(currentHunkLines));
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

    private static HunkDTO ReadHunk(Queue<string> currentHunkLines)
    {
        return new HunkDTO();
    }
}