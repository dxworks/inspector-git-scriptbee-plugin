using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dxworks.ScriptBee.Plugins.InspectorGit.Transformers;
using NUnit.Framework;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Test;

public class TransformTest
{
    private readonly string _honeydewRepoPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dxw", "scriptbee", "ig",
            "honeydew");

    private readonly string _rootDirPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dxw", "scriptbee", "ig");

    string _honeydewIglogPath =
        "resources/honeydew.iglog";

    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(_honeydewRepoPath))
            Directory.Delete(_honeydewRepoPath, true);

        Directory.CreateDirectory(Path.GetDirectoryName(_honeydewRepoPath) ?? string.Empty);
        CloneRepository("https://github.com/dxworks/honeydew");
    }

    [Test]
    public void TestTransformHoneydew()
    {
        Assert.True(Directory.Exists(_honeydewRepoPath));
        (var output, var error) = RunGitCommand("log", _honeydewRepoPath);

        var commitIds = output.Split("\n").Where(it => it.StartsWith("commit")).Select(it => it.Split(" ").Last())
            .ToList();
        commitIds.Reverse();

        Console.WriteLine($"Reading Iglog at {_honeydewIglogPath}");
        var gitlogDto = new IGLogReader().Read(_honeydewIglogPath);
        Console.WriteLine("Done reading Iglog");
        Console.WriteLine("Starting Transformation");
        var repository = new RepositoryTransformer().Transform(gitlogDto, false);
        Console.WriteLine("Done Transformation");

        var commitDict = repository.Commits.ToDictionary(it => it.Id);

        commitIds.ForEach(commitId =>
        {
            Console.WriteLine($"Checking out {commitId}");
            RunGitCommand($"checkout {commitId}", _honeydewRepoPath);

            var allFilesOnDisk = walkDir(new DirectoryInfo(_honeydewRepoPath));

            var commit = commitDict[commitId];

            var igFileDict = repository.Files.Where(f =>f.Alive(commit)).ToDictionary(it => it.Path(commit));
            
            allFilesOnDisk.ForEach(fileOnDisk =>
            {
                var relativeFileName = getRelativePath(fileOnDisk, _honeydewRepoPath);
                if (!igFileDict.ContainsKey(relativeFileName))
                {
                    Console.WriteLine($"File {fileOnDisk.FullName} is missing on commit {commit.Id}");
                }
             });
        });

        Console.WriteLine(error);
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
            Console.WriteLine("Unauthorized Error!");
        }

        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine(e.Message);
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
            return fileInfo.FullName.Substring(dir.Length + 1);
        }

        return "";
    }
}