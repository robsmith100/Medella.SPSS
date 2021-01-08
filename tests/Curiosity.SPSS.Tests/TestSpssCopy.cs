using System.IO;
using Curiosity.SPSS.DataReader;
using Xunit;

namespace Curiosity.SPSS.Tests
{
    public class TestSpssCopy
    {
        [Fact]
        public void TestCopyFile()
        {
            using (var fileStream =
                new FileStream("TestFiles/cakespss1000similarvars.sav", FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read, 2048 * 10, FileOptions.SequentialScan))
            {
                using var writeStream = new FileStream("TestFiles/ourcake1000similarvars.sav", FileMode.Create, FileAccess.Write);
                var spssDataSet = new SpssReader(fileStream);

                var spssWriter = new SpssWriter(writeStream, spssDataSet.Variables);

                foreach (var record in spssDataSet.Records)
                {
                    var newRecord = spssWriter.CreateRecord(record);
                    spssWriter.WriteRecord(newRecord);
                }

                spssWriter.EndFile();
            }

            Assert.True(true); // To check errors, set <DeleteDeploymentDirectoryAfterTestRunIsComplete> to False and open the file
        }
    }
}