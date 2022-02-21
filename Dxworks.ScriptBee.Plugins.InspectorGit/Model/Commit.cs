using System;
using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class Commit : ReferenceEntity
{
    public Repository Repository { get; set; }
    public string Id { get; set; }
    public string Message { get; set; }
    public DateTime AuthorDate { get; set; }
    public DateTime CommitterDate { get; set; }
    public Account Author { get; set; }
    public Account Committer { get; set; }
    public List<Commit> Parents { get; set; }
    public List<Commit> Children { get; set; }
    public List<Change> Changes { get; set; }
    public long BranchId { get; set; }
    public long RepoSize { get; set; }
    
    public bool IsMerge => Parents.Count > 1;
    public bool IsSplit => Children.Count > 1;

    protected bool Equals(Commit other)
    {
        return Repository.Name == other.Repository.Name && Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Commit) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Repository.Name, Id);
    }
}