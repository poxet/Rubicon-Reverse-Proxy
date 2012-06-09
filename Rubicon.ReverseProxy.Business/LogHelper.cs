using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Configuration;
using Tharga.Support.Client.Base;

namespace Rubicon.ReverseProxy.Business
{
    using System.Collections;
    using System.Text;

    class LogHelper
    {
        #region Event


        public class MessageEventArgs : EventArgs
        {
            public string Message { get; private set; }
            public ConsoleColor? ConsoleColor { get; private set; }

            public MessageEventArgs(string message, ConsoleColor? consoleColor)
            {
                Message = message;
                ConsoleColor = consoleColor;
            }
        }

        public static event EventHandler<MessageEventArgs> MessageEvent;

        public static void OnMessageEvent(MessageEventArgs e)
        {
            var handler = MessageEvent;
            if (handler != null)
                handler(null, e);
        }


        #endregion

        public enum ExceptionSeverity { ErrorException, WarningException, InformationException, FatalException }

        private static readonly object _syncRoot = new object();

        #region Tharga Support Communication


        private static void RegisterExceptionAsync(Exception exception, bool visibleToUser, ExceptionSeverity exceptionSeverity)
        {
            var registerExceptionTask = new Task(() => RegisterExceptionEx(exception, visibleToUser, exceptionSeverity));
            registerExceptionTask.Start();
        }

        private static void RegisterMessageAsync(string message, Issue.IssueLevel issueLevel)
        {
            var registerMessageTask = new Task(() => RegisterMessageEx(message, issueLevel));
            registerMessageTask.Start();            
        }

        private static void RegisterExceptionEx(Exception exception, bool visibleToUser, ExceptionSeverity exceptionSeverity)
        {
            try
            {
                EnsureSessionEx();
                Debug.WriteLine("Provide {0} to tharga.support service when possible.", exceptionSeverity);
                Issue.Register(exception, visibleToUser);
            }
            catch (Exception exp)
            {
                Trace.TraceError(string.Format("Cannot register exception using tharga.support. {0}", exp.Message));
            }
        }

        private static void RegisterMessageEx(string message, Issue.IssueLevel issueLevel)
        {
            try
            {
                EnsureSessionEx();
                Issue.Register(message, issueLevel);
            }
            catch (Exception exp)
            {
                Trace.TraceError(string.Format("Cannot register message using tharga.support. {0}", exp.Message));
            }            
        }

        public static void StartSessionAsync()
        {
            var startSessionTask = new Task(EnsureSessionEx);
            startSessionTask.Start();
        }

        public static void EndSession()
        {
            if (Session.IsStarted)
                Session.End();
        }

        private static void EnsureSessionEx()
        {
            try
            {
                if (!Session.IsStarted)
                {
                    lock (_syncRoot) //Check-lock-check pattern
                    {
                        if (!Session.IsStarted)
                        {
                            Tharga.Support.Client.Base.Configuration.ClientToken = "WRCMCK9N5ALKTZCOTSMSXAJU0GXOLG";
                            Session.Start();

                            if (!Session.IsStarted)
                                throw new InvalidOperationException("Session has not been started after that the start request was sent.");
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Trace.TraceError(string.Format("Cannot start session using tharga.support. {0}", exp.Message));
            }
        }


        #endregion

        public static void LogMessage(string message, Issue.IssueLevel issueLevel)
        {
            try
            {
                ShowMessage(string.Format("{0}: {1}", issueLevel, message), ConsoleColor.Blue);

                switch(issueLevel)
                {
                    case Issue.IssueLevel.Error:
                        ShowMessage(message, ConsoleColor.Red);
                        Trace.TraceError(message);
                        RegisterMessageAsync(message, issueLevel);
                        break;
                    case Issue.IssueLevel.Warning:
                        ShowMessage(message, ConsoleColor.Yellow);
                        Trace.TraceWarning(message);
                        RegisterMessageAsync(message, issueLevel);
                        break;
                    case Issue.IssueLevel.Information:
                        ShowMessage(message, ConsoleColor.Green);
                        Trace.TraceInformation(message);
                        //RegisterMessageAsync(message, issueLevel); //This will be too much and of no value to the support service.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Unknown issuelevel {0}.", issueLevel));
                }                
            }
            catch (Exception exp)
            {
                ShowMessage(exp.Message, ConsoleColor.DarkMagenta);
            }
        }

        public static void LogException(Exception exception, bool visibleToUser, ExceptionSeverity exceptionSeverity)
        {
            try
            {
                switch(exceptionSeverity)
                {
                    case ExceptionSeverity.FatalException:
                    case ExceptionSeverity.ErrorException:
                        ShowMessage(string.Format("{0}: {1}", exceptionSeverity, exception.Message), ConsoleColor.Red);
                        Trace.TraceError(string.Format("{0} [{1}]", exception.Message, CreateString(exception.Data)));
                        RegisterExceptionAsync(exception, visibleToUser, exceptionSeverity);
                        break;
                    case ExceptionSeverity.WarningException:
                        ShowMessage(string.Format("{0}: {1}", exceptionSeverity, exception.Message), ConsoleColor.Yellow);
                        Trace.TraceError(string.Format("{0} [{1}]", exception.Message, CreateString(exception.Data)));
                        RegisterExceptionAsync(exception, visibleToUser, exceptionSeverity);
                        break;
                    case ExceptionSeverity.InformationException:
                        if (IsInDebugMode())
                        {
                            ShowMessage(string.Format("{0}: {1}", exceptionSeverity, exception.Message), ConsoleColor.Cyan);
                            Trace.TraceInformation(exception.Message);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Unknown exception severity {0}.", exceptionSeverity));
                }
            }
            catch (Exception exp)
            {
                ShowMessage(exp.Message, ConsoleColor.DarkMagenta);
            }
        }

        private static string CreateString(IDictionary data)
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry item in data)
                sb.AppendFormat("{0}={1}, ", item.Key, item.Value);    

            return sb.ToString();
        }

        private static bool IsInDebugMode()
        {
            bool debug;

            var value = ConfigurationManager.AppSettings["Debug"];
            if (string.IsNullOrEmpty(value)) return false; //No config
            if (!bool.TryParse(value, out debug)) return false; //Invalid config
            return debug;
        }

        //private static void OutputMessage(string message, ConsoleColor? consoleColor = null)
        //{
        //    if (consoleColor == null)
        //        Console.WriteLine(message);
        //    else
        //    {
        //        lock (_syncRoot)
        //        {
        //            var currentColor = Console.ForegroundColor;
        //            Console.ForegroundColor = consoleColor.Value;
        //            Console.WriteLine(message);
        //            Console.ForegroundColor = currentColor;
        //        }
        //    }
        //}

        //Will be displayed but not stored anywhere
        public static void ShowMessage(string message, ConsoleColor? consoleColor = null)
        {
            OnMessageEvent(new MessageEventArgs(message, consoleColor));
            //OutputMessage(message, consoleColor);
        }
    }
}