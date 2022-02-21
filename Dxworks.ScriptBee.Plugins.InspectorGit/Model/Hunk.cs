using System.Collections.Generic;
using System.Linq;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class Hunk
{
    public Hunk(List<LineChange> lineChanges)
    {
        LineChanges = lineChanges;
        var splitLines = lineChanges.GroupBy(it => it.Operation).ToList();
        AddedLines = splitLines.FirstOrDefault(grouping => grouping.Key == LineOperation.Add)?.ToList() ?? new List<LineChange>();
        DeletedLines = splitLines.FirstOrDefault(grouping => grouping.Key == LineOperation.Delete)?.ToList() ?? new List<LineChange>();
    }

    public List<LineChange> LineChanges { get; }
    
    public List<LineChange> AddedLines { get; }
    
    public List<LineChange> DeletedLines { get; }
    
}