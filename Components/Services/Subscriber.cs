using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using MQTTnet;

public class MqttSubscriber : BackgroundService
{
    private readonly SensorState _state;
    private IMqttClient? _client;

    private const string Broker = "test.mosquitto.org";
    private const int Port = 1883;
    private const string Topic = "tp/eng/iotp_project/grp_02/bme280";

    public MqttSubscriber(SensorState state) => _state = state;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient(); 

        _client.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var payload = e.ApplicationMessage.ConvertPayloadToString();
                var doc = JsonDocument.Parse(payload);

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

        if (_client.IsConnected)
            await _client.DisconnectAsync();
    }
}