using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyticsBridgeTableSpoofer
{
    class Program
    {
        static int NumberOfFilesLimit = -1;
        const int NumberOfBranches = 1;

        const int StartingBuildPipelineSK = 123;
        const int StartingProjectSK = 1234;
        const int StartingBranchSK = 123456;

        static void Main(string[] args)
        {
            var folders = Directory.EnumerateDirectories(@"C:\VSO\src", "*.*", SearchOption.AllDirectories)
               .Where(folder => !folder.StartsWith(@"C:\VSO\src\."))
               .Select(folder => folder.Remove(0, @"C:\VSO\src\".Length).Replace('\\', '/'))
               .ToList();

            var files = Directory.EnumerateFiles(@"C:\VSO\src", "*.*", SearchOption.AllDirectories)
               .Where(file => new string[] { ".cs", ".tsx", ".ts" }
               .Contains(Path.GetExtension(file)))
               .Select(file => file.Remove(0, @"C:\VSO\src\".Length).Replace('\\', '/'))
               .ToList();

            if (NumberOfFilesLimit != -1)
            {
                files = files.Take(NumberOfFilesLimit).ToList();
            }

            Dictionary<long, Dictionary<string, long>> parentSKMapping = new Dictionary<long, Dictionary<string, long>>();

            using (StreamWriter dimensionFileStream = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), $"FileDimension{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.csv")),
                bridgeDimensionFileStream = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), $"BridgeFileDimension{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.csv")))
            {
                dimensionFileStream.WriteLine("FileSK, BranchSK, ProjectSK, FullPath");
                dimensionFileStream.Flush();

                var currentfileSK = 0;
                var startingDate = DateTime.Parse("2019-01-01");

                // First Insert All files for each active branch
                for (int currentBranchIndex = 0; currentBranchIndex < NumberOfBranches; currentBranchIndex++)
                {
                    foreach (var file in files)
                    {
                        //lastKnownSk[currentBranchIndex][file] = currentfileSK;

                        var dimLine = string.Format("{0},{1},{2},{3}",
                            currentfileSK,
                            StartingBranchSK + currentBranchIndex,
                            StartingProjectSK,
                            file);

                        dimensionFileStream.WriteLine(dimLine);
                        dimensionFileStream.Flush();

                        currentfileSK++;
                    }
                }

                bridgeDimensionFileStream.WriteLine("FileSK, BranchSK, ProjectSK, FullPath, ParentPathSK, IsFile");

                var currentBridgeTableSK = 0;

                // First Insert All files for each active branch
                for (int currentBranchIndex = 0; currentBranchIndex < NumberOfBranches; currentBranchIndex++)
                {
                    parentSKMapping[currentBranchIndex] = new Dictionary<string, long>();

                    foreach (var folder in folders)
                    {
                        parentSKMapping[currentBranchIndex][folder] = currentBridgeTableSK;

                        int parentCount = folder.Count(character => character == '/');

                        var fileParts = folder.Split('/');

                        if (parentCount == 0)
                        {
                            var dimLine = string.Format("{0},{1},{2},{3},{4},{5}",
                            currentBridgeTableSK,
                            StartingBranchSK + currentBranchIndex,
                            StartingProjectSK,
                            folder,
                            null,
                            false);

                            bridgeDimensionFileStream.WriteLine(dimLine);
                            bridgeDimensionFileStream.Flush();

                            currentBridgeTableSK++;
                        }

                        for (int currentParentNumber = parentCount; currentParentNumber > 0; currentParentNumber--)
                        {
                            var currentParent = String.Join("/", fileParts.Take(currentParentNumber).ToArray());

                            var dimLine = string.Format("{0},{1},{2},{3},{4},{5}",
                            currentBridgeTableSK,
                            StartingBranchSK + currentBranchIndex,
                            StartingProjectSK,
                            folder,
                            parentSKMapping[currentBranchIndex][currentParent],
                            false);

                            bridgeDimensionFileStream.WriteLine(dimLine);
                            bridgeDimensionFileStream.Flush();

                            currentBridgeTableSK++;
                        }
                    }

                    foreach (var file in files)
                    {
                        int parentCount = file.Count(character => character == '/');

                        var fileParts = file.Split('/');

                        for (int currentParentNumber = parentCount; parentCount > 0; parentCount--)
                        {
                            var currentParent = String.Join("/", fileParts.Take(currentParentNumber).ToArray());

                            var dimLine = string.Format("{0},{1},{2},{3},{4},{5}",
                            currentBridgeTableSK,
                            StartingBranchSK + currentBranchIndex,
                            StartingProjectSK,
                            file,
                            parentSKMapping[currentBranchIndex][currentParent],
                            true);

                            bridgeDimensionFileStream.WriteLine(dimLine);
                            bridgeDimensionFileStream.Flush();

                            currentBridgeTableSK++;
                        }
                    }
                }
                
            }
        }
    }
}
