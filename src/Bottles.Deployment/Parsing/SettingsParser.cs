using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FubuCore;
using FubuCore.CommandLine;
using FubuCore.Configuration;

namespace Bottles.Deployment.Parsing
{
    public class SettingsParser
    {
        public static readonly string INVALID_SYNTAX = "Configuration line must be in the form of 'Class.Prop=Value' or 'bottle:<bottle name> <relationship>'";
        private readonly SettingsData _settings;
        private readonly IList<BottleReference> _references = new List<BottleReference>();

        public SettingsParser(string description, IDictionary<string, string> substitutions)
        {
            _settings = new SettingsData(SettingCategory.core, substitutions);
            _settings.Description = description;
        }

        public void ParseText(string text)
        {
            text = text.Trim();
            try
            {
                if (text.StartsWith(ProfileFiles.BottlePrefix))
                {
                    parseBottle(text);
                }
                else if (text.Contains("="))
                {
                    parseProperty(text);
                }
                else
                {
                    throw new SettingsParserException(INVALID_SYNTAX);
                }
            }
            catch (SettingsParserException e)
            {
                e.AppendText(text);
                throw;
            }
        }

        private void parseProperty(string text)
        {
            var index = text.IndexOf('=');
            if (index <= 0)
            {
                throw new SettingsParserException("Missing property name");
            }
            
            var propertyName = text.Substring(0, index).Trim();
            var value = text.Substring(index + 1, text.Length - index - 1).Trim();

            if (propertyName.IsEmpty())
            {
                throw new SettingsParserException("Missing property name");
            }

            _settings[propertyName] = value;
        }

        private void parseBottle(string text)
        {
            var reference = BottleReference.ParseFrom(text);
            _references.Add(reference);
        }

        public SettingsData Settings
        {
            get { return _settings; }
        }

        public IEnumerable<BottleReference> References
        {
            get { return _references; }
        }
    }
}