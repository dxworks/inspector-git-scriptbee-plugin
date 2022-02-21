using Dxworks.ScriptBee.Plugins.InspectorGit.Model;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class LineChange : ReferenceEntity
{
    public LineOperation Operation { get; init; }
    public int number { get; init; }
    public Commit Commit { get; init; }
}