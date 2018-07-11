using System;
using System.Collections.Generic;
using System.IO;
using CustomLitJson;

namespace etucli
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputDir = args[0];
            if(!Directory.Exists(inputDir))
            {
                throw new Exception("[ETU] directory: " + inputDir + " not exsists");
            }
            Console.WriteLine("[ETU] generate json from directory: " + inputDir);
            // create workspace
            var workspace = inputDir + "/etu";
            if(Directory.Exists(workspace))
            {
                Directory.Delete(workspace, true);
            }
            Directory.CreateDirectory(workspace);

            //
            File.Delete($"{inputDir}/etu.json");

            // build
            var result = new EtuBuildResult();
            DataMaker.Instance.Build(inputDir, false, result);

            // write to file
            foreach (var kv in result.fileNameToJsonObject)
            {
                var name = kv.Key;
                var jd = kv.Value;
                var json = jd.ToJson();
                File.WriteAllText($"{workspace}/{name}.json", json);
            }

            // package json
            var package = new JsonData();
            foreach (var kv in result.fileNameToJsonObject)
            {
                var name = kv.Key;
                var jd = kv.Value;
                package[name] = jd;
            }
            var packageJson = package.ToJson();
            File.WriteAllText($"{inputDir}/etu.json", packageJson);

            // check fail
            if(result.failCount > 0)
            {
                throw new Exception("one or more excel file convert fail");
            }
        }
    }
}
