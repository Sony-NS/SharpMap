﻿using System.IO;
using System.Linq;
using DelftTools.TestUtils;
using DelftTools.Utils.IO;
using log4net;
using NUnit.Framework;

namespace DelftTools.Tests
{
    [TestFixture]
    public class ProjectStructureValidation
    {
        private static ILog log = LogManager.GetLogger(typeof(ProjectStructureValidation));
        private static string[] ignoreList = new[]{".svn","bin"};
        [Test]
        [Ignore("WIP")]
        public void ValidateTestTreeHasSameStructureAsSrc()
        {
            LogHelper.ConfigureLogging();
            ValidateDirectoriesAreMirrored(@"..\..\..\..\..\src", @"..\..\..\..\..\test");
        }

        private static void ValidateDirectoriesAreMirrored(string sourceDir,string mirrorDir)
        {
            //brearth first directory structure matching
            //validate local directory structure
            //check if each dir of the mirror is available in source
            var relativeSourceSubDirectories = FileUtils.GetDirectoriesRelative(sourceDir);
            var relativeMirrorSubDirectories = FileUtils.GetDirectoriesRelative(mirrorDir); 
            log.DebugFormat("Comparing {0} to {1}", sourceDir, mirrorDir);
            
            foreach (string mirrorSubDir in relativeMirrorSubDirectories)
            {
                if (!relativeSourceSubDirectories.Contains(mirrorSubDir))
                    Assert.Fail("Directory \n {0} \n does not exist in \n {1} \n but does exist in mirror \n {2} ",Path.GetFullPath(Path.Combine(mirrorDir,mirrorSubDir))
                        ,Path.GetFullPath(sourceDir),Path.GetFullPath(mirrorDir));
            }
            //traverse into the subdirs of the mirror which are not in our ignore list
            var subdirs = from d in Directory.GetDirectories(mirrorDir)
                          where !ignoreList.Any(d.EndsWith)
                          select d;
            foreach (string mirrorSubDir in subdirs)
            {
                var sourceSubDir = mirrorSubDir.Replace(mirrorDir, sourceDir);
                ValidateDirectoriesAreMirrored(sourceSubDir,mirrorSubDir);
            }
        }
    }
}
