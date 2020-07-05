using System;
using System.Windows.Controls;

namespace CGProject1.Pages {
    public interface IChannelComponent {
        void Reset(Signal signal);

        void AddChannel(Channel channel);

        void UpdateActiveSegment(int start, int end);
    }
}
