namespace Rubicon.ReverseProxy.Business.Entities
{
    class TelnetMainMenuPage : TelnetPage
    {
        public TelnetMainMenuPage()
            : base('x', "Welcome to Rubicon Reverse Proxy.")
        {
        }

        public override string GetPageContent()
        {
            return string.Empty;
        }
    }
}