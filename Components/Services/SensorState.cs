using System.Threading;

public class SensorState
{
    private SensorReading? _latest;
    public SensorReading? Latest => _latest;

    public void SetLatest(SensorReading reading) => Interlocked.Exchange(ref _latest, reading);
}