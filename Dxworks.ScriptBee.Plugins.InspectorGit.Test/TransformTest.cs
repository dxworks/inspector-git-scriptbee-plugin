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

    private static string _sutRepoPath = _honeydewRepoPath;
    private static string _sutIglogPath = _honeydewIglogPath;
    private static string _sutRepoUrl = _honeydewRepoUrl;
    
    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(_sutRepoPath))
            Directory.Delete(_sutRepoPath, true);

        Directory.CreateDirectory(Path.GetDirectoryName(_sutRepoPath) ?? string.Empty);
        CloneRepository(_sutRepoUrl);
    }

    [Test]
    public void TestTransformHoneydew()
    {
        Assert.True(Directory.Exists(_sutRepoPath));
        (var output, var error) = RunGitCommand("log", _sutRepoPath);

        var commitIds = output.Split("\n").Where(it => it.StartsWith("commit")).Select(it => it.Split(" ").Last())
            .ToList();
        commitIds.Reverse();
        
        Debug.WriteLine($"Reading Iglog at {_sutIglogPath}");
        var gitlogDto = new IGLogReader().Read(_sutIglogPath);
        Debug.WriteLine("Done reading Iglog");
        Debug.WriteLine("Starting Transformation");
        var repository = new RepositoryTransformer().Transform(gitlogDto, true);
        Debug.WriteLine("Done Transformation");

        var commitDict = repository.Commits.ToDictionary(it => it.Id);

        Dictionary<string, List<string>> commitsWithMissingFiles = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> filesMissingInCommits = new Dictionary<string, List<string>>();

        
        foreach (var (commitId, index) in commitIds.Select((c, i) => (c, i)))
        {

            commitsWithMissingFiles[commitId] = new List<string>();
            Debug.WriteLine($"Checking out {commitId} ({index}/{commitIds.Count})");
            RunGitCommand($"checkout {commitId}", _sutRepoPath);

            var allFilesOnDisk = walkDir(new DirectoryInfo(_sutRepoPath));

            var commit = commitDict[commitId];

            var igFileDict = repository.Files.Where(f => f.Alive(commit)).Select(it=>it.Path(commit)).ToHashSet();
            
            allFilesOnDisk.ForEach(fileOnDisk =>
            {
                var relativeFileName = getRelativePath(fileOnDisk, _sutRepoPath);
                if (!igFileDict.Contains(relativeFileName))
                {
                    Debug.WriteLine($"File {fileOnDisk.FullName} is missing on commit {commit.Id}");
                    commitsWithMissingFiles[commitId].Add(relativeFileName);
                    if(filesMissingInCommits.ContainsKey(relativeFileName))
                        filesMissingInCommits[relativeFileName].Add(commitId);
                    else
                        filesMissingInCommits[relativeFileName] = new List<string>{commitId};
                }
                else
                {
                    var expectedLines = File.ReadLines(fileOnDisk.FullName).Count();

                    var file = repository.Files.Where(f => f.Path(commit) == relativeFileName);

                }
            });
        }

        Debug.WriteLine($"Did not find a total of {filesMissingInCommits.Count} files in {commitsWithMissingFiles.Where(it => it.Value.Count > 0)} commits");
        Debug.WriteLine(error);
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