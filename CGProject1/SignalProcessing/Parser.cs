using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public static class Parser {

        public static Dictionary<int, List<ModelPreset>> ParseModels(string path) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!File.Exists(path)) {
                return null;
            }

            var presets = new Dictionary<int, List<ModelPreset>>();

            using (var file = new StreamReader(path)) {
                while (!file.EndOfStream) {
                    var modelId = int.Parse(file.ReadLine());
                    var argsCnt = int.Parse(file.ReadLine());

                    var args = new double[argsCnt];

                    for (int i = 0; i < argsCnt; i++) {
                        args[i] = double.Parse(file.ReadLine(), CultureInfo.InvariantCulture);
                    }

                    var varargsCnt = int.Parse(file.ReadLine());
                    var varargs = new double[varargsCnt][];

                    for (int i = 0; i < varargsCnt; i++) {
                        string[] curVararg = file.ReadLine().Split(',');
                        varargs[i] = new double[curVararg.Length];
                        for (int j = 0; j < curVararg.Length; j++) {
                            varargs[i][j] = double.Parse(curVararg[j], CultureInfo.InvariantCulture);
                        }
                    }

                    var model = new ModelPreset(modelId, args, varargs);
                    if (!presets.ContainsKey(modelId)) {
                        presets.Add(modelId, new List<ModelPreset>());
                    }

                    presets[modelId].Add(model);

                    file.ReadLine();
                }
            }

            return presets;
        }
    }
}
