using NDesk.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SaveConverter
{
    class Program
    {
        static void Main(string[] args)
        {

            var settings = new ConvertSettings()
            {
                omitNull = true,
                omitFalse = false,
                omitZero = false,
                minify = false,
                singleFile = false
            };
            string outputPath = null;

            var p = new OptionSet() {
                { "file=",      v => outputPath = v },
                { "singleFile",  v => settings.singleFile = true },
                { "minify",  v => settings.minify = true },
                { "omitDefault",  v => { settings.omitFalse = true; settings.omitNull = true; settings.omitZero = true; } },
                { "omitNull=",  v => settings.omitNull = v == "1" },
                { "omitZero=",  v => settings.omitZero = v == "1" },
                { "omitFalse=",  v => settings.omitFalse = v == "1" },
            };
            List<string> extras = p.Parse(args);
            if (extras.Count < 1)
            {
                Console.WriteLine("File to convert missing");
                Environment.Exit(-1);
            }

            string file = extras[0];

            bool compress = false;
            if (file.EndsWith(".json"))
                compress = true;
            if (Directory.Exists(file))
            {
                compress = true;
                settings.singleFile = false;
            }

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                FloatFormatHandling = FloatFormatHandling.Symbol
            };

            if (compress)
            {
                if (outputPath == null)
                {
                    if (file.EndsWith("_json"))
                        outputPath = file.Substring(0, file.Length - 5);
                    else if (file.EndsWith(".json"))
                        outputPath = file.Substring(0, file.Length - 5);
                    else
                        throw new Exception("Can't guess output path");
                }
                SaveCompressor.CompressSave(file, outputPath);
            }
            else
            {
                SaveExpander.ExpandSave(file, outputPath, settings);
            }


        }

    }
}
