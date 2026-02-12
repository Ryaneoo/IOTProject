

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

public class MqttSubscribers : BackgroundService
{
    // _ is used so that it will be forgotten , a fire and forget method, as the remaining data is not important
    private readonly LoadingBayState _states; //allows reading of sensor readings
    private IMqttClient? _clients;  //allows subscribing to MQTT
    //initalise all variables needed for subscribing
    private  string Brokers = "ab4d2e8a48a6402ca2ff5ec16a2b05a2.s1.eu.hivemq.cloud";
    private  int Ports = 8883;
    private  string Topics = "tp/eng/iotp_project/grp_02/loadingbay/data";
    private  string Usernames = "iotp_grp02";
    private  string Passwords = "Iotp_grp02";

    public MqttSubscribers(LoadingBayState states)
    {
        _states = states; //update of state
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("MqttSubscriber started");
        var factory = new MqttFactory();
        _clients = factory.CreateMqttClient(); //using MQTT client

        _clients.UseConnectedHandler(_ =>
        {
            Console.WriteLine("MQTT connected");
        });

        _clients.UseDisconnectedHandler(e =>
        {
            Console.WriteLine("MQTT disconnected: " + e.Exception?.Message);
        });

        _clients.UseApplicationMessageReceivedHandler(e => //adds the data on message recieved (subscriber)
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
                var reading = new LoadingBayReading
                {
                    LoadingBayStatus = doc.RootElement.GetProperty("loadingBayStatus").GetString()
                };

                _states.SetLatest(reading, topic, payload);  //updating the class based on latest recieved data
            }
            catch (Exception ex)
            {
                _states.SetError(ex.Message, topic, payload); //if error encountered display it(used for debugging)
            }
        });
        //initalising the connection
        var options = new MqttClientOptionsBuilder()
            .WithClientId($"sub-{Guid.NewGuid():N}")
            .WithTcpServer(Brokers, Ports)
            .WithCredentials(Usernames, Passwords)
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
            Console.WriteLine("Connecting..."); //console logging used for debugging
            await _clients.ConnectAsync(options);
            Console.WriteLine("ConnectAsync done. IsConnected=" + _clients.IsConnected);

            Console.WriteLine("Subscribing to: " + Topics);
            await _clients.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(Topics)
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
        if (_clients.IsConnected)
            await _clients.DisconnectAsync();
    }
}