using System;
using System.Collections.Generic;
using System.ComponentModel;

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
                    new string[] { "Период" }, new double[] { 1 }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args) => {
                        int l = (int)args[0];
                        if ((n % l) < l / 2.0) {
                            return 1;
                        }
                        return -1;
                    });
            discreteModels.Add(meandr);

            var saw = new ChannelConstructor("Пила", 5,
                    new string[] { "Период" }, new double[] { 1 }, new double[] { double.MaxValue },
                    (int n, double dt, double[] args) => {
                        int l = (int)args[0];
                        return (n % l) * 1.0 / l;
                    });
            discreteModels.Add(saw);

            continiousModels = new List<ChannelConstructor>();
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
