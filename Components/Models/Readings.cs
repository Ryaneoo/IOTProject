public class SensorReading // initalise a class to contain the necessary variables to retrieve and display the corresponding data
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}