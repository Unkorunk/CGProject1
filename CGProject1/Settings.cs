using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CGProject1
{
    public class Settings
    {
        private const string Filename = "settings.ini";
        private readonly Dictionary<string, string> Values = new Dictionary<string, string>();

        public static Settings Instance { get; }

        private Settings()
        {
            if (!File.Exists(Filename)) return;
            
            var lines = File.ReadAllLines(Filename);
            foreach (var line in lines)
            {
                var parts = line.Split("=");
                if (parts.Length != 2 || !parts[0].All(char.IsLower) ||
                    parts[0].Length == 0 || parts[1].Length == 0)
                {
                    Values.Clear();
                    return;
                }
                Values.Add(parts[0], parts[1]);
            }
        }
        
        static Settings()
        {
            Instance = new Settings();
        }

        public void Set<T>(string key, T value)
        {
            var converted = value.ToString();

            key = key.ToLower();
            if (key.Length == 0 || !key.All(char.IsLower) ||
                converted == null || converted.Length == 0) return;

            if (Values.ContainsKey(key))
            {
                Values[key] = converted;
            }
            else
            {
                Values.Add(key, converted);
            }
        }

        public string Get(string key)
        {
            key = key.ToLower();
            if (key.Length == 0 || !key.All(char.IsLower)) return null;
            
            return Values.TryGetValue(key, out var value) ? value : null;
        }

        public void Save()
        {
            var stringBuilder = new StringBuilder();
            
            foreach (var (key, value) in Values)
            {
                stringBuilder.AppendLine($"{key}={value}");
            }

            File.WriteAllText(Filename, stringBuilder.ToString());
        }
    }
}