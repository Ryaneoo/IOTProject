using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using MQTTnet;

public class MqttSubscriber : BackgroundService
{
    // _ is used so that it will be forgotten , a fire and forget method, as the remaining data is not important
    private readonly SensorState _state; //allows reading of sensor readings
    private IMqttClient? _client; //allows subscribing to MQTT
    //initalise all variables needed for subscribing
    private const string Broker = "test.mosquitto.org";
    private const int Port = 1883;
    private const string Topic = "tp/eng/iotp_project/grp_02/bme280";

    public MqttSubscriber(SensorState state) => _state = state; //update of state

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient(); //using MQTT client

        _client.ApplicationMessageReceivedAsync += e => //adds the data on message recieved (subscriber)
        {
            try
            {
                //converts received message to string then json 
                var payload = e.ApplicationMessage.ConvertPayloadToString(); 
                var doc = JsonDocument.Parse(payload);
                // sets the variable to the assigned data
                var reading = new SensorReading
                {
                    Temperature = doc.RootElement.GetProperty("temperature").GetDouble(),
                    Humidity = doc.RootElement.GetProperty("humidity").GetDouble(),
                    Pressure = doc.RootElement.GetProperty("pressure").GetDouble(),
                    Timestamp = DateTime.UtcNow
                };

                _state.SetLatest(reading);
            }
            catch { }
  
            return Task.CompletedTask;
        };
        //initalising the connection
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(Broker, Port)
            .WithCleanSession()
            .Build();

        // connect
        await _client.ConnectAsync(options, stoppingToken);

        // subscribe
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(Topic)
            .Build();

        await _client.SubscribeAsync(subscribeOptions, stoppingToken);

        // keep running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        //if connected, disconnects, so it can re-connect from the start to get the new info
        if (_client.IsConnected)
            await _client.DisconnectAsync();
    }
}