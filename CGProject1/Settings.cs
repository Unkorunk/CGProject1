using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace CGProject1
{
    public class Settings
    {
        private static readonly Configuration Configuration =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private readonly Dictionary<string, string> values = new();

        private static readonly Dictionary<string, Settings> Instances = new();

        public string Section { get; }

        public static Settings GetInstance(string section)
        {
            if (!ValidateKey(section)) return null;
            if (!Instances.ContainsKey(section)) Instances[section] = new Settings(section);
            return Instances[section];
        }

        private Settings(string section)
        {
            Section = section;

            if (!Configuration.AppSettings.Settings.AllKeys.Contains(section)) return;
            if (Configuration.AppSettings.Settings[section].Value == string.Empty) return;

            var items = Configuration.AppSettings.Settings[section].Value
                .Split(";")
                .Select(item => item.Split("="));

            foreach (var item in items)
            {
                if (item.Length != 2) throw new Exception();
                values.Add(item[0], item[1]);
            }
        }

        public void Set(string key, float value) => Set(key, value.ToString(CultureInfo.InvariantCulture));
        public void Set(string key, double value) => Set(key, value.ToString(CultureInfo.InvariantCulture));

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

        public float GetOrDefault(string key, float defaultValue)
        {
            if (!ValidateKey(key)) return defaultValue;
            var item = values.TryGetValue(key, out var str) ? str : null;
            if (item == null) return defaultValue;

            return float.TryParse(item, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture,
                out var value)
                ? value
                : defaultValue;
        }

        public double GetOrDefault(string key, double defaultValue)
        {
            if (!ValidateKey(key)) return defaultValue;
            var item = values.TryGetValue(key, out var str) ? str : null;
            if (item == null) return defaultValue;

            return double.TryParse(item, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture,
                out var value)
                ? value
                : defaultValue;
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            if (!ValidateKey(key)) return defaultValue;
            var item = values.TryGetValue(key, out var value) ? value : null;
            if (item == null) return defaultValue;

            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter == null) return defaultValue;

            return (T) converter.ConvertFromString(item);
        }

        public static void Save()
        {
            foreach (var (section, instance) in Instances)
            {
                var value = string.Join(";", instance.values.Select(item => $"{item.Key}={item.Value}"));

                var item = Configuration.AppSettings.Settings[section];

                if (item != null)
                {
                    item.Value = value;
                }
                else
                {
                    Configuration.AppSettings.Settings.Add(section, value);
                }
            }

            Configuration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(Configuration.AppSettings.SectionInformation.Name);
        }
    }
}
