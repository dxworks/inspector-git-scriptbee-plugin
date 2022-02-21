using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class File
{
    public bool Binary { get; set; }
    public List<Change> Changes { get; set; }
    public Repository Repository { get; init; }

    public readonly string Uuid = Guid.NewGuid().ToString();

    public string Path(Commit? commit = null) => GetLastChange(commit)?.NewFileName ?? "";
    public bool Alive(Commit? commit = null)
    {
        var lastChange = GetLastChange(commit);
        return lastChange != null && lastChange.Type != ChangeType.Delete;
    }


    public Change? GetLastChange(Commit? commit = null)
    {
        if (Changes.Count == 0)
            return null;
        if (commit == null)
            return Changes.Last();
        return GetLastChangeRecursively(commit);
    }

    private Change? GetLastChangeRecursively(Commit commit)
    {
        var change = Changes.Find(it => Equals(it.Commit, commit));
        if (change != null)
            return change;

        // it is safe to only consider going on the first parent, because
        // if the file was changed just on some of the parents, it appears as a change in the merge commit
        var parent = commit.Parents.FirstOrDefault();
        if (parent == null)
            return null;

        return GetLastChangeRecursively(parent);
    }

    protected bool Equals(File other)
    {
        return Uuid == other.Uuid && Binary == other.Binary;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((File) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Uuid, Binary);
    }

    public override string ToString()
    {
        return $"{Path()}";
    }
}