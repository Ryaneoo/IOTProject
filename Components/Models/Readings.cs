public class SensorReading
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}