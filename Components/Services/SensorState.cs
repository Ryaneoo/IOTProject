using System.Threading;

public class SensorState
{
    // _ is used so that it will be forgotten , a fire and forget method, as the remaining data is not important
    private SensorReading? _latest; //initalise variable
    public SensorReading? Latest => _latest; //initalise variable public to allow passing data to Info.razor and Home.razor

    public void SetLatest(SensorReading reading) => Interlocked.Exchange(ref _latest, reading);
    // be able to consistently update the reading to change based on the latest reading received interlocked.exchange is used as this is runs in the back
}