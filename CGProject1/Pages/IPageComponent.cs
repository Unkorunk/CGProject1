using System;
using System.Collections.Generic;
using System.Text;

namespace CGProject1.Pages {
    interface IPageComponent {
        void Reset(Signal signal);

        void AddChannel(Channel channel);

        void UpdateActiveSegment(int start, int end);
    }
}
