using System;
using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model
{
    public record class Commit(string Id,
                               string Message,
                               DateTime AuthorDate,
                               DateTime CommiterDate,
                               Account Author,
                               Account Committer,
                               List<Commit> parents,
                               List<Commit> children,
                               List<Change> Changes,
                               long BranchId,
                               long RepoSize)
    {
    }
}