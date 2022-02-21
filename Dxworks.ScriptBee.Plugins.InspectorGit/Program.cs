using System;
using Dxworks.ScriptBee.Plugins.InspectorGit.Transformers;

namespace Dxworks.ScriptBee.Plugins.InspectorGit
{
    class Program
    {
        static void Main(string[] args)
        {
            var iglogPath = "resources/honeydew.iglog";
            
            Console.WriteLine($"Reading Iglog at {iglogPath}");

            var gitlogDTO = new IGLogReader().Read(iglogPath);
            
            Console.WriteLine("Done reading Iglog");
            Console.WriteLine("Starting Transformation");
            var repository = new RepositoryTransformer().Transform(gitlogDTO, false);
            Console.WriteLine("Done Transformation");
        }
    }
}
