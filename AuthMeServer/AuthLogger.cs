using System;
using System.IO;
using UnityEngine;

namespace AuthMeServer
{
    public class AuthLogger
    {
        struct Writer
        {
            public StreamWriter LogWriter;
            public string DateTime;
        }
        
        private static Writer LogWriter;
        
        internal static void LogWriterInit()
        {
            try
            {
                if (LogWriter.LogWriter != null)
                    LogWriter.LogWriter.Close();

                LogWriter.DateTime = DateTime.Now.ToString("yyyy_MM_dd");
                LogWriter.LogWriter = new StreamWriter(Path.Combine(AuthMeServer.AuthLogPath, string.Format("Log_{0}.log", LogWriter.DateTime)), true);
                LogWriter.LogWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        private static string LogFormat(string Text)
        {
            Text = "[" + DateTime.Now + "] " + Text;
            return Text;
        }
        
        private static void WriteLog(string Message)
        {
            try
            {
                if (LogWriter.DateTime != DateTime.Now.ToString("yyyy_MM_dd"))
                    LogWriterInit();
                LogWriter.LogWriter.WriteLine(LogFormat(Message));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        public static void Log(string Message)
        {
            Message = "[Log] " + Message;
            WriteLog(Message);
        }
    }
}