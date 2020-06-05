using System;
using System.Collections.Generic;

namespace CGProject1.SignalProcessing {
    public class ChannelConstructor {
        public ChannelConstructor(string modelName, int id, string[] argsNames, string[] varargNames,
                double[] minValues, double[] maxValues, double[] defaultValues, double[][] defaultVarargs, Model modelingRule = null) {
            this.ModelId = id;
            this.ModelName = modelName;
            this.ArgsNames = argsNames;
            this.VarArgNames = varargNames;
            this.MinArgValues = minValues;
            this.MaxArgValues = maxValues;
            this.DefaultValues = defaultValues;
            this.DefaultVarargValues = defaultVarargs;

            this.LastValues = this.DefaultValues;
            this.LastVarargs = this.DefaultVarargValues;

            if (modelingRule == null) {
                modelingRule = (int n, double deltaTime, double[] args, double[][] varargs, double[] signalVals) => {
                    return Math.Cos(n * deltaTime) * 10;
                };
            }

            this.modelDelegate = modelingRule;
        }

        public List<ModelPreset> presets = new List<ModelPreset>();

        private Model modelDelegate;

        private int channelCounter = 0;

        public string ModelName { get; }
        public int ModelId { get; }

        public string[] ArgsNames { get; }
        public string[] VarArgNames { get; }
        public double[] MinArgValues { get; }
        public double[] MaxArgValues { get; }

        public double[] DefaultValues { get; }

        public double[][] DefaultVarargValues { get; }

        public double[] LastValues { get; set; }

        public double[][] LastVarargs { get; set; }


        public delegate double Model(int n, double deltaTime, double[] args, double[][] varargs, double[] signalVals);

        public void AddPreset(double[] args, double[][] varargs) {
            var newPreset = new ModelPreset(this.ModelId, args, varargs);
            presets.Add(newPreset);
        }

        public void ResetCounter() {
            channelCounter = 0;
        }

        public Channel CreatePreviewChannel(int samplesCount, double[] args, double[][] varargs, double samplingFrq, DateTime startDateTime) {
            this.LastValues = args;
            this.LastVarargs = varargs;
            var channel = ConstructChannel(samplesCount, args, varargs, samplingFrq, startDateTime);

            channel.Name = "Model_" + this.ModelId.ToString() + "_" + this.channelCounter.ToString() + "_Preview";

            return channel;
        }

        public Channel CreateChannel(int samplesCount, double[] args, double[][] varargs, double samplingFrq, DateTime startDateTime) {
            this.LastValues = args;
            this.LastVarargs = varargs;
            var channel = ConstructChannel(samplesCount, args, varargs, samplingFrq, startDateTime);

            channel.Name = "Model_" + this.ModelId.ToString() + "_" + this.channelCounter.ToString();
            this.channelCounter++;

            return channel;
        }
        
        public void IncCounter() {
            this.channelCounter++;
        }

        private Channel ConstructChannel(int samplesCount, double[] args, double[][] varargs, double samplingFrq, DateTime startDateTime) {
            if (args.Length < ArgsNames.Length || varargs.Length < VarArgNames.Length) {
                throw new Exception("Not enough arguments");
            }

            var channel = new Channel(samplesCount);
            channel.SamplingFrq = samplingFrq;
            channel.StartDateTime = startDateTime;
            channel.Source = this.ModelName;

            for (int i = 0; i < samplesCount; i++) {
                channel.values[i] = modelDelegate(i, channel.DeltaTime, args, varargs, channel.values);
            }

            return channel;
        }
    }
}
