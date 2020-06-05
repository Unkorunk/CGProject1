using System;
using System.Collections.Generic;
using System.Text;

namespace CGProject1.SignalProcessing {
    public class ModelPreset {
        public ModelPreset(int id, double[] args, double[][] varargs) {
            this.ModelId = id;

            this.args = Clone(args);

            this.varargs = Clone(varargs);
        }

        public int ModelId { get; }

        public double[] Args { get { return Clone(this.args); } }
        public double[][] VarArgs { get { return Clone(this.varargs); } }


        private double[] args;
        private double[][] varargs;

        private double[] Clone(double[] arr) {
            var res = new double[arr.Length];

            for (int i = 0; i < arr.Length; i++) {
                res[i] = arr[i];
            }

            return res;
        }

        private double[][] Clone(double[][] arr) {
            var res = new double[arr.Length][];
            for (int i = 0; i < arr.Length; i++) {
                res[i] = Clone(arr[i]);
            }
            return res;
        }
    }
}
