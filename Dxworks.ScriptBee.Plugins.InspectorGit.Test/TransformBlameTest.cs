using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dxworks.ScriptBee.Plugins.InspectorGit.Model;
using Dxworks.ScriptBee.Plugins.InspectorGit.Transformers;
using NUnit.Framework;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Test;

public class TransformBlameTest
{
    private static readonly string _honeydewRepoPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dxw", "scriptbee", "ig",
            "honeydew");

    private static readonly string _kafkaRepoPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dxw", "scriptbee", "ig",
            "kafka");

    private static readonly string _honeydewRepoUrl = "https://github.com/dxworks/honeydew";
    private static readonly string _kafkaRepoUrl = "https://github.com/apache/kafka";

    private static readonly string _rootDirPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dxw", "scriptbee", "ig");

    private static readonly string _honeydewIglogPath = "resources/honeydew.iglog";
    private static readonly string _kafkaIglogPath = "resources/kafka.iglog";

    private static string _sutRepoPath = _kafkaRepoPath;
    private static string _sutIglogPath = _kafkaIglogPath;
    private static string _sutRepoUrl = _kafkaRepoUrl;

    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(_sutRepoPath))
            Directory.Delete(_sutRepoPath, true);

        Directory.CreateDirectory(Path.GetDirectoryName(_sutRepoPath) ?? string.Empty);
        CloneRepository(_sutRepoUrl);
    }

    [Test]
    public void TestBlameHoneydew()
    {
        Assert.True(Directory.Exists(_sutRepoPath));
        var (output, error) = RunGitCommand("log", _sutRepoPath);

        var commitIds = output.Split("\n").Where(it => it.StartsWith("commit")).Select(it => it.Split(" ").Last())
            .ToList();
        commitIds.Reverse();

        Debug.WriteLine($"Reading Iglog at {_sutIglogPath}");
        var gitlogDto = new IGLogReader().Read(_sutIglogPath);
        Debug.WriteLine("Done reading Iglog");
        Debug.WriteLine("Starting Transformation");
        var repository = new RepositoryTransformer().Transform(gitlogDto, true);
        Debug.WriteLine("Done Transformation");

        var allFilesOnDisk = walkDir(new DirectoryInfo(_sutRepoPath));

        var lastCommit = repository.Commits.Last();

        var lastChanges = repository.Files.Select(it => it.GetLastChange(lastCommit)).Where(it => it != null)
            .Where(it => it != null && it.Type != ChangeType.Delete).ToDictionary(it => it.NewFileName);

        Console.WriteLine(lastChanges.Count() - allFilesOnDisk.Count());
        allFilesOnDisk.ForEach(file =>
        {
            var (output, error) = RunGitCommand($"blame {file} {lastCommit.Id}", _sutRepoPath);
            var blameCommits = output.Split('\n')
                .Where(line => line.Any())
                .Select(line => line.Substring(0, line.IndexOf(' ')))
                .Select(line => line.Replace("^", "")).ToList();
            var fileName = getRelativePath(file, _sutRepoPath);
            var computedBlameCommits = lastChanges[fileName]?.AnnotatedLines.Select(it => it.Id);
            if (computedBlameCommits == null)
            {
                Console.WriteLine($"File not found in model: {fileName}");
            }

            else if (blameCommits.Count != computedBlameCommits.Count())
            {
                Console.WriteLine(
                    $"Blames differ in size for: {fileName} - expected: {blameCommits.Count} actual: {computedBlameCommits.Count()}");
            }
            else
            {
                var size = blameCommits.Count();
                var correctLines = 0;
                var computedBlamesList = computedBlameCommits.ToList();
                var blamesList = blameCommits.ToList();
                for (int i = 0; i < size; i++)
                {
                    if (computedBlamesList[i].StartsWith(blamesList[i]))
                        correctLines++;
                }

                if (correctLines != size)
                    Console.WriteLine(
                        $"Blames do not match: matched {correctLines} out of {size} ({correctLines * 100 / size}%) for {fileName}");
            }
        });
    }

    public List<string> RunCommandWithBash(string command)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = "/bin/bash";
        psi.Arguments = command;
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using var process = Process.Start(psi);

        process?.WaitForExit();

        var output = process?.StandardOutput.ReadToEnd();

        return output?.Split('\n').ToList() ?? new List<string>();
    }

    private List<FileInfo> walkDir(DirectoryInfo root)
    {
        FileInfo[]? files = null;

        try
        {
            files = root.GetFiles("*.*");
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized Error!");
        }

        catch (DirectoryNotFoundException e)
        {
            Debug.WriteLine(e.Message);
        }

        if (files != null)
        {
            var subDirs = root.GetDirectories();
            var filesFromSubDirs = subDirs.Where(it => it.Name != ".git").SelectMany(walkDir);

            return files.Concat(filesFromSubDirs).ToList();
        }
        else
            return new List<FileInfo>();
    }

    private void CloneRepository(string repoUrl)
    {
        RunGitCommand($"clone {repoUrl}",
            _rootDirPath);
    }

    private (string, string) RunGitCommand(string command, string workingDirectory)
    {
        using Process process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = command;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = workingDirectory;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (output, error);
    }

    private string getRelativePath(FileInfo fileInfo, string dir)
    {
        if (fileInfo.FullName.StartsWith(dir))
        {
            return fileInfo.FullName.Substring(dir.Length + 1).Replace("\\", "/");
        }

        return "";
    }
}