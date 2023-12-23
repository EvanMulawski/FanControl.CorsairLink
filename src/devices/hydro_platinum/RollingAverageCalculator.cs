namespace CorsairLink.Devices.HydroPlatinum;

internal sealed class RollingAverageCalculator
{
    private readonly int _windowSize;
    private double _sum;
    private readonly Queue<double> _values;

    public RollingAverageCalculator(int windowSize, double defaultAverage)
    {
        _windowSize = windowSize;
        _sum = 0;
        _values = new(windowSize);

        for (var i = 0; i < _windowSize; i++)
        {
            _values.Enqueue(defaultAverage);
            _sum += defaultAverage;
        }
    }

    public double Update(double newValue)
    {
        if (_values.Count == _windowSize)
        {
            double removedValue = _values.Dequeue();
            _sum -= removedValue;
        }

        _values.Enqueue(newValue);
        _sum += newValue;

        return _sum / _values.Count;
    }
}
