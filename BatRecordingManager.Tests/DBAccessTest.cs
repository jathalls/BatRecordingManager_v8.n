// <copyright file="DBAccessTest.cs" company="Echolocation.org">Copyright ©  2015 (C) 2017</copyright>
using System;
using BatRecordingManager;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatRecordingManager.Tests
{
    /// <summary>This class contains parameterized unit tests for DBAccess</summary>
    [PexClass(typeof(DBAccess))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class DBAccessTest
    {
        /// <summary>Test stub for GetLocationList()</summary>
        [PexMethod]
        internal BulkObservableCollection<string> GetLocationListTest()
        {
            BatReferenceDBLinqDataContext batReferenceDataContext =
                new BatReferenceDBLinqDataContext(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + "TestDatabase.mdf" + @";Integrated Security=False;Connect Timeout=60");

            BulkObservableCollection<string> result = DBAccess.GetLocationList();
            return result;
            // TODO: add assertions to method DBAccessTest.GetLocationListTest()
        }
    }
}
