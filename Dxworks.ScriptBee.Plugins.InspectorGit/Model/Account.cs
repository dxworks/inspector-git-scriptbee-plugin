using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model
{
    public record Account(AccountId AccountId,
                          List<Commit> Commits)
    {
    }
}