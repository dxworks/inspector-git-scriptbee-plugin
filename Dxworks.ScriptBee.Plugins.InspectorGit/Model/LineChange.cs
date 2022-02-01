namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model
{
    public record LineChange(LineOperation Operation,
                             int number,
                             Commit Commit)
    {
    }
}