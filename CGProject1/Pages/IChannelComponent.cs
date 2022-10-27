using System.Threading.Tasks;
using CGProject1.SignalProcessing;

namespace CGProject1.Pages
{
    public interface IChannelComponent
    {
        void Reset(Signal signal);

        void AddChannel(Channel channel);

        Task UpdateActiveSegment(int start, int end);
    }
}