using System.ComponentModel;

namespace Medley.CollectorUtil;

public class SortableBindingList<T> : BindingList<T>
{
    private bool _isSorted;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    private PropertyDescriptor? _sortProperty;

    public SortableBindingList() : base() { }

    public SortableBindingList(IList<T> list) : base(list) { }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _isSorted;

    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        var items = Items as List<T>;
        if (items == null) return;

        var comparer = new PropertyComparer<T>(prop, direction);
        items.Sort(comparer);

        _sortProperty = prop;
        _sortDirection = direction;
        _isSorted = true;

        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    protected override void RemoveSortCore()
    {
        _isSorted = false;
        _sortProperty = null;
    }

    private class PropertyComparer<TItem> : IComparer<TItem>
    {
        private readonly PropertyDescriptor _property;
        private readonly ListSortDirection _direction;

        public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
        {
            _property = property;
            _direction = direction;
        }

        public int Compare(TItem? x, TItem? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return _direction == ListSortDirection.Ascending ? -1 : 1;
            if (y == null) return _direction == ListSortDirection.Ascending ? 1 : -1;

            var xValue = _property.GetValue(x);
            var yValue = _property.GetValue(y);

            if (xValue == null && yValue == null) return 0;
            if (xValue == null) return _direction == ListSortDirection.Ascending ? -1 : 1;
            if (yValue == null) return _direction == ListSortDirection.Ascending ? 1 : -1;

            int result;
            if (xValue is IComparable comparableX)
            {
                result = comparableX.CompareTo(yValue);
            }
            else if (xValue is string strX && yValue is string strY)
            {
                result = string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                result = string.Compare(xValue.ToString(), yValue.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }
}
