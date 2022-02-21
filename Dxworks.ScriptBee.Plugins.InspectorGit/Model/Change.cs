using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class Change : ReferenceEntity
{
    public Commit Commit { get; set; }
    public ChangeType Type { get; set; }
    public string OldFileName { get; set; }
    public string NewFileName { get; set; }
    public File File { get; set; }
    public Commit? ParentCommit { get; set; }
    public Change? ParentChange { get; set; }
    public List<Hunk> Hunks { get; set; }
    public List<Commit> AnnotatedLines { get; set; } = new();

    private ImmutableList<LineChange> _lineChanges;
    private ImmutableList<LineChange> _addedLines;
    private ImmutableList<LineChange> _deletedLines;

    public ImmutableList<LineChange> LineChanges =>
        _lineChanges ??= Hunks.SelectMany(it => it.LineChanges).ToImmutableList();

    public ImmutableList<LineChange> AddedLines =>
        _addedLines ??= Hunks.SelectMany(it => it.AddedLines).ToImmutableList();

    public ImmutableList<LineChange> DeletedLines =>
        _deletedLines ??= Hunks.SelectMany(it => it.DeletedLines).ToImmutableList();

    protected bool Equals(Change other)
    {
        return Equals(Commit, other.Commit) &&
               Type == other.Type &&
               OldFileName == other.OldFileName &&
               NewFileName == other.NewFileName;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Change) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Commit, (int) Type, OldFileName, NewFileName);
    }
}