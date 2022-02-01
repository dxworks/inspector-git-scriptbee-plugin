using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model
{
    public record File(bool Binary,
                       List<Change> Changes)
    {
    }
}