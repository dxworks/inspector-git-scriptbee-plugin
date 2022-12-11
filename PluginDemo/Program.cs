using System.Diagnostics;
using Dxworks.ScriptBee.Plugins.InspectorGit;
using Dxworks.ScriptBee.Plugins.InspectorGit.Model;
using Dxworks.ScriptBee.Plugins.InspectorGit.Transformers;
using File = Dxworks.ScriptBee.Plugins.InspectorGit.Model.File;

namespace PluginDemo;

internal static class Program
{
    private static readonly string RootDirPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dxw", "scriptbee", "ig");

    private const string IglogPath = "resources/honeydew.iglog";

    private static readonly string RepoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".dxw", "scriptbee", "ig",
        "kafka");

    private static readonly string RepoUrl = "https://github.com/apache/kafka";

    private static void Main()
    {
        Console.WriteLine($"Reading Iglog at {IglogPath}");

        var gitLogDto = new IGLogReader().Read(IglogPath);

        Console.WriteLine("Done reading Iglog");
        Console.WriteLine("Starting Transformation");
        var repository = new RepositoryTransformer().Transform(gitLogDto);
        Console.WriteLine("Done Transformation");
        // var (wrongFiles, notFoundFiles) = TestBlames(repository);
        // Console.WriteLine("Done testing");
    }


    private static (Dictionary<string, List<File>> wrongFiles, List<string> notFoundFiles) TestBlames(
        Repository repository)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RepoUrl) ?? string.Empty);
        CloneRepository(RepoUrl);

        var allFilesOnDisk = WalkDir(new DirectoryInfo(RepoPath));

        var lastCommit = repository.Commits.Last();

        var lastChanges = repository.Files.Select(it => it.GetLastChange(lastCommit)).Where(it => it != null)
            .Select(it => it!)
            .Where(it => it.Type != ChangeType.Delete).ToDictionary(it => it.NewFileName);

        Console.WriteLine($"ig files - all files = {lastChanges.Count - allFilesOnDisk.Count}");

        var wrongFiles = new Dictionary<string, List<File>>();
        wrongFiles["notSized"] = new List<File>();
        wrongFiles["notMatch"] = new List<File>();

        var notFoundFiles = new List<string>();

        var totalCorrectLines = 0;
        var totalLines = 0;

        var numberOfFiles = allFilesOnDisk.Count;
        var count = 0;
        allFilesOnDisk.ForEach(file =>
        {
            count++;
            Debug.WriteLine($"file {count} of {numberOfFiles} ({count * 100 / numberOfFiles}%)");

            var fileName = GetRelativePath(file, RepoPath);

            if (!lastChanges.ContainsKey(fileName))
            {
                Console.WriteLine($"File not found in model: {fileName}");
                notFoundFiles.Add(fileName);
            }
            else if (!lastChanges[fileName]!.File.Binary)
            {
                var output = RunGitCommand($"blame {file} {lastCommit.Id}", RepoPath);
                var blameCommits = output.Split('\n')
                    .Where(line => line.Any())
                    .Select(line => line[..line.IndexOf(' ')])
                    .Select(line => line.Replace("^", "")).ToList();

                var computedBlameCommits = lastChanges[fileName]!.AnnotatedLines.Select(it => it.Id);


                var enumerable = computedBlameCommits.ToList();
                if (blameCommits.Count != enumerable!.Count)
                {
                    Console.WriteLine(
                        $"Blames differ in size for: {fileName} - expected: {blameCommits.Count} actual: {enumerable.Count}");
                    wrongFiles["notSized"].Add(lastChanges[fileName].File);
                }
                else
                {
                    var size = blameCommits.Count;
                    totalLines += size;
                    var correctLines = 0;
                    var computedBlamesList = enumerable.ToList();
                    var blamesList = blameCommits.ToList();
                    for (var i = 0; i < size; i++)
                    {
                        if (computedBlamesList[i].StartsWith(blamesList[i]))
                        {
                            correctLines++;
                            totalCorrectLines++;
                        }
                    }

                    if (correctLines != size)
                    {
                        wrongFiles["notMatch"].Add(lastChanges[fileName].File);
                        Console.WriteLine(
                            $"Blames do not match: matched {correctLines} out of {size} ({correctLines * 100 / size}%) for {fileName}");
                    }
                }
            }
        });

        Console.WriteLine(
            $"Blame acurracy: matched {totalCorrectLines} / {totalLines}, {totalCorrectLines * 100 / totalLines}%");
        return (wrongFiles, notFoundFiles);
    }

    private static List<FileInfo> WalkDir(DirectoryInfo root)
    {
        FileInfo[] files = null;

        try
        {
            files = root.GetFiles("*.*");
        }
        catch (UnauthorizedAccessException)
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
            var filesFromSubDirs = subDirs.Where(it => it.Name != ".git").SelectMany(WalkDir);

            return files.Concat(filesFromSubDirs).ToList();
        }
        else
            return new List<FileInfo>();
    }

    private static void CloneRepository(string repoUrl)
    {
        RunGitCommand($"clone {repoUrl}",
            RootDirPath);
    }

    private static string RunGitCommand(string command, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = command;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = workingDirectory;

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private static string GetRelativePath(FileInfo fileInfo, string dir)
    {
        if (fileInfo.FullName.StartsWith(dir))
        {
            return fileInfo.FullName.Substring(dir.Length + 1).Replace("\\", "/");
        }

        return "";
    }
}
