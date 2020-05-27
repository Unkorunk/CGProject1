using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Policy;
using System.Windows.Navigation;

namespace CGProject1.SignalProcessing {
    public class Modelling {
        static Modelling() {
            discreteModels = new List<ChannelConstructor>();
            var delayedPulse = new ChannelConstructor("Задержанный импульс", 0,
                    new string[] { "Задержка импульса" }, new double[] { double.MinValue }, new double[] { double.MaxValue },
                    (int n, double deltaTime, double[] args, double[] signalVals) => {
                        if (n == (int)args[0]) {
                            return 1;
                        }
                        return 0;
                    });
            discreteModels.Add(delayedPulse);

            var delayedRise = new ChannelConstructor("Задержанный скачок", 1,
                    new string[] { "Задержка скачка" }, new double[] { double.MinValue }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        if (n < args[0]) {
                            return 0;
                        }
                        return 1;
                    });
            discreteModels.Add(delayedRise);

            var discretExp = new ChannelConstructor("Дискр. уб. экспонента", 2,
                    new string[] { "Основание экспоненты" }, new double[] { 0 }, new double[] { 1 },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        return Math.Pow(args[0], n);
                    });
            discreteModels.Add(discretExp);

            var discretSin = new ChannelConstructor("Дискр. синусоида", 3,
                    new string[] { "Амплитуда", "Круг. частота", "Нач. фаза" },
                    new double[] { double.MinValue, 0, 0 }, new double[] { double.MaxValue, Math.PI, Math.PI * 2 },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        return args[0] * Math.Sin(n * args[1] + args[2]);
                    });
            discreteModels.Add(discretSin);

            var meandr = new ChannelConstructor("Меандр", 4,
                    new string[] { "Период" }, new double[] { 2 }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        int l = (int)args[0];
                        if ((n % l) < l / 2.0) {
                            return 1;
                        }
                        return -1;
                    });
            discreteModels.Add(meandr);

            var saw = new ChannelConstructor("Пила", 5,
                    new string[] { "Период" }, new double[] { 2 }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        int l = (int)args[0];
                        return (n % l) * 1.0 / l;
                    });
            discreteModels.Add(saw);

            continiousModels = new List<ChannelConstructor>();

            var withExpEnvelope = new ChannelConstructor("С эксп. огибающей", 6,
                    new string[] { "Амплитуда", "Ширина огибающей", "Частота несущей", "Нач. фаза несущей" },
                    new double[] { double.MinValue, 1e-6, 0, 0 },
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, Math.PI * 2 },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        double t = n * dt;
                        return args[0] * Math.Exp(-t / args[1]) * Math.Cos(2 * Math.PI * args[2] * t + args[3]);
                    });
            continiousModels.Add(withExpEnvelope);

            var withBalancedEnvelope = new ChannelConstructor("С балансной огибающей", 7,
                    new string[] { "Амплитуда", "Частота огибающей", "Частота несущей", "Нач. фаза несущей" },
                    new double[] { double.MinValue, 0, 0, 0 },
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, Math.PI * 2 },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        double t = n * dt;
                        return args[0] * Math.Cos(2 * Math.PI * args[1] * t) * Math.Cos(2 * Math.PI * args[2] * t + args[3]);
                    });
            continiousModels.Add(withBalancedEnvelope);

            var withTonicEnvelope = new ChannelConstructor("С тональной огибающей", 8,
                    new string[] { "Амплитуда", "Индекс глубины модуляции", "Частота огибающей", "Частота несущей", "Нач. фаза несущей" },
                    new double[] { double.MinValue, 0, 0, 0, 0 },
                    new double[] { double.MaxValue, 1, double.MaxValue, double.MaxValue, 2 * Math.PI },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        double t = n * dt;
                        return args[0] * (1 + args[1] * Math.Cos(2 * Math.PI * args[2] * t)) * Math.Cos(2 * Math.PI * args[3] * t + args[4]);
                    });
            continiousModels.Add(withTonicEnvelope);

            randomModels = new List<ChannelConstructor>();

            var testUniform = new ChannelConstructor("Проверка равномерного распределения", 9,
                    new string[] { "Величина выборки", "Нижняя граница", "Верхняя граница" },
                    new double[] { 0, 0, 0 }, new double[] { 1000000000, 1000000000, 1000000000 },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        int a = (int)args[1];
                        int b = (int)args[2];
                        if (n == 0) {
                            int t = (int)args[0];
                            randomVals = new Dictionary<int, int>();

                            for (int i = 0; i < t; i++) {
                                int cur = Randomizer.UniformRand(a, b);
                                if (!randomVals.ContainsKey(cur)) {
                                    randomVals.Add(cur, 0);
                                }

                                randomVals[cur]++;
                            }
                        }

                        if (randomVals.ContainsKey(n)) {
                            return randomVals[n];
                        }

                        return 0;

                    });
            randomModels.Add(testUniform);

            var testNormal = new ChannelConstructor("Проверка нормального распределения", 10,
                    new string[] { "Величина выборки", "Медиана", "Дисперсия" },
                    new double[] { 0, 0, 0 }, new double[] { 1000000000, 1000000000, 1000000000 },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        int m = (int)args[1];
                        int d = (int)args[2];
                        if (n == 0) {
                            int t = (int)args[0];
                            randomVals = new Dictionary<int, int>();

                            for (int i = 0; i < t; i++) {
                                int cur = Randomizer.NormalRand(m, d);
                                if (!randomVals.ContainsKey(cur)) {
                                    randomVals.Add(cur, 0);
                                }

                                randomVals[cur]++;
                            }
                        }

                        if (randomVals.ContainsKey(n)) {
                            return randomVals[n];
                        }

                        return 0;

                    });
            randomModels.Add(testNormal);

            var uniformWhiteNoise = new ChannelConstructor("Белый шум (равномерный)", 11,
                    new string[] { "Нижняя граница интервала", "Верхняя граница интервала" },
                    new double[] { double.MinValue, double.MinValue }, new double[] { double.MaxValue, double.MaxValue },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        return Randomizer.UniformRand(args[0], args[1]);
                    });
            randomModels.Add(uniformWhiteNoise);

            var normalWhiteNoise = new ChannelConstructor("Белый шум (нормальный)", 11,
                    new string[] { "Среднее", "Дисперсия" },
                    new double[] { double.MinValue, double.MinValue }, new double[] { double.MaxValue, double.MaxValue },
                    (int n, double dt, double[] args, double[] signalVals) => {
                        return Randomizer.NormalRand(args[0], args[1]);
                    });
            randomModels.Add(normalWhiteNoise);

            //var ARMA = new ChannelConstructor("АРСС", 12,
            //        new string[] { "Дисперсия", "P", "Q" },
            //        new double[] { double.MinValue, 0, 0 }, new double[] { double.MaxValue, 1000000000, 1000000000 },
            //        (int n, double dt, double[] args, double[] signalVals) => {
            //            if (n == 0) {
            //                whiteNoise_ARMA = new double[signalVals.Length];

            //                for (int i = 0; i < signalVals.Length; i++) {
            //                    whiteNoise_ARMA[i] = Randomizer.NormalRand(0, (int)args[0]);
            //                }
            //            }

            //            int p = (int)args[1];
            //            int q = (int)args[2];

            //            double res = whiteNoise_ARMA[n];

            //            for (int i = )

            //            return Randomizer.NormalRand(args[0], args[1]);
            //        });
            //randomModels.Add(ARMA);
        }

        public static List<ChannelConstructor> discreteModels;

        public static List<ChannelConstructor> continiousModels;

        public static List<ChannelConstructor> randomModels;

        public static DateTime defaultStartDateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0);

        private static Dictionary<int, int> randomVals = null;

        private static double[] whiteNoise_ARMA = null;

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
        } 
    }
}
