using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Navigation;

namespace CGProject1.SignalProcessing {
    public class Modelling {
        static Modelling() {
            discreteModels = new List<ChannelConstructor>();
            var delayedPulse = new ChannelConstructor("Задержанный импульс", 0,
                    new string[] { "Задержка импульса" }, new double[] { double.MinValue }, new double[] { double.MaxValue },
                    (int n, double deltaTime, double[] args) => {
                        if (n == (int)args[0]) {
                            return 1;
                        }
                        return 0;
                    });
            discreteModels.Add(delayedPulse);

            var delayedRise = new ChannelConstructor("Задержанный скачок", 1,
                    new string[] { "Задержка скачка" }, new double[] { double.MinValue }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args) => {
                        if (n < args[0]) {
                            return 0;
                        }
                        return 1;
                    });
            discreteModels.Add(delayedRise);

            var discretExp = new ChannelConstructor("Дискр. уб. экспонента", 2,
                    new string[] { "Основание экспоненты" }, new double[] { 0 }, new double[] { 1 },
                    (int n, double dt, double[] args) => {
                        return Math.Pow(args[0], n);
                    });
            discreteModels.Add(discretExp);

            var discretSin = new ChannelConstructor("Дискр. синусоида", 3,
                    new string[] { "Амплитуда", "Круг. частота", "Нач. фаза" },
                    new double[] { double.MinValue, 0, 0 }, new double[] { double.MaxValue, Math.PI, Math.PI * 2 },
                    (int n, double dt, double[] args) => {
                        return args[0] * Math.Sin(n * args[1] + args[2]);
                    });
            discreteModels.Add(discretSin);

            var meandr = new ChannelConstructor("Меандр", 4,
                    new string[] { "Период" }, new double[] { 2 }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args) => {
                        int l = (int)args[0];
                        if ((n % l) < l / 2.0) {
                            return 1;
                        }
                        return -1;
                    });
            discreteModels.Add(meandr);

            var saw = new ChannelConstructor("Пила", 5,
                    new string[] { "Период" }, new double[] { 2 }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args) => {
                        int l = (int)args[0];
                        return (n % l) * 1.0 / l;
                    });
            discreteModels.Add(saw);

            continiousModels = new List<ChannelConstructor>();

            var withExpEnvelope = new ChannelConstructor("С эксп. огибающей", 6,
                    new string[] { "Амплитуда", "Ширина огибающей", "Частота несущей", "Нач. фаза несущей" },
                    new double[] { double.MinValue, 1e-6, 0, 0 },
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, Math.PI * 2 },
                    (int n, double dt, double[] args) => {
                        double t = n * dt;
                        return args[0] * Math.Exp(-t / args[1]) * Math.Cos(2 * Math.PI * args[2] * t + args[3]);
                    });
            continiousModels.Add(withExpEnvelope);

            var withBalancedEnvelope = new ChannelConstructor("С балансной огибающей", 7,
                    new string[] { "Амплитуда", "Частота огибающей", "Частота несущей", "Нач. фаза несущей" },
                    new double[] { double.MinValue, 0, 0, 0 },
                    new double[] { double.MaxValue, double.MaxValue, double.MaxValue, Math.PI * 2 },
                    (int n, double dt, double[] args) => {
                        double t = n * dt;
                        return args[0] * Math.Cos(2 * Math.PI * args[1] * t) * Math.Cos(2 * Math.PI * args[2] * t + args[3]);
                    });
            continiousModels.Add(withBalancedEnvelope);

            var withTonicEnvelope = new ChannelConstructor("С тональной огибающей", 8,
                    new string[] { "Амплитуда", "Индекс глубины модуляции", "Частота огибающей", "Частота несущей", "Нач. фаза несущей" },
                    new double[] { double.MinValue, 0, 0, 0, 0 },
                    new double[] { double.MaxValue, 1, double.MaxValue, double.MaxValue, 2 * Math.PI },
                    (int n, double dt, double[] args) => {
                        double t = n * dt;
                        return args[0] * (1 + args[1] * Math.Cos(2 * Math.PI * args[2] * t)) * Math.Cos(2 * Math.PI * args[3] * t + args[4]);
                    });
            continiousModels.Add(withTonicEnvelope);

            //var sin = new ChannelConstructor("Синусоида", 9,
            //        new string[] { "Амплитуда", "Частота несущей", "Нач. фаза" },
            //        new double[] { double.MinValue, 0, 0 },
            //        new double[] { double.MaxValue, double.MaxValue, Math.PI * 2 },
            //        (int n, double dt, double[] args) => {
            //            double t = n * dt;
            //            return args[0] * Math.Sin(t * args[1] + args[2]);
            //        });
            //continiousModels.Add(sin);
        }

        public static void ResetCounters() {
            foreach (var model in discreteModels) {
                model.ResetCounter();
            }

            foreach (var model in continiousModels) {
                model.ResetCounter();
            }
        }

        public static List<ChannelConstructor> discreteModels;

        public static List<ChannelConstructor> continiousModels;

        public static DateTime defaultStartDateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0);
    }
}
