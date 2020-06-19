using System;
using System.Collections.Generic;


namespace CGProject1.SignalProcessing {
    public class Modelling {
        static Modelling() {
            Dictionary<int, List<ModelPreset>> presets = Parser.ParseModels(defaultPath);

            discreteModels = new List<ChannelConstructor>();
            var delayedPulse = new ChannelConstructor("Задержанный импульс", 0,
                    new string[] { "Задержка импульса" }, new string[0], new double[] { double.MinValue }, new double[] { double.MaxValue }, new double[] { 0 }, null,
                    (int n, double deltaTime, double[] args, double[][] varargs, double[] signalVals) => {
                        if (n == (int)args[0]) {
                            return 1;
                        }
                        return 0;
                    });
            discreteModels.Add(delayedPulse);

            var delayedRise = new ChannelConstructor("Задержанный скачок", 1,
                    new string[] { "Задержка скачка" }, new string[0], new double[] { double.MinValue }, new double[] { double.MaxValue }, new double[] { 0 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        if (n < args[0]) {
                            return 0;
                        }
                        return 1;
                    });
            discreteModels.Add(delayedRise);

            var discretExp = new ChannelConstructor("Дискр. уб. экспонента", 2,
                    new string[] { "Основание экспоненты" }, new string[0], new double[] { 0 }, new double[] { 1 }, new double[] { 0.5 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        return Math.Pow(args[0], n);
                    });
            discreteModels.Add(discretExp);

            var discretSin = new ChannelConstructor("Дискр. синусоида", 3,
                    new string[] { "Амплитуда", "Круг. частота", "Нач. фаза" }, new string[0],
                    new double[] { double.MinValue, 0, 0 }, new double[] { double.MaxValue, Math.PI, Math.PI * 2 }, new double[] { 1, Math.PI, 0 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        return args[0] * Math.Sin(n * args[1] + args[2]);
                    });
            discreteModels.Add(discretSin);

            var meandr = new ChannelConstructor("Меандр", 4,
                    new string[] { "Период" }, new string[0], new double[] { 2 }, new double[] { double.MaxValue }, new double[] { 2 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        int l = (int)args[0];
                        if ((n % l) < l / 2.0) {
                            return 1;
                        }
                        return -1;
                    });
            discreteModels.Add(meandr);

            var saw = new ChannelConstructor("Пила", 5,
                    new string[] { "Период" }, new string[0], new double[] { 2 }, new double[] { double.MaxValue }, new double[] { 2 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        int l = (int)args[0];
                        return (n % l) * 1.0 / l;
                    });
            discreteModels.Add(saw);

            continiousModels = new List<ChannelConstructor>();

            var withExpEnvelope = new ChannelConstructor("С эксп. огибающей", 6,
                    new string[] { "Амплитуда", "Ширина огибающей", "Частота несущей", "Нач. фаза несущей" }, new string[0],
                    new double[] { double.MinValue, 1e-6, 0, 0 },
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, Math.PI * 2 },
                     new double[] { 1, 10, 44000, 0 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        double t = n * dt;
                        return args[0] * Math.Exp(-t / args[1]) * Math.Cos(2 * Math.PI * args[2] * t + args[3]);
                    });
            continiousModels.Add(withExpEnvelope);

            var withBalancedEnvelope = new ChannelConstructor("С балансной огибающей", 7,
                    new string[] { "Амплитуда", "Частота огибающей", "Частота несущей", "Нач. фаза несущей" }, new string[0],
                    new double[] { double.MinValue, 0, 0, 0 },
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, Math.PI * 2 },
                    new double[] { 1, 1000, 44000, 0 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        double t = n * dt;
                        return args[0] * Math.Cos(2 * Math.PI * args[1] * t) * Math.Cos(2 * Math.PI * args[2] * t + args[3]);
                    });
            continiousModels.Add(withBalancedEnvelope);

            var withTonicEnvelope = new ChannelConstructor("С тональной огибающей", 8,
                    new string[] { "Амплитуда", "Индекс глубины модуляции", "Частота огибающей", "Частота несущей", "Нач. фаза несущей" }, new string[0],
                    new double[] { double.MinValue, 0, 0, 0, 0 },
                    new double[] { double.MaxValue, 1, double.MaxValue, double.MaxValue, 2 * Math.PI },
                    new double[] { 1, 1, 1000, 44000, 0 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        double t = n * dt;
                        return args[0] * (1 + args[1] * Math.Cos(2 * Math.PI * args[2] * t)) * Math.Cos(2 * Math.PI * args[3] * t + args[4]);
                    });
            continiousModels.Add(withTonicEnvelope);

            var linearChirp = new ChannelConstructor("ЛЧМ - сигнал", 13,
                    new string[] { "Амплитуда", "Нач. частота", "Конечная частота", "Нач. фаза"}, new string[0],
                    new double[] { double.MinValue, 0, 0, 0},
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, 2 * Math.PI },
                    new double[] { 1, 0.1, 1, 0 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        double t = n * dt;
                        double dF = (args[2] - args[1]) / (signalVals.Length * dt);
                        return args[0] * Math.Cos(2 * Math.PI * (args[1] + dF * t) * t + args[3] );
                    });
            continiousModels.Add(linearChirp);

            randomModels = new List<ChannelConstructor>();

            var uniformWhiteNoise = new ChannelConstructor("Белый шум (равномерный)", 9,
                    new string[] { "Нижняя граница интервала", "Верхняя граница интервала" }, new string[0],
                    new double[] { double.MinValue, double.MinValue }, new double[] { double.MaxValue, double.MaxValue },
                     new double[] { -5, 5 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        return Randomizer.UniformRand(args[0], args[1]);
                    });
            randomModels.Add(uniformWhiteNoise);

            var normalWhiteNoise = new ChannelConstructor("Белый шум (нормальный)", 10,
                    new string[] { "Среднее", "Дисперсия" }, new string[0],
                    new double[] { double.MinValue, double.MinValue }, new double[] { double.MaxValue, double.MaxValue },
                     new double[] { 0, 1 }, null,
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        return Randomizer.NormalRand(args[0], args[1]);
                    });
            randomModels.Add(normalWhiteNoise);

            var ARMA = new ChannelConstructor("АРСС", 12,
                    new string[] { "Дисперсия" }, new string[] { "A", "B" },
                    new double[] { double.MinValue }, new double[] { double.MaxValue },
                    new double[] { 1 }, new double[][] { new double[] { 0 }, new double[] { 0 } },
                    (int n, double dt, double[] args, double[][] varargs, double[] signalVals) => {
                        if (n == 0) {
                            whiteNoise_ARMA = new double[signalVals.Length];

                            for (int i = 0; i < signalVals.Length; i++) {
                                whiteNoise_ARMA[i] = Randomizer.NormalRand(0, args[0]);
                            }
                        }

                        int p = varargs[0].Length;
                        int q = varargs[1].Length;

                        double res = whiteNoise_ARMA[n];

                        for (int i = 0; i < q && n - i > 0; i++) {
                            res += whiteNoise_ARMA[n - i - 1] * varargs[1][i];
                        }

                        for (int i = 0; i < p && n - i > 0; i++) {
                            res -= signalVals[n - i - 1] * varargs[0][i];
                        }

                        return res;
                    });
            randomModels.Add(ARMA);

            if (presets != null) {
                foreach (var model in discreteModels) {
                    if (!presets.ContainsKey(model.ModelId)) {
                        continue;
                    }

                    foreach (var preset in presets[model.ModelId]) {
                        model.presets.Add(preset);
                    }
                }

                foreach (var model in randomModels) {
                    if (!presets.ContainsKey(model.ModelId)) {
                        continue;
                    }

                    foreach (var preset in presets[model.ModelId]) {
                        model.presets.Add(preset);
                    }
                }

                foreach (var model in continiousModels) {
                    if (!presets.ContainsKey(model.ModelId)) {
                        continue;
                    }

                    foreach (var preset in presets[model.ModelId]) {
                        model.presets.Add(preset);
                    }
                }
            }
        }

        public static string defaultPath = "modelsPresets.txt";

        public static List<ChannelConstructor> discreteModels;

        public static List<ChannelConstructor> continiousModels;

        public static List<ChannelConstructor> randomModels;

        public static DateTime defaultStartDateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0);

        private static double[] whiteNoise_ARMA = null;

        private static int superposCounter = 0;
        private static int multCounter = 0;

        public static void ResetCounters() {
            foreach (var model in discreteModels) {
                model.ResetCounter();
            }

            foreach (var model in continiousModels) {
                model.ResetCounter();
            }

            foreach (var model in randomModels) {
                model.ResetCounter();
            }

            superposCounter = 0;
            multCounter = 0;
        }

        public static Channel LinearSuperpos(double[] a, Channel[] channels, bool isPreview) {
            if (a.Length != channels.Length + 1 || channels.Length == 0) {
                throw new Exception("Not enough arguments");
            }

            var res = new Channel(channels[0].SamplesCount);
            res.SamplingFrq = channels[0].SamplingFrq;
            res.StartDateTime = channels[0].StartDateTime;
            res.Source = "Суперпозиция";
            res.Name = "Суперпозиция_" + superposCounter.ToString();

            if (!isPreview) {
                superposCounter++;
            }

            for (int i = 0; i < res.SamplesCount; i++) {
                res.values[i] = a[0];
                for (int j = 0; j < channels.Length; j++) {
                    res.values[i] += channels[j].values[i] * a[j + 1];
                }
            }

            return res;
        }

        public static Channel MultiplicativeSuperpos(double[] a, Channel[] channels, bool isPreview) {
            if (a.Length != channels.Length + 1 || channels.Length == 0) {
                throw new Exception("Not enough arguments");
            }

            var res = new Channel(channels[0].SamplesCount);
            res.SamplingFrq = channels[0].SamplingFrq;
            res.StartDateTime = channels[0].StartDateTime;
            res.Source = "Произведение";
            res.Name = "Произведение_" + superposCounter.ToString();

            if (!isPreview) {
                multCounter++;
            }

            for (int i = 0; i < res.SamplesCount; i++) {
                res.values[i] = a[0];
                for (int j = 0; j < channels.Length; j++) {
                    res.values[i] *= Math.Pow(channels[j].values[i], a[j + 1]);
                }
            }

            return res;
        }
    }
}
