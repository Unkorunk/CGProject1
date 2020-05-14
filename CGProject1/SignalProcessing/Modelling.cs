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

            continiousModels = new List<ChannelConstructor>();
        }

        public static List<ChannelConstructor> discreteModels;

        public static List<ChannelConstructor> continiousModels;

        public static DateTime defaultStartDateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0);
    }
}
