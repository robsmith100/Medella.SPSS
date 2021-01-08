using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Curiosity.SPSS.DataReader;
using Curiosity.SPSS.FileParser;
using Curiosity.SPSS.SpssDataset;
using Xunit;

namespace Curiosity.SPSS.Tests
{
    public class TestSpssReader
    {
        [Fact]
        public void TestReadFile()
        {
            var fileStream = new FileStream("TestFiles/test.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); //To enable support of 1252 encoding

            int[] varValues = { 1, 2, 1 };
            string[] streetValues = { "Landsberger Straße", "Fröbelplatz", "Bayerstraße" };

            int varCount;
            int rowCount;
            try
            {
                ReadData(fileStream, out varCount, out rowCount,
                    new Dictionary<int, Action<int, Variable>>
                    {
                        {
                            0, (i, variable) =>
                            {
                                Assert.Equal("varaible ñ", variable.Label); //Label mismatch
                                Assert.Equal(DataType.Numeric, variable.Type); //First file variable should be  a Number
                            }
                        },
                        {
                            1, (i, variable) =>
                            {
                                Assert.Equal("straße", variable.Label); //Label mismatch
                                Assert.Equal(DataType.Text, variable.Type); //Second file variable should be  a text
                            }
                        }
                    },
                    new Dictionary<int, Action<int, int, Variable, object>>
                    {
                        {
                            0, (r, c, variable, value) =>
                            {
                                // All numeric values are doubles
                                Assert.IsType<double>(value); // First row variable should be a Number
                                var v = (double) value;
                                Assert.Equal(varValues[r], v); //Int value is different
                            }
                        },
                        {
                            1, (r, c, variable, value) =>
                            {
                                Assert.IsType<string>(value); //Second row variable should be  a text
                                var v = (string) value;
                                Assert.Equal(streetValues[r], v); //String value is different
                            }
                        }
                    });
            }
            finally
            {
                fileStream.Close();
            }

            Assert.Equal(3, varCount); // Variable count does not match
            Assert.Equal(3, rowCount); // Rows count does not match
        }

        [Fact]
        public void TestEmptyStream()
        {
            Assert.Throws<SpssFileFormatException>(() => { ReadData(new MemoryStream(Array.Empty<byte>()), out _, out _); });
        }

        [Fact]
        public void TestReadMissingValuesAsNull()
        {
            var fileStream = new FileStream("TestFiles/MissingValues.sav", FileMode.Open, FileAccess.Read,
                FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

            double?[][] varValues =
            {
                new double?[] { 0, 1, 2, 3, 4, 5, 6, 7 }, // No missing values
                new double?[] { 0, null, 2, 3, 4, 5, 6, 7 }, // One mssing value
                new double?[] { 0, null, null, 3, 4, 5, 6, 7 }, // Two missing values
                new double?[] { 0, null, null, null, 4, 5, 6, 7 }, // Three missing values
                new double?[] { 0, null, null, null, null, null, 6, 7 }, // Range
                new double?[] { 0, null, null, null, null, null, 6, null }, // Range & one value
            };

            void RowCheck(int r, int c, Variable variable, object value)
            {
                Assert.Equal(varValues[c][r], value); //Wrong value: row {r}, variable {c}
            }

            try
            {
                ReadData(fileStream, out _, out _, new Dictionary<int, Action<int, Variable>>
                    {
                        { 0, (i, variable) => Assert.Equal(MissingValueType.NoMissingValues, variable.MissingValueType) },
                        { 1, (i, variable) => Assert.Equal(MissingValueType.OneDiscreteMissingValue, variable.MissingValueType) },
                        { 2, (i, variable) => Assert.Equal(MissingValueType.TwoDiscreteMissingValue, variable.MissingValueType) },
                        { 3, (i, variable) => Assert.Equal(MissingValueType.ThreeDiscreteMissingValue, variable.MissingValueType) },
                        { 4, (i, variable) => Assert.Equal(MissingValueType.Range, variable.MissingValueType) },
                        { 5, (i, variable) => Assert.Equal(MissingValueType.RangeAndDiscrete, variable.MissingValueType) },
                    },
                    new Dictionary<int, Action<int, int, Variable, object>>
                    {
                        { 0, RowCheck },
                        { 1, RowCheck },
                        { 2, RowCheck },
                        { 3, RowCheck },
                        { 4, RowCheck },
                        { 5, RowCheck },
                        { 6, RowCheck },
                    });
            }
            finally
            {
                fileStream.Close();
            }
        }

        internal static void ReadData(Stream fileStream, out int varCount, out int rowCount,
            IDictionary<int, Action<int, Variable>>? variableValidators = null, IDictionary<int, Action<int, int, Variable, object>>? valueValidators = null)
        {
            var spssDataSet = new SpssReader(fileStream);

            varCount = 0;
            rowCount = 0;

            var variables = spssDataSet.Variables;
            foreach (var variable in variables)
            {
                Debug.WriteLine("{0} - {1}", variable.Name, variable.Label);
                foreach (var (key, value) in variable!.ValueLabels!) Debug.WriteLine(" {0} - {1}", key, value);

                if (variableValidators != null && variableValidators.TryGetValue(varCount, out var checkVariable)) checkVariable(varCount, variable);

                varCount++;
            }

            foreach (var record in spssDataSet.Records)
            {
                var varIndex = 0;
                foreach (var variable in variables)
                {
                    Debug.Write(variable.Name);
                    Debug.Write(':');
                    var value = record.GetValue(variable);
                    Debug.Write(value);
                    Debug.Write('\t');

                    if (valueValidators != null && valueValidators.TryGetValue(varIndex, out var checkValue)) checkValue(rowCount, varIndex, variable, value!);

                    varIndex++;
                }

                Debug.WriteLine("");

                rowCount++;
            }
        }
    }
}