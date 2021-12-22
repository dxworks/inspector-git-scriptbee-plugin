using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.DTO;

public record GitLogDTO(string IGLogVersion, List<CommitDTO> Commits);

public record CommitDTO(string Id,
    string[] ParentIds,
    string AuthorDate,
    string AuthorEmail,
    string AuthorName,
    string CommitterDate,
    string CommitterEmail,
    string CommitterName,
    string Message,
    List<ChangeDTO> Changes);

public record ChangeDTO(string OldFileName, string NewFileName, ChangeType Type, string ParentCommitId, bool Binary,
    List<HunkDTO> Hunks);

public record HunkDTO
{
    public HunkType Type;
    public List<LineChangeDTO> AddedLineChanges;
    public List<LineChangeDTO> DeletedLineChanges;
    public List<LineChangeDTO> LineChanges;
}

public record LineChangeDTO
{
    public LineOperationDTO Operation;
    public int Number;
    public string Content;
}

public enum LineOperationDTO
{
    Add,
    Delete
}

public enum ChangeType
{
    Add,
    Delete,
    Modify,
    Rename
}

public enum HunkType
{
    Add,
    Delete,
    Modify
}