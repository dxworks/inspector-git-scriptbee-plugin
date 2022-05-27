using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DxWorks.ScriptBee.Plugin.Api;
using Dxworks.ScriptBee.Plugins.InspectorGit.Transformers;

namespace Dxworks.ScriptBee.Plugins.InspectorGit;

public class ScriptBeeModelLoader : IModelLoader
{
    public string GetName() => "InspectorGit";

    public Task<Dictionary<string, Dictionary<string, ScriptBeeModel>>> LoadModel(List<Stream> fileStreams,
        Dictionary<string, object> configuration = null, CancellationToken cancellationToken = default)
    {
        var models = new Dictionary<string, Dictionary<string, ScriptBeeModel>>();
        var repositoryDictionary = new Dictionary<string, ScriptBeeModel>();
        // authors
        // commits
        // files

        foreach (var fileStream in fileStreams)
        {
            var name = Guid.NewGuid().ToString();
            var gitLogDto = new IGLogReader().Read(name, fileStream);
            var repository = new RepositoryTransformer().Transform(gitLogDto);

            repositoryDictionary.Add(repository.Name, repository);
        }

        models.Add("Repository", repositoryDictionary);

        return Task.FromResult(models);
    }
}
