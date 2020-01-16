using NUnit.Framework;
using System.Threading;

namespace BatRecordingManager.Tests
{
    [Apartment(ApartmentState.STA)]
    [TestFixture()]
    public class AppFilterFrequencyDataTests
    {
        [Test()]
        public void AppFilterTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ApplyFilterTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ContainsKeywordsTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ContainsKeywordTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetCommentsForFileTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ChangeExtensionToTxtTest()
        {
            AppFilter appFilter = SetUpAppFilter();
            string f = null;
            string r=appFilter.ChangeExtensionToTxt(f);
            Assert.IsNull(r,"does not return null for null");
            f = "";
            r = appFilter.ChangeExtensionToTxt(f);
            Assert.IsEmpty(r,"Does not return empty for empty");
            f = @"C:\BRMTestData\File";
            r = appFilter.ChangeExtensionToTxt(f);
            Assert.AreEqual(f+".txt",r,"extensionless does not get .txt added");
            f = @"C:\BRMTextData\File.wav";
            Assert.AreEqual(@"C:\BRMTextData\File.txt",r,"Does not replace .wav with .txt");
            
        }

        private AppFilter SetUpAppFilter()
        {
            AppFilter af=new AppFilter();
            return (af);
        }
    }
}