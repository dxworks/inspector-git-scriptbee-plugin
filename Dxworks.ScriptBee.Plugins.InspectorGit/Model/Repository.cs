using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class Repository
{
    public string Name { get; init; }
    public List<Commit> Commits { get; set; }
    public List<File> Files { get; set; }
    public List<Account> Accounts { get; set; }
}