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
        string currentUnitTestCase = "";

        [Test]
        public void ThenSubKeyLevelShouldReturnSuccess()
        {
            currentUnitTestCase = "ThenSubKeyLevelShouldReturnSuccess";

            receivedAuditMessage = false;

            Pubnub pubnub = new Pubnub(PubnubKey.PublishKey, PubnubKey.SubscribeKey, PubnubKey.SecretKey, "", false);

            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "WhenAuditIsRequested";
            unitTest.TestCaseName = "ThenSubKeyLevelShouldReturnSuccess";
            pubnub.PubnubUnitTest = unitTest;

            string channel = "hello_my_channel";

            pubnub.GrantAccess<string>(channel, true, true, 5, AccessToSubKeyLevelCallback, DummyErrorCallback);
            Thread.Sleep(1000);

            auditManualEvent.WaitOne();

            Assert.IsTrue(receivedAuditMessage, "WhenAuditIsRequested -> ThenSubKeyLevelShouldReturnSuccess failed.");

        }

        [Test]
        public void ThenChannelLevelShouldReturnSuccess()
        {
            currentUnitTestCase = "ThenChannelLevelShouldReturnSuccess";

            receivedAuditMessage = false;

            Pubnub pubnub = new Pubnub(PubnubKey.PublishKey, PubnubKey.SubscribeKey, PubnubKey.SecretKey, "", false);

            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "WhenAuditIsRequested";
            unitTest.TestCaseName = "ThenChannelLevelShouldReturnSuccess";
            pubnub.PubnubUnitTest = unitTest;

            string channel = "hello_my_channel";

            pubnub.GrantAccess<string>(channel, true, true, 5, AccessToSubKeyLevelCallback, DummyErrorCallback);
            Thread.Sleep(1000);

            auditManualEvent.WaitOne();

            Assert.IsTrue(receivedAuditMessage, "WhenAuditIsRequested -> ThenChannelLevelShouldReturnSuccess failed.");

        }

        void AccessToSubKeyLevelCallback(string receivedMessage)
        {
            try
            {
                if (!string.IsNullOrEmpty(receivedMessage) && !string.IsNullOrEmpty(receivedMessage.Trim()))
                {
                    object[] serializedMessage = JsonConvert.DeserializeObject<object[]>(receivedMessage);
                    JContainer dictionary = serializedMessage[0] as JContainer;
                    if (dictionary != null)
                    {
                        int statusCode = dictionary.Value<int>("status");
                        string statusMessage = dictionary.Value<string>("message");
                        if (statusCode == 200 && statusMessage.ToLower() == "success")
                        {
                            var payload = dictionary.Value<JContainer>("payload");
                            if (payload != null)
                            {
                                bool read = payload.Value<bool>("r");
                                bool write = payload.Value<bool>("w");
                                string level = payload.Value<string>("level");
                                if (level == "subkey")
                                {
                                    switch (currentUnitTestCase)
                                    {
                                        case "ThenSubKeyLevelWithReadWriteShouldReturnSuccess":
                                            if (read && write) receivedAuditMessage = true;
                                            break;
                                        case "ThenSubKeyLevelWithReadShouldReturnSuccess":
                                            if (read && !write) receivedAuditMessage = true;
                                            break;
                                        case "ThenSubKeyLevelWithWriteShouldReturnSuccess":
                                            if (!read && write) receivedAuditMessage = true;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }

                        //if (dictionary.
                        //if (status == "200")
                        //{
                        //    receivedGrantMessage = true;
                        //}
                    }
                    //var level = dictionary["level"].ToString();
                }
            }
            catch { }
            finally
            {
                auditManualEvent.Set();
            }
        }

        void AccessToChannelLevelCallback(string receivedMessage)
        {
            try
            {
                if (!string.IsNullOrEmpty(receivedMessage) && !string.IsNullOrEmpty(receivedMessage.Trim()))
                {
                    object[] serializedMessage = JsonConvert.DeserializeObject<object[]>(receivedMessage);
                    JContainer dictionary = serializedMessage[0] as JContainer;
                    string currentChannel = serializedMessage[1].ToString();
                    if (dictionary != null)
                    {
                        int statusCode = dictionary.Value<int>("status");
                        string statusMessage = dictionary.Value<string>("message");
                        if (statusCode == 200 && statusMessage.ToLower() == "success")
                        {
                            var payload = dictionary.Value<JContainer>("payload");
                            if (payload != null)
                            {
                                string level = payload.Value<string>("level");
                                var channels = payload.Value<JContainer>("channels");
                                if (channels != null)
                                {
                                    var channelContainer = channels.Value<JContainer>(currentChannel);
                                    if (channelContainer != null)
                                    {
                                        bool read = channelContainer.Value<bool>("r");
                                        bool write = channelContainer.Value<bool>("w");
                                        if (level == "channel")
                                        {
                                            switch (currentUnitTestCase)
                                            {
                                                case "ThenChannelLevelWithReadWriteShouldReturnSuccess":
                                                    if (read && write) receivedAuditMessage = true;
                                                    break;
                                                case "ThenChannelLevelWithReadShouldReturnSuccess":
                                                    if (read && !write) receivedAuditMessage = true;
                                                    break;
                                                case "ThenChannelLevelWithWriteShouldReturnSuccess":
                                                    if (!read && write) receivedAuditMessage = true;
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
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
