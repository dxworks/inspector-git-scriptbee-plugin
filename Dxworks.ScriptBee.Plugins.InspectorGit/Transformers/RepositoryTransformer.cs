#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dxworks.ScriptBee.Plugins.InspectorGit.DTO;
using Dxworks.ScriptBee.Plugins.InspectorGit.Model;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Transformers;

public class RepositoryTransformer
{
    const string DateTimeFormat = "ddd MMM d HH:mm:ss yyyy K";

    public IDictionary<string, Account> AccountRegistry = new Dictionary<string, Account>();
    public IDictionary<string, File> FileRegistry = new Dictionary<string, File>();
    public IDictionary<string, Commit> CommitRegistry = new Dictionary<string, Commit>();

    public Repository Transform(GitLogDTO gitLogDto, bool computeAnnotatedLines = true)
    {
        Repository repository = new Repository {Name = gitLogDto.Name};

        var commitsCount = gitLogDto.Commits.Count;

        repository.Commits = gitLogDto.Commits.Select((commitDto, index) =>
        {
            Console.Write($"Creating commit {index + 1} / {commitsCount} ({(index + 1) * 100 / commitsCount}%)\r");
            return TransformCommit(commitDto);
        }).ToList();

        Commit TransformCommit(CommitDTO commitDto)
        {
            var authorDate = DateTime.ParseExact(commitDto.AuthorDate, DateTimeFormat, null);
            var committerDate = commitDto.CommitterDate.Length == 0
                ? authorDate
                : DateTime.ParseExact(commitDto.CommitterDate, DateTimeFormat, null);
            var parents = ExtractParents(commitDto);

            var author = getAccount(commitDto.AuthorName, commitDto.AuthorEmail, repository);
            var committer = string.IsNullOrEmpty(commitDto.CommitterName)
                ? author
                : getAccount(commitDto.CommitterName, commitDto.CommitterEmail, repository);
            var commit = new Commit
            {
                Repository = repository,
                Id = commitDto.Id,
                Message = commitDto.Message,
                AuthorDate = authorDate,
                CommitterDate = committerDate,
                Author = author,
                Committer = committer,
                Parents = parents,
                Children = new List<Commit>(),
                Changes = new List<Change>()
            };

            commit.Parents.ForEach(it => it.Children.Add(commit));
            CommitRegistry[commit.Id] = commit;
            author.Commits.Add(commit);
            if (!committer.AccountId.Equals(author.AccountId))
            {
                committer.Commits.Add(commit);
            }

            AddChangesToCommit(commitDto.Changes, commit, repository, computeAnnotatedLines);

            return commit;
        }

        repository.Accounts = AccountRegistry.Values.ToList();
        repository.Files = FileRegistry.Values.ToList();
        return repository;
    }

    private void AddChangesToCommit(List<ChangeDTO> changeDtos, Commit commit, Repository repository,
        bool computeAnnotatedLines)
    {
        if (!commit.IsMerge)
        {
            commit.Changes = changeDtos
                .Select(TransformChange)
                .Where(it => it != null)
                .Select(it => it!)
                .ToList();
        }
        else
        {
            commit.Changes = changeDtos.GroupBy(it => it.Type == ChangeType.Delete ? it.OldFileName : it.NewFileName)
                .SelectMany(grouping =>
                {
                    var changes = grouping
                        .Select(TransformChange)
                        .Where(it => it != null)
                        .Select(it => it!)
                        .ToList();
                    if (changes.Count == 0) return new List<Change>();
                    else
                    {
                        var missingChange = GetMissingChange(changes);
                        if (computeAnnotatedLines)
                            FixAnnotatedLines(changes, missingChange, commit);
                        MergeFiles(changes, missingChange);
                        return changes;
                    }
                }).ToList();
        }

        commit.Changes.ForEach(it => it.File.Changes.Add(it));

        void FixAnnotatedLines(List<Change> changes, Change? missingChange, Commit commit1)
        {
            if (missingChange != null)
                changes.First().AnnotatedLines = missingChange.AnnotatedLines;

            var annotatedFiles = changes.Select(it => it.AnnotatedLines).ToList();

            for (var i = 0; i < annotatedFiles.First().Count; i++)
            {
                var currentAnnotatedLines = annotatedFiles.Select(it => it[i]).ToList();
                var firstAnnotatedLine = currentAnnotatedLines.First();
                currentAnnotatedLines.RemoveAt(0);
                if (Equals(firstAnnotatedLine, commit))
                {
                    var find = currentAnnotatedLines.Find(it => !Equals(it, commit));
                    if(find != null) 
                        annotatedFiles[0][i] = find;
                }
            }

            var firstChange = changes.First();
            var restOfChanges = changes.ToList();
            restOfChanges.RemoveAt(0);
            restOfChanges.ForEach(it => it.AnnotatedLines = firstChange.AnnotatedLines);
        }

        void MergeFiles(List<Change> changes, Change? missingChange)
        {
            var files = changes.Where(it => it.File != null).Select(it => it.File).Distinct().ToList();
            if (missingChange?.File != null)
            {
                files.Add(missingChange.File);
                files = files.Distinct().ToList();
            }

            if (files.Count > 1)
            {
                var allFileChanges = files.SelectMany(it => it.Changes).Distinct().ToList();
                var file = files.First();
                file.Changes = allFileChanges.OrderBy(it => it.Commit.CommitterDate).ToList();
                allFileChanges.ForEach(it => it.File = file);

                var list = files.ToList();
                list.RemoveAt(0);
                list.ForEach(it => FileRegistry.Remove(it.Uuid));
            }
        }

        Change? GetMissingChange(List<Change> changes)
        {
            if (changes.Count < commit.Parents.Count && !changes.TrueForAll(it => it.Type == ChangeType.Delete))
            {
                var cleanParent =
                    commit.Parents.First(it => !changes.Any(change => it.Equals(change.ParentCommit)))!;
                return GetLastChangeRecursively(cleanParent, changes.First().NewFileName);
            }
            else
                return null;
        }

        Change? TransformChange(ChangeDTO changeDto)
        {
            var parentCommit = string.IsNullOrEmpty(changeDto.ParentCommitId)
                ? null
                : commit.Parents.Find(it => it.Id == changeDto.ParentCommitId)!;
            try
            {
                var lastChange = GetLastChange(changeDto, parentCommit);
                var file = GetFileForChange(changeDto, lastChange, repository);
                var hunks = getHunks(changeDto, lastChange, commit);
                var change = new Change
                {
                    Commit = commit,
                    Type = changeDto.Type,
                    OldFileName = changeDto.OldFileName,
                    NewFileName = changeDto.NewFileName,
                    ParentCommit = parentCommit,
                    ParentChange = lastChange,
                    File = file,
                    Hunks = hunks,
                };
                if (computeAnnotatedLines)
                    change.AnnotatedLines = computeAnnotatedLinesForChange(change);

                return change;
            }
            catch (NoChangeException e)
            {
                Console.Error.WriteLine(e.Message);
                return null;
            }
        }
    }

    private List<Commit> computeAnnotatedLinesForChange(Change change)
    {
        if (change.File.Binary || change.ParentChange == null)
            return new List<Commit>();

        try
        {
            var newAnnotatedLines = change.ParentChange?.AnnotatedLines.ToList() ?? new List<Commit>();
            var deletes = change.DeletedLines;
            var adds = change.AddedLines;

            deletes.OrderByDescending(it => it.number).ToList()
                .ForEach(it => newAnnotatedLines.RemoveAt(it.number - 1));
            adds.ForEach(it => newAnnotatedLines.Insert(it.number - 1, it.Commit));
            return newAnnotatedLines;
        }
        catch (ArgumentOutOfRangeException e)
        {
            change.File.Binary = true;
            Console.Error.WriteLine($"Applying change to {change.NewFileName} failed. File will be considered binary");
            return new List<Commit>();
        }
    }

    private List<Hunk> getHunks(ChangeDTO changeDto, Change? lastChange, Commit commit)
    {
        if (lastChange != null && lastChange.File.Binary)
            return new List<Hunk>();

        return changeDto.Hunks.Select(hunkDto => new Hunk
        (
            hunkDto.LineChanges.Select(lineChangeDto => new LineChange
            {
                number = lineChangeDto.Number,
                Operation = lineChangeDto.Operation,
                Commit = commit
            }).ToList()
        )).ToList();
    }

    private File GetFileForChange(ChangeDTO changeDto, Change? lastChange, Repository repository)
    {
        if (changeDto.Type == ChangeType.Add)
        {
            var newFile = new File
            {
                Binary = changeDto.Binary,
                Repository = repository,
                Changes = new List<Change>()
            };

            FileRegistry[newFile.Uuid] = newFile;
            return newFile;
        }
        else
        {
            return lastChange!.File;
        }
    }

    private Change? GetLastChange(ChangeDTO changeDto, Commit? parentCommit)
    {
        if (changeDto.Type == ChangeType.Add)
            return null;

        return GetLastChangeRecursively(parentCommit, changeDto.OldFileName);
    }

    private Change? GetLastChangeRecursively(Commit? parentCommit, string fileName)
    {
        if (parentCommit == null)
        {
            throw new NoChangeException(fileName);
        }

        var change = parentCommit.Changes.Find(it => it.NewFileName == fileName);
        if (change != null)
            return change;

        // we only recurse after the first parent because if the file is not changed in the first parent,
        // but changed in any of the others, it appears as change in the merge commit.
        return GetLastChangeRecursively(parentCommit.Parents.FirstOrDefault(), fileName);
    }

    private Account getAccount(string name, string email, Repository repository)
    {
        var accountId = new AccountId(name, email);
        var accountKey = accountId.ToString();
        if (!AccountRegistry.ContainsKey(accountKey))
        {
            var account = new Account
            {
                AccountId = accountId,
                Repository = repository,
                Commits = new List<Commit>()
            };
            AccountRegistry[accountKey] = account;
            return account;
        }
        else
        {
            return AccountRegistry[accountKey];
        }
    }

    private List<Commit> ExtractParents(CommitDTO commitDto)
    {
        return commitDto.ParentIds.Where(it => CommitRegistry.ContainsKey(it)).Select(it => CommitRegistry[it])
            .ToList();
    }
}

internal class NoChangeException : Exception
{
    public NoChangeException(string fileName) : base($"Could not find change for file {fileName}")
    {
    }
}