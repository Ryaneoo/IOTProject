public class SensorState
{
    public SensorReading? Latest { get; private set; } //gets the latest reading and all of the info below

    public string? LastTopic { get; private set; }
    public string? LastRawPayload { get; private set; }
    public string? LastError { get; private set; }

    public void SetLatest(SensorReading reading, string? topic = null, string? raw = null) //tracks last recieved, used for debugging only
    {
        Latest = reading;
        LastTopic = topic;
        LastRawPayload = raw;
        LastError = null;
    }

    public void SetError(string error, string? topic = null, string? raw = null) //gets the error if there is, used for debugging only 
    {
        LastError = error;
        LastTopic = topic;
        LastRawPayload = raw;
    }
}