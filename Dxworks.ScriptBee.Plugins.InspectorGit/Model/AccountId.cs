using Dxworks.ScriptBee.Plugins.InspectorGit.Model;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public record AccountId(string Name, string Email)
{
    public override string ToString()
    {
        return $"{Name} <{Email}>";
    }
}