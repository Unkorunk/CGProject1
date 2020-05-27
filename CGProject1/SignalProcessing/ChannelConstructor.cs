using System;
using System.Linq;
using System.Security.RightsManagement;

namespace CGProject1.SignalProcessing {
    public class ChannelConstructor {
        public ChannelConstructor(string modelName, int id, string[] argsNames,
                double[] minValues, double[] maxValues, Model modelingRule = null) {
            this.ModelId = id;
            this.ModelName = modelName;
            this.ArgsNames = argsNames;
            this.MinArgValues = minValues;
            this.MaxArgValues = maxValues;

            if (modelingRule == null) {
                modelingRule = (int n, double deltaTime, double[] args, double[] signalVals) => {
                    return Math.Cos(n * deltaTime) * 10;
                };
            }

            this.modelDelegate = modelingRule;
        }

        private Model modelDelegate;

        private int channelCounter = 0;

        public string ModelName { get; }
        public int ModelId { get; }

        public string[] ArgsNames { get; }
        public double[] MinArgValues { get; }
        public double[] MaxArgValues { get; }

        public delegate double Model(int n, double deltaTime, double[] args, double[] signalVals);

        public void ResetCounter() {
            channelCounter = 0;
        }

        public Channel CreatePreviewChannel(int samplesCount, double[] args, double samplingFrq, DateTime startDateTime) {
            var channel = ConstructChannel(samplesCount, args, samplingFrq, startDateTime);

            channel.Name = "Model_" + this.ModelId.ToString() + "_" + this.channelCounter.ToString() + "_Preview";

            return channel;
        }

        public Channel CreateChannel(int samplesCount, double[] args, double samplingFrq, DateTime startDateTime) {
            var channel = ConstructChannel(samplesCount, args, samplingFrq, startDateTime);

            channel.Name = "Model_" + this.ModelId.ToString() + "_" + this.channelCounter.ToString();
            this.channelCounter++;

            return channel;
        }

        private Channel ConstructChannel(int samplesCount, double[] args, double samplingFrq, DateTime startDateTime) {
            if (args.Length < ArgsNames.Length) {
                throw new Exception("Not enough arguments");
            }

            var channel = new Channel(samplesCount);
            channel.SamplingFrq = samplingFrq;
            channel.StartDateTime = startDateTime;
            channel.Source = this.ModelName;

            for (int i = 0; i < samplesCount; i++) {
                channel.values[i] = modelDelegate(i, channel.DeltaTime, args, channel.values);
            }

            return channel;
        }
    }
}
