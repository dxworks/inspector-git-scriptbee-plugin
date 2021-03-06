using System.Collections.Generic;
using DxWorks.ScriptBee.Plugin.Api;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class Repository : ScriptBeeModel
{
    public string Name { get; init; }
    public List<Commit> Commits { get; set; }
    public List<File> Files { get; set; }
    public List<Account> Accounts { get; set; }
}
