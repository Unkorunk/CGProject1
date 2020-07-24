using System;
using System.Linq;
using CGProject1.Chart;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CGProject1.Pages.AnalyzerContainer
{
    public class GroupChartLineFactory : IDisposable
    {
        private List<ChartLineFactory> factories;

        public delegate void DelUpdate();
        public event DelUpdate OnUpdate;

        public string Title { get; }
        public IReadOnlyCollection<ChartLineFactory> Factories
        {
            get
            {
                ClearDeletedFactories();
                return factories;
            }
        }
        public Func<ChartLineFactory, ChartLine> Selector { get; }

        public GroupChartLineFactory([NotNull] string title, [NotNull] Func<ChartLineFactory, ChartLine> selector)
        {
            Title = title;
            factories = new List<ChartLineFactory>();
            Selector = selector;
        }

        public IEnumerable<ChartLine> Process()
        {
            foreach (var factory in Factories)
            {
                yield return Selector(factory);
            }
        }

        private void ClearDeletedFactories()
        {
            foreach (var factory in factories.Where(factory => factory.Deleted))
            {
                factory.OnDeleted -= Factory_OnDelete;
            }

            factories = factories.Where(factory => !factory.Deleted).ToList();
        }

        private void Factory_OnDelete() => OnUpdate?.Invoke();

        public void Add(ChartLineFactory chartLineFactory)
        {
            chartLineFactory.OnDeleted += Factory_OnDelete;
            factories.Add(chartLineFactory);
        }

        public void Clear()
        {
            foreach (var factory in Factories)
            {
                factory.OnDeleted -= Factory_OnDelete;
            }
            factories.Clear();
        }

        public void Dispose()
        {
            foreach (var factory in Factories)
            {
                factory.OnDeleted -= Factory_OnDelete;
            }
        }
    }
}