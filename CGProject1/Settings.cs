using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace CGProject1
{
    public class Settings
    {
        private const string Filename = "settings.ini";
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public static Settings Instance { get; }

        private Settings()
        {
            if (!File.Exists(Filename)) return;
            var lines = File.ReadAllLines(Filename);

            foreach (var line in lines)
            {
                var parts = line.Split("=");

                if (parts.Length != 2 || !ValidateKey(parts[0]) || string.IsNullOrEmpty(parts[1]))
                {
                    values.Clear();
                    return;
                }

                values.Add(parts[0], parts[1]);
            }
        }

        static Settings()
        {
            Instance = new Settings();
        }

        public void Set<T>(string key, T value)
        {
            if (!ValidateKey(key)) return;

            var valueStr = value.ToString();
            if (string.IsNullOrEmpty(valueStr)) return;

            if (values.ContainsKey(key))
            {
                values[key] = valueStr;
            }
            else
            {
                values.Add(key, valueStr);
            }
        }

        private static bool ValidateKey(string key)
        {
            return key.Length != 0 && key.All(x => char.IsLower(x) || char.IsUpper(x) || char.IsDigit(x) || x == '_') &&
                   !char.IsDigit(key[0]);
        }

        public string Get(string key)
        {
            if (!ValidateKey(key)) return null;
            return values.TryGetValue(key, out var value) ? value : null;
        }

        public void Save()
        {
            var stringBuilder = new StringBuilder();

            foreach (var (key, value) in values)
            {
                stringBuilder.AppendLine($"{key}={value}");
            }

            File.WriteAllText(Filename, stringBuilder.ToString());
        }
    }
}
