using Xunit;
using RotinaClone.Domain.Models;

namespace RotinaClone.Tests
{
    public class CloningTests
    {
        [Fact]
        public void TestCloneOptionsDefaultValues()
        {
            var options = new CloneOptions();
            
            // Verify default safety simulation switch is ON
            Assert.True(options.IsSimulation);
            Assert.True(options.IsIntelligent);
            Assert.False(options.IsSectorBySector);
            Assert.True(options.UseVss);
            Assert.True(options.Align4K);
        }

        [Fact]
        public void TestPartitionInfoCalculation()
        {
            var partition = new PartitionInfo
            {
                TotalSize = 100L * 1024 * 1024 * 1024, // 100GB
                UsedSize = 30L * 1024 * 1024 * 1024   // 30GB
            };

            Assert.Equal(100.0, partition.TotalSizeGB);
            Assert.Equal(30.0, partition.UsedSizeGB);
            Assert.Equal(70.0, partition.FreeSizeGB);
            Assert.Equal(30.0, partition.UsedPercent);
        }
    }
}
