using System.Collections.Generic;

namespace InspectorGit.Dxworks.ScriptBee.Plugins.InspectorGit
{
    public record Log
    {
        public string Date;
        public long Timestamp;
        public List<Commit> commits;
    }

    public record Commit
    {
        public string Id;
    }
}
