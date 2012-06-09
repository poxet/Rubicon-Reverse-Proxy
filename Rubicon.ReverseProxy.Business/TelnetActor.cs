using Rubicon.ReverseProxy.Repository.Interface;
using System.Threading.Tasks;
using System.Text;
using System;
using Rubicon.ReverseProxy.Business.Entities;
using System.Linq;
using System.Globalization;

namespace Rubicon.ReverseProxy.Business
{
    class TelnetActor
    {
        private string _currentContext = string.Empty;
        private readonly ITcpClientActor _tcpClientActor;
        private readonly TelnetPage _rootPage;

        private TelnetPage CreatePages(ITcpClientActor tcpClientActor)
        {
            //Add telnet pages here
            var page = new TelnetMainMenuPage();

            page.Add(new TelnetRouteListPage('1', tcpClientActor));
            page.Add(new TelnetMonitorPage('2', tcpClientActor));

            return page;
        }

        public TelnetActor(ITcpClientActor tcpClientActor)
        {
            _tcpClientActor = tcpClientActor;
            _rootPage = CreatePages(tcpClientActor);
        }

        public void HandleTelnetClientCommAsync()
        {
            SendMessage(_currentContext);

            var reader = new Task(Engine);
            reader.Start();
        }

        private void SendMessage(string context, bool showCommandsAndPages = true, string additionalMessage = null)
        {
            var page = this.FindPage(_rootPage, _currentContext);
            var pageResponse = page.GetPageContent();

            //If there is no response from the page, automatically step up one level (Just do this once)
            if (pageResponse == null)
            {
                _currentContext = _currentContext.Substring(0, _currentContext.Length - 1); //Step up one level
                page = this.FindPage(_rootPage, _currentContext);
                pageResponse = page.GetPageContent();
            }

            
            var sb = new StringBuilder();

            _tcpClientActor.Action(TelnetAction.ClearScreen);

            sb.AppendLine(string.Format(page.Name));
            sb.AppendLine(pageResponse);

            if (!string.IsNullOrEmpty(additionalMessage))
            {
                sb.AppendLine(additionalMessage);
                sb.AppendLine("");
            }

            if (showCommandsAndPages)
            {
                foreach (var command in page.Commands) sb.AppendLine(string.Format("{0}. {1}", command.Key, command.Name));

                if (page.Commands.Count > 0) sb.AppendLine(); //If there are commands, apped an empty line.

                foreach (var subPage in page.Pages) sb.AppendLine(string.Format("{0}. {1}", subPage.Key, subPage.Name));

                sb.AppendLine(string.IsNullOrEmpty(context) ? "0. Quit" : "0. Exit");
                sb.AppendLine("");
            }

            _tcpClientActor.Write(sb.ToString());
        }

        private void Engine()
        {
            try
            {
                PerformaceCounters.Instance.TelnetClientCounterIncrement();

                while (true)
                {
                    var b = (char)_tcpClientActor.ReadByte();

                    string cmdMessage;
                    if (!InvokeCommand(_currentContext, b, out cmdMessage))
                    {
                        if (HasPage(_currentContext, b))
                        {
                            _currentContext += b; //Enter subpage
                        }
                        else if (b == '0')
                        {
                            if (string.IsNullOrEmpty(_currentContext)) return; //Exit
                            _currentContext = _currentContext.Substring(0, _currentContext.Length - 1); //Step up one level
                        }
                    }

                    SendMessage(_currentContext, true, cmdMessage);
                }
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);

                try
                {
                    if (_tcpClientActor != null) 
                        _tcpClientActor.WriteLine(string.Format("Server Exception: {0}", exception.Message));
                }
                catch (Exception subException)
                {
                    LogHelper.LogException(subException, false, LogHelper.ExceptionSeverity.ErrorException);
                }
            }
            finally
            {
                PerformaceCounters.Instance.TelnetClientCounterDecrement();

                if (_tcpClientActor != null) 
                    _tcpClientActor.Stop();
            }
        }

        private bool HasPage(string context, char page)
        {
            return FindPage(_rootPage, context).Pages.Any(x => string.Compare(x.Key.ToString(CultureInfo.InvariantCulture), page.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        private bool InvokeCommand(string context, char command, out string result)
        {
            result = string.Empty;

            //Try to find the command
            var cmd = FindPage(_rootPage, context).Commands.SingleOrDefault(x => string.Compare(x.Key.ToString(CultureInfo.InvariantCulture), command.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase) == 0);
            if (cmd == null)
                return false;

            try
            {
                SendMessage(_currentContext, false); //Show the page, but without the command and page navigation options.
                result = cmd.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, true, LogHelper.ExceptionSeverity.ErrorException);
                result = exception.Message;
                return true; //Yes, the command was executed (but there was an error)
            }
        }
        
        private TelnetPage FindPage(TelnetPage page, string context)
        {
            if (string.IsNullOrEmpty(context)) return page;

            var subPage = page.Pages.SingleOrDefault(x => x.Key == context[0]);
            if (subPage == null) throw new InvalidOperationException(string.Format("No page {0} under page {1}.", context[0], page.Name));

            return FindPage(subPage, context.Substring(1));
        }
    }
}