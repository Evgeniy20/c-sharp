using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using PubNubMessaging.Core;
using System.Collections.Concurrent;
using System;
using System.Reflection;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Text;

public class PubnubExample : MonoBehaviour {

    enum PubnubState
    {
        Presence,
        Subscribe,
        Publish,
        DetailedHistory,
        HereNow,
        Time,
        Unsubscribe,
        PresenceUnsubscribe,
        DisconnectRetry,
        EnableNetwork,
        DisableNetwork
    }

    bool ssl = false;
    bool resumeOnReconnect = true;

    string cipherKey = "";
    string secretKey = "";
    string uuid = Guid.NewGuid().ToString();

    string subscribeTimeoutInSeconds = "310";
    string operationTimeoutInSeconds = "15";
    string networkMaxRetries = "50";
    string networkRetryIntervalInSeconds = "10";
    string heartbeatIntervalInSeconds = "10";

    string channel = "hello_world";
    string publishedMessage = "";

    static Pubnub pubnub;

    private static ConcurrentQueue<string> recordQueue = new ConcurrentQueue<string>();

    //GameObject PubnubApiResult;
    Vector2 scrollPosition = Vector2.zero;
    string pubnubApiResult = "";

    bool requestInProcess = false;

    bool showPublishPopupWindow = false;

    Rect publishWindowRect = new Rect(60, 365, 300, 150);

    bool allowUserSettingsChange = true;
    
    public void OnGUI()
    {
        GUI.enabled = !allowUserSettingsChange;
        GUIStyle customStyle = new GUIStyle(GUI.skin.button);
        customStyle.fontSize = 10;
        customStyle.hover.textColor = Color.yellow;
        customStyle.fontStyle = FontStyle.Italic;

        float fLeft = 20;
        float fLeftInit = 20;
        float fTop = 10;
        float fTopInit = 10;
        float fRowHeight = 35;
        float fHeight = 25;
        float fButtonHeight = 35;

        fLeft = fLeftInit;
        fTop = fTopInit;
        if (GUI.Button(new Rect(fLeft, fTop, 120, 30), "Reset Settings",customStyle))
        {
            allowUserSettingsChange = true;
            ResetPubnubInstance();
            pubnubApiResult = "";
        }

        GUI.enabled = allowUserSettingsChange;

        fLeft = fLeftInit + 150;
        ssl = GUI.Toggle(new Rect(fLeft, fTop, 100, fButtonHeight), ssl," Enable SSL ");

        fLeft = fLeft + 100;
        resumeOnReconnect = GUI.Toggle(new Rect(fLeft, fTop, 200, fButtonHeight), resumeOnReconnect," Resume On Reconnect ");

        fTop = fTopInit + fRowHeight;
        fLeft = fLeftInit;
        GUI.Label(new Rect(fLeft, fTop, 70, fHeight), "Cipher Key");

        fLeft = fLeft + 75;
        cipherKey = GUI.TextField(new Rect(fLeft, fTop, 130, fHeight),cipherKey);

        fLeft = fLeft + 145;
        GUI.Label(new Rect(fLeft, fTop, 70, fHeight), "UUID");

        fLeft = fLeft + 45;
        uuid = GUI.TextField(new Rect(fLeft, fTop, 170, fHeight),uuid);

        fTop = fTopInit + 2 * fRowHeight;
        fLeft = fLeftInit;
        GUI.Label(new Rect(fLeft, fTop, 70, fHeight), "Secret Key");
        fLeft = fLeft + 75;
        secretKey = GUI.TextField(new Rect(fLeft, fTop, 130, fHeight),secretKey);

        fLeft = fLeft + 145;
        GUI.Label(new Rect(fLeft, fTop, 160, fHeight), "Subscribe Timeout (secs)");

        fLeft = fLeft + 185;
        subscribeTimeoutInSeconds = GUI.TextField(new Rect(fLeft, fTop, 30, 25),subscribeTimeoutInSeconds,6);
        subscribeTimeoutInSeconds = Regex.Replace(subscribeTimeoutInSeconds, "[^0-9]", "");

        fTop = fTopInit + 3 * fRowHeight;
        fLeft = fLeftInit;
        GUI.Label(new Rect(fLeft, fTop, 160, fHeight), "MAX retries");

        fLeft = fLeft + 175;
        networkMaxRetries = GUI.TextField(new Rect(fLeft, fTop, 30, fHeight),networkMaxRetries,6);
        networkMaxRetries = Regex.Replace(networkMaxRetries, "[^0-9]", "");

        fLeft = fLeft + 45;
        GUI.Label(new Rect(fLeft, fTop, 180, fHeight), "Non Subscribe Timeout (secs)");

        fLeft = fLeft + 185;
        operationTimeoutInSeconds = GUI.TextField(new Rect(fLeft, fTop, 30, fHeight),operationTimeoutInSeconds,6);
        operationTimeoutInSeconds = Regex.Replace(operationTimeoutInSeconds, "[^0-9]", "");

        fTop = fTopInit + 4 * fRowHeight;
        fLeft = fLeftInit;
        GUI.Label(new Rect(fLeft, fTop, 160, fHeight), "Retry Interval (secs)");

        fLeft = fLeft + 175;
        networkRetryIntervalInSeconds = GUI.TextField(new Rect(fLeft, fTop, 30, fHeight),networkRetryIntervalInSeconds,6);
        networkRetryIntervalInSeconds = Regex.Replace(networkRetryIntervalInSeconds, "[^0-9]", "");

        fLeft = fLeft + 45;
        GUI.Label(new Rect(fLeft, fTop, 180, fHeight), "Heartbeat Interval (secs)");
        fLeft = fLeft + 185;
        heartbeatIntervalInSeconds = GUI.TextField(new Rect(fLeft, fTop, 30, fHeight),heartbeatIntervalInSeconds,6);
        heartbeatIntervalInSeconds = Regex.Replace(heartbeatIntervalInSeconds, "[^0-9]", "");

        GUI.enabled = true;

        fTop = fTopInit + 5 * fRowHeight;
        fLeft = fLeftInit;
        GUI.Label(new Rect(fLeft, fTop, 90, fHeight), "Channel Name");
        fLeft = fLeft + 95;
        channel = GUI.TextField(new Rect(fLeft, fTop, 200, fHeight),channel,100);

        fLeft = fLeft + 220;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Disconnect/Retry"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.DisconnectRetry);
        }

        fTop = fTopInit + 6 * fRowHeight + 10;
        fLeft = fLeftInit;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Presence"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.Presence);
        }

        fLeft = fLeft + 155;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Subscribe"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.Subscribe);
        }

        fLeft = fLeft + 160;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Detailed History"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.DetailedHistory);
        }

        fTop = fTopInit + 7 * fRowHeight + 10 * 2;
        fLeft = fLeftInit;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Publish"))
        {
            InstantiatePubnub();
            allowUserSettingsChange = false;
            showPublishPopupWindow = true;
        }
        if (showPublishPopupWindow)
        {
            GUI.backgroundColor = Color.black;
            publishWindowRect = GUI.ModalWindow(0, publishWindowRect, DoPublishWindow, "Message Publish");
            GUI.backgroundColor = new Color(1,1,1,1);
        }

        fLeft = fLeft + 155;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Unsubscribe"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.Unsubscribe);
        }

        fLeft = fLeft + 160;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Presence-Unsub"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.PresenceUnsubscribe);
        }

        fTop = fTopInit + 8 * fRowHeight + 10 * 3;
        fLeft = fLeftInit;
        if (GUI.Button(new Rect(fLeft, fTop, 75, fButtonHeight), "Here Now"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.HereNow);
        }

        fLeft = fLeft + 95;
        if (GUI.Button(new Rect(fLeft, fTop, 60, fButtonHeight), "Time"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.Time);
        }
    
        #if(UNITY_IOS || UNITY_ANDROID)
        GUI.enabled = false;
        #endif
        fLeft = fLeft + 80;
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Disable Network"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.DisableNetwork);
        }
        fLeft = fLeft + 140;        
        if (GUI.Button(new Rect(fLeft, fTop, 120, fButtonHeight), "Enable Network"))
        {
            InstantiatePubnub();
            AsyncOrNonAsyncCall (PubnubState.EnableNetwork);
        }
        #if(UNITY_IOS || UNITY_ANDROID)
        GUI.enabled = true;
        #endif

        fTop = fTopInit + 9 * fRowHeight + 10 * 4;
        fLeft = fLeftInit;
        scrollPosition = GUI.BeginScrollView(new Rect(fLeft, fTop, 430, 320), scrollPosition, new Rect(fLeft, fTop, 430, 320),false, true);
        GUI.enabled = false;
        pubnubApiResult = GUI.TextArea(new Rect(fLeft, fTop, 430, 320), pubnubApiResult);            
        GUI.enabled = true;
        GUI.EndScrollView();

    }

    /// <summary>
    /// Determines whether to send an asynchronous or synchronous call on the button click
    /// Async calls on button click when used in iOS results in random crashes thus sync calls 
    /// are preferred in case iOS
    /// </summary>
    /// <param name="pubnubState">Pubnub state.</param>
    void AsyncOrNonAsyncCall (PubnubState pubnubState)
    {
#if(UNITY_IOS)
        if(pubnubState == PubnubState.DisconnectRetry)
        {
            if(!requestInProcess)
            {
                requestInProcess = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(DoAction), pubnubState);
            }
        }
        else
        {
            DoAction(pubnubState);
        }
#else
        ThreadPool.QueueUserWorkItem(new WaitCallback(DoAction), pubnubState);
#endif
    }
    
    void Awake(){
        Application.RegisterLogCallback(new Application.LogCallback(CaptureLogs));
    }
    
    void CaptureLogs(string condition, string stacktrace, LogType logType)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Type");
        sb.AppendLine(logType.ToString());
        sb.AppendLine("Condition");
        sb.AppendLine(condition);
        sb.AppendLine("stacktrace");
        sb.AppendLine(stacktrace);
        //UnityEngine.Debug.Log("Type: ", );
    }
    
    private void DoAction (object pubnubState)
    {
        try
        {
            if ((PubnubState)pubnubState == PubnubState.Presence) {
                AddToPubnubResultContainer ("Running Presence");
                allowUserSettingsChange = false;
                pubnub.Presence<string> (channel, DisplayReturnMessage, DisplayConnectStatusMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.Subscribe) {
                AddToPubnubResultContainer ("Running Subscribe");
                allowUserSettingsChange = false;
                pubnub.Subscribe<string> (channel, DisplayReturnMessage, DisplayConnectStatusMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.DetailedHistory) {
                AddToPubnubResultContainer ("Running Detailed History");
                allowUserSettingsChange = false;
                pubnub.DetailedHistory<string>(channel, 100, DisplayReturnMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.HereNow) {
                AddToPubnubResultContainer ("Running Here Now");
                allowUserSettingsChange = false;
                pubnub.HereNow<string>(channel, DisplayReturnMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.Time) {
                AddToPubnubResultContainer ("Running Time");
                allowUserSettingsChange = false;
                pubnub.Time<string>(DisplayReturnMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.Unsubscribe) {
                AddToPubnubResultContainer ("Running Unsubscribe");
                allowUserSettingsChange = false;
                pubnub.Unsubscribe<string>(channel, DisplayReturnMessage, DisplayConnectStatusMessage, DisplayDisconnectStatusMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.PresenceUnsubscribe) {
                AddToPubnubResultContainer ("Running Presence Subscribe");
                allowUserSettingsChange = false;
                pubnub.PresenceUnsubscribe<string>(channel, DisplayReturnMessage, DisplayConnectStatusMessage, DisplayDisconnectStatusMessage, DisplayErrorMessage);
            } else if ((PubnubState)pubnubState == PubnubState.EnableNetwork) {
                AddToPubnubResultContainer ("Running Enable Network");
                pubnub.DisableSimulateNetworkFailForTestingOnly();
            } else if ((PubnubState)pubnubState == PubnubState.DisableNetwork)     {
                AddToPubnubResultContainer ("Running Disable Network");
                pubnub.EnableSimulateNetworkFailForTestingOnly();
            } else if ((PubnubState)pubnubState == PubnubState.DisconnectRetry) {
                AddToPubnubResultContainer ("Running Disconnect Retry");
                pubnub.TerminateCurrentSubscriberRequest();
                requestInProcess = false;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log ("DoAction exception:"+ ex.ToString());
        }
    }

    void InstantiatePubnub()
    {
        if (pubnub == null)
        {
            pubnub = new Pubnub("demo","demo",secretKey,cipherKey,ssl);

            pubnub.SessionUUID = uuid;
            pubnub.SubscribeTimeout = int.Parse(subscribeTimeoutInSeconds);
            pubnub.NonSubscribeTimeout = int.Parse(operationTimeoutInSeconds);
            pubnub.NetworkCheckMaxRetries = int.Parse(networkMaxRetries);
            pubnub.NetworkCheckRetryInterval = int.Parse(networkRetryIntervalInSeconds);
            pubnub.HeartbeatInterval = int.Parse(heartbeatIntervalInSeconds);
            pubnub.EnableResumeOnReconnect = resumeOnReconnect;
        }
    }


    void DoPublishWindow(int windowID) {

        GUI.Label(new Rect(10,25,100,25), "Enter Message");
        publishedMessage = GUI.TextArea(new Rect(110,25,150,60),publishedMessage,2000);
		string stringMessage = publishedMessage;
        if (GUI.Button(new Rect(30, 100, 100, 30), "Publish"))
        {
			pubnub.Publish<string>(channel, stringMessage, DisplayReturnMessage, DisplayErrorMessage);
            publishedMessage = "";
            showPublishPopupWindow = false;
        }

        if (GUI.Button(new Rect(150, 100, 100, 30), "Cancel"))
        {
            showPublishPopupWindow = false;
        }
        GUI.DragWindow(new Rect(0,0,800,400));
    }    

    void Start()
    {
        System.Net.ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
        {
            if (chain != null && chain.ChainStatus != null)
            {
                X509Certificate2 cert2 = new X509Certificate2(certificate);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                //chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                //chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(1000);
                //chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                //chain.ChainPolicy.VerificationTime = DateTime.Now;
                chain.Build(cert2);

                foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus)
                {
                    if ((certificate.Subject == certificate.Issuer) &&
                        (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot)) 
                    {
                        // Self-signed certificates with an untrusted root are valid. 
                        continue;
                    }
                    else
                    {
                        if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
                        {
                            // If there are any other errors in the certificate chain, the certificate is invalid,
                            // so the method returns false.
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Do not allow this client to communicate with unauthenticated servers. 
        return false;
    }    


    void Update()
    {
        if (pubnub == null) return;

        try{
            //UnityEngine.Debug.Log(DateTime.Now.ToLongTimeString() + " Update called " + pubnubApiResult.Length.ToString());            
            string recordTest;
            System.Text.StringBuilder sbResult = new System.Text.StringBuilder();

            int existingLen = pubnubApiResult.Length;
            int newRecordLen = 0;
            sbResult.Append(pubnubApiResult);

            if (recordQueue.TryPeek(out recordTest))
            {
                string currentRecord = "";
                while (recordQueue.TryDequeue(out currentRecord))
                {
                    sbResult.AppendLine(currentRecord);
                }

                pubnubApiResult = sbResult.ToString();

                newRecordLen = pubnubApiResult.Length - existingLen;
                int windowLength = 600;

                if (pubnubApiResult.Length > windowLength)
                {
                    bool trimmed = false;
                    if (pubnubApiResult.Length > windowLength){
                        trimmed = true;
                        int lengthToTrim = (((pubnubApiResult.Length - windowLength) < pubnubApiResult.Length -newRecordLen)? pubnubApiResult.Length - windowLength : pubnubApiResult.Length - newRecordLen);
                        pubnubApiResult = pubnubApiResult.Substring(lengthToTrim);
                    }
                    if(trimmed)
                    {
                        string prefix = "Output trimmed...\n";

                        pubnubApiResult = prefix + pubnubApiResult;
                    }
                }
            } 
        }
        catch (Exception ex){
            Debug.Log ("Update exception:" + ex.ToString());
        }
    }

    void OnApplicationQuit()
    {
        ResetPubnubInstance();
    }

    void ResetPubnubInstance()
    {
        if (pubnub != null)
        {
            pubnub.EndPendingRequests();
            System.Threading.Thread.Sleep(1000);
            pubnub = null;
        }
    }

    void DisplayReturnMessage(string result)
    {
        //print(result);
        AddToPubnubResultContainer(string.Format("REGULAR CALLBACK: {0}",result));
    }

    void DisplayConnectStatusMessage(string result)
    {
        //print(result);
        AddToPubnubResultContainer(string.Format("CONNECT CALLBACK: {0}",result));
    }

    void DisplayDisconnectStatusMessage(string result)
    {
        //print(result);
        AddToPubnubResultContainer(string.Format("DISCONNECT CALLBACK: {0}",result));
    }

    void DisplayErrorMessage(string result)
    {
        //print(result);
        UnityEngine.Debug.Log (string.Format("ERROR CALLBACK: {0}",result));
        AddToPubnubResultContainer(string.Format("ERROR CALLBACK: {0}",result));
    }

    void AddToPubnubResultContainer(string result)
    {
        recordQueue.Enqueue(result);
    }

}


