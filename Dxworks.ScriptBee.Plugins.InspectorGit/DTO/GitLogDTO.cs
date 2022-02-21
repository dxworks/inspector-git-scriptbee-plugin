using System.Collections.Generic;
using System.Linq;
using Dxworks.ScriptBee.Plugins.InspectorGit.Model;
using Newtonsoft.Json;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.DTO;

public record GitLogDTO(string IGLogVersion, string Name,  List<CommitDTO> Commits);

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

public record HunkDTO(List<LineChangeDTO> AddedLineChanges, List<LineChangeDTO> DeletedLineChanges)
{
    public HunkType Type
    {
        get
        {
            if (!AddedLineChanges.Any()) return HunkType.Delete;
            if (!DeletedLineChanges.Any()) return HunkType.Add;
            return HunkType.Modify;
        }
    }

    public List<LineChangeDTO> LineChanges => AddedLineChanges.Concat(DeletedLineChanges).ToList();
}

public record LineChangeDTO(LineOperation Operation, int Number, string Content);

