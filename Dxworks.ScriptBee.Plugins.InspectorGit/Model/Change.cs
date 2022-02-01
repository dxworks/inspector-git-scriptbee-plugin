
using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model
{
    public record Change(Commit Commit,
                         ChangeType ChangeType,
                         string OldFileName,
                         string NewFileName,
                         File File,
                         Commit ParentCommit,
                         List<Hunk> Hunks,
                         List<Commit> annotatedLines);
}