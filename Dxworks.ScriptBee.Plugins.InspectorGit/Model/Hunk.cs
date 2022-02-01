using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model
{
    public record Hunk(List<LineChange> LineChanges)
    {
    }
}