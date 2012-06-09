using System.Collections.Generic;
using System.Linq;

namespace Rubicon.ReverseProxy.Business.Entities
{
    abstract class TelnetPage
    {
        private readonly char _key;
        private readonly string _name;
        private readonly Dictionary<char, TelnetPage> _pages = new Dictionary<char, TelnetPage>();
        private readonly List<TelnetCommand> _telnetCommands = new List<TelnetCommand>();

        public List<TelnetPage> Pages { get { return this._pages.Select(x => x.Value).OrderBy(x => x._key).ToList(); } }

        public char Key { get { return this._key; } }
        public string Name { get { return this._name; } }

        public TelnetPage(char key, string name)
        {
            this._key = key;
            this._name = name;
        }

        public void Add(TelnetPage page)
        {
            this._pages.Add(page._key, page);
        }

        public abstract string GetPageContent();

        public virtual List<TelnetCommand> Commands { get { return _telnetCommands; } }
    }
}