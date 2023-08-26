using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogManager : MonoBehaviour
{
    public static string myLog = "";
    private string output;
    private string stack;
    private static LogManager logManagerInstance;

    void Awake()
    {
        if (AppGlobal.trackLogs)
        {            
            if (logManagerInstance == null)
            {
                logManagerInstance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }
        else
        {
            Destroy(this);
        }        
    }

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
        myLog = output + "\n" + myLog;
        if (AppGlobal.logsIncludeStackTrace)
        {
            myLog = stack + "\n" + myLog;
        }
        if (myLog.Length > 5000)
        {
            myLog = myLog.Substring(0, 4000);
        }
    }
}
