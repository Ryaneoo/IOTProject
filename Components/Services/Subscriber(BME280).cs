

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

public class MqttSubscriber : BackgroundService
{
    // _ is used so that it will be forgotten , a fire and forget method, as the remaining data is not important
    private readonly SensorState _state; //allows reading of sensor readings
    private IMqttClient? _client;  //allows subscribing to MQTT
    //initalise all variables needed for subscribing
    private const string Broker = "ab4d2e8a48a6402ca2ff5ec16a2b05a2.s1.eu.hivemq.cloud";
    private const int Port = 8883;
    private const string Topic = "tp/eng/iotp_project/grp_02/bme280";
    private const string Username = "iotp_grp02";
    private const string Password = "Iotp_grp02";

    public MqttSubscriber(SensorState state) => _state = state; //update of state

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("MqttSubscriber started");
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient(); //using MQTT client

        _client.UseConnectedHandler(_ =>
        {
            Console.WriteLine("MQTT connected");
        });

        _client.UseDisconnectedHandler(e =>
        {
            Console.WriteLine("MQTT disconnected: " + e.Exception?.Message);
        });

        _client.UseApplicationMessageReceivedHandler(e => //adds the data on message recieved (subscriber)
        {
            var topic = e.ApplicationMessage?.Topic;

            var payload = e.ApplicationMessage?.Payload == null
                ? ""
                : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            try
            {
                //converts received message to string then json 
                var doc = JsonDocument.Parse(payload);
                // sets the variable to the assigned data
                var reading = new SensorReading
                {
                    Temperature = doc.RootElement.GetProperty("temperature").GetDouble(),
                    Humidity = doc.RootElement.GetProperty("humidity").GetDouble(),
                    Pressure = doc.RootElement.GetProperty("pressure").GetDouble(),
                    Timestamp = DateTime.UtcNow
                };

                _state.SetLatest(reading, topic, payload);
            }
            catch (Exception ex)
            {
                _state.SetError(ex.Message, topic, payload);
            }
        });
        //initalising the connection
        var options = new MqttClientOptionsBuilder()
            .WithClientId($"sub-{Guid.NewGuid():N}")
            .WithTcpServer(Broker, Port)
            .WithCredentials(Username, Password)
            .WithCleanSession()
            .WithTls(new MqttClientOptionsBuilderTlsParameters //allows access of using hive
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12
            })
            .Build();
        // connection check via debugging 
        try
        {
            Console.WriteLine("Connecting...");
            await _client.ConnectAsync(options);
            Console.WriteLine("ConnectAsync done. IsConnected=" + _client.IsConnected);

            Console.WriteLine("Subscribing to: " + Topic);
            await _client.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(Topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
            Console.WriteLine("SubscribeAsync done");
        }
        catch (Exception ex)
        {
            Console.WriteLine("MQTT connect/subscribe error: " + ex);
        }

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
        //if connected, disconnects, so it can re-connect from the start to get the new info
        if (_client.IsConnected)
            await _client.DisconnectAsync();
    }
}