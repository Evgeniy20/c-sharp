using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.ComponentModel;
using System.Threading;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubNubMessaging.Core;

namespace PubNubMessaging.Tests
{
    [TestFixture]
    public class WhenAuditIsRequested
    {
        ManualResetEvent auditManualEvent = new ManualResetEvent(false);
        bool receivedAuditMessage = false;

        [Test]
        public void ThenSubKeyLevelShouldReturnSuccess()
        {
            receivedAuditMessage = false;

            Pubnub pubnub = new Pubnub(PubnubKey.PublishKey, PubnubKey.SubscribeKey, PubnubKey.SecretKey, "", false);

            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "WhenAuditIsRequested";
            unitTest.TestCaseName = "ThenAccessToSubKeyShouldReturnSuccess";
            pubnub.PubnubUnitTest = unitTest;

            string channel = "hello_my_channel";

            pubnub.GrantAccess<string>(channel, true, true, 5, AccessToSubKeyCallback, DummyErrorCallback);
            Thread.Sleep(1000);

            auditManualEvent.WaitOne();

            Assert.IsTrue(receivedAuditMessage, "WhenAuditIsRequested -> ThenAccessToSubKeyShouldReturnSuccess failed.");

        }

        void AccessToSubKeyCallback(string receivedMessage)
        {
            try
            {
                if (!string.IsNullOrEmpty(receivedMessage) && !string.IsNullOrEmpty(receivedMessage.Trim()))
                {
                    object[] serializedMessage = JsonConvert.DeserializeObject<object[]>(receivedMessage);
                    JContainer dictionary = serializedMessage[0] as JContainer;
                    var status = dictionary["status"].ToString();
                    if (status == "200")
                    {
                        receivedAuditMessage = true;
                    }
                }
            }
            catch { }
            finally
            {
                auditManualEvent.Set();
            }
        }

        private void DummyErrorCallback(string result)
        {

        }
    }
}
