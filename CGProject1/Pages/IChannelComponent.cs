using System;
using System.Windows.Controls;
using CGProject1.SignalProcessing;

namespace CGProject1.Pages {
    public interface IChannelComponent {
        void Reset(Signal signal);

        void AddChannel(Channel channel);

        void UpdateActiveSegment(int start, int end);
    }
}
