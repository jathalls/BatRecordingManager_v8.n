using Microsoft.VisualStudio.TestTools.UnitTesting;
using BatRecordingManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;

namespace BatRecordingManager.Tests
{
    [TestClass()]
    public class ReportByFrequencyFrequencyDataTests
    {
        [TestMethod()]
        public void SetDataTest()
        {
            BulkObservableCollection<BatStatistics> reportBatStatsList=new BulkObservableCollection<BatStatistics>();
            BulkObservableCollection<RecordingSession> reportSessionList=new BulkObservableCollection<RecordingSession>();
            BulkObservableCollection<Recording> reportRecordingList=new BulkObservableCollection<Recording>();

            ReportMaster rbf=new ReportByFrequency();
            rbf.SetData(reportBatStatsList,reportSessionList,reportRecordingList);

            Assert.Fail();
        }
    }
}