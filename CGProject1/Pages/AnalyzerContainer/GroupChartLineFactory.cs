using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CGProject1.Chart;

namespace CGProject1.Pages.AnalyzerContainer
{
    public class GroupChartLineFactory
    {
        public string Title { get; }
        public IList<ChartLineFactory> Factories { get; private set; }
        public Func<ChartLineFactory, ChartLine> Selector { get; }

        public GroupChartLineFactory([NotNull] string title, [NotNull] Func<ChartLineFactory, ChartLine> selector)
        {
            Title = title;
            Factories = new List<ChartLineFactory>();
            Selector = selector;
        }

        public IEnumerable<ChartLine> Process()
        {
            ClearDeletedFactories();

            foreach (var factory in Factories)
            {
                yield return Selector(factory);
            }
        }

        private void ClearDeletedFactories()
        {
            Factories = Factories.Where(factory => !factory.Deleted).ToList();
        }
    }
}