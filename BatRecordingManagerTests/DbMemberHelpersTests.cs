using Microsoft.VisualStudio.TestTools.UnitTesting;
using BatRecordingManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatRecordingManager.Tests
{
    [TestClass()]
    public class DbMemberHelpersTests
    {
        [TestMethod()]
        public void FrequencyContributionsEmptySegmentTest()
        {
            LabelledSegment segment=new LabelledSegment	();
            List<int> blocks=new List<int>();
            int BlockSize = 10;
            int start = 12 * 60;

            bool result = segment.FrequencyContributions(out int FirstBlock, out blocks, start, BlockSize);

            Assert.IsFalse(result);

        }

        [TestMethod()]
        public void FrequencyContributionsSimpleTest()
        {
            LabelledSegment segment = new LabelledSegment();
            List<int> blocks = new List<int>();
            int BlockSize = 10;
            int start = 12 * 60;

            Recording recording=new Recording();
            recording.RecordingStartTime=new TimeSpan(20,0,0);
            recording.RecordingEndTime=new TimeSpan(22,0,0);

            segment.Recording = recording;
            segment.StartOffset=new TimeSpan();
            segment.EndOffset=new TimeSpan(0,4,0);


            bool result = segment.FrequencyContributions(out int FirstBlock, out blocks, start, BlockSize);

            Assert.IsTrue(result);
            Assert.AreEqual(8*6,FirstBlock,$"First block should be 20*6=120 but was {FirstBlock}");
            Assert.AreEqual(1,blocks.Count,$"expected 1 block, but got {blocks.Count}");
            Assert.AreEqual(4,blocks[0],$"Expected first and only block to contain 4 but got {blocks[0]}");

        }

        [TestMethod()]
        public void FrequencyContributionsShortOverlapTest()
        {
            LabelledSegment segment = new LabelledSegment();
            List<int> blocks = new List<int>();
            int BlockSize = 10;
            int start = 12 * 60;

            Recording recording = new Recording();
            recording.RecordingStartTime = new TimeSpan(21, 37, 05);
            recording.RecordingEndTime = new TimeSpan(21, 37, 16);

            segment.Recording = recording;
            segment.StartOffset = new TimeSpan(0,2,48); // actually 21:39:53
            segment.EndOffset = new TimeSpan(0, 4, 10); // actually 21:41:15


            bool result = segment.FrequencyContributions(out int FirstBlock, out blocks, start, BlockSize);

            Assert.IsTrue(result);
            Assert.AreEqual((9*6)+3, FirstBlock, $"First block should be 20*6=120 but was {FirstBlock}");
            Assert.AreEqual(2, blocks.Count, $"expected 2 block, but got {blocks.Count}");
            Assert.AreEqual(1, blocks[0], $"Expected first and only block to contain 4 but got {blocks[0]}");
            Assert.AreEqual(2,blocks[1],$"second block size error");

        }

        [TestMethod()]
        public void FrequencyContributionsLongOverlapTest()
        {
            LabelledSegment segment = new LabelledSegment();
            List<int> blocks = new List<int>();
            int BlockSize = 10;
            int start = 12 * 60;

            Recording recording = new Recording();
            recording.RecordingStartTime = new TimeSpan(21, 37, 05);
            recording.RecordingEndTime = new TimeSpan(21, 55, 10); // duration=0:18:05

            segment.Recording = recording;
            segment.StartOffset = new TimeSpan(0,1,10); // actually 21:38:15
            segment.EndOffset = new TimeSpan(0, 14, 03); // actually 21:51:08


            bool result = segment.FrequencyContributions(out int FirstBlock, out blocks, start, BlockSize);

            Assert.IsTrue(result);
            Assert.AreEqual((9*6)+3, FirstBlock, $"First block should be 20*6=120 but was {FirstBlock}");
            Assert.AreEqual(3, blocks.Count, $"expected 3 block, but got {blocks.Count}");
            Assert.AreEqual(2, blocks[0], $"Expected first and only block to contain 4 but got {blocks[0]}");
            Assert.AreEqual(10,blocks[1],"second block");
            Assert.AreEqual(2,blocks[2],"third block");

        }
    }
}