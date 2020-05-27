using System;
using System.Collections.Generic;
using System.Text;

namespace CGProject1.SignalProcessing {
    public static class Randomizer {
        private static Random rnd = new Random();

        public static double UniformRand(double a, double b) {
            return a + (b - a) * rnd.NextDouble();
        }

        public static int UniformRand(int a, int b) {
            return rnd.Next(a, b);
        }

        public static int NormalRand(int a, int d) {
            double n = NormalDouble();
            return a + (int)Math.Round(d * n);
        }

        public static double NormalRand(double a, double d) {
            double n = NormalDouble();
            return a + d * n;
        }

        private static double NormalDouble() {
            double n = 0;
            for (int i = 0; i < 12; i++) {
                n += rnd.NextDouble();
            }
            return n - 6;
        }
    }
}
