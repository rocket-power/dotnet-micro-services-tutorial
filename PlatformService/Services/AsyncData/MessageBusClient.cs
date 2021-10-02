using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PlatformService.DTOs;
using RabbitMQ.Client;

namespace PlatformService.Services.AsyncData
{
  public class MessageBusClient : IMessageBusClient
  {
    private readonly IConfiguration _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public MessageBusClient(IConfiguration config)
    {
      _config = config;
      var factory = new ConnectionFactory()
      {
        HostName = _config["RabbitMQHost"],
        Port = int.Parse(_config["RabbitMQPort"])
      };

      try
      {
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);

        // Subscribe to ConnectionShutdown event
        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;

        Console.WriteLine("--> Connected to Message Bus");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"--> Could not connect to Message Bus : {ex.Message}");
      }
    }

    public void PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
    {
      var message = JsonSerializer.Serialize(platformPublishedDto);

      if (_connection.IsOpen)
      {
        Console.WriteLine("--> RabbitMQ connection is open, sending message...");
        SendMessage(message);
      }
      else
      {
        Console.WriteLine("--> RabbitMQ connection is closed");
      }
    }

    private void SendMessage(string message)
    {
      var body = Encoding.UTF8.GetBytes(message);

      _channel.BasicPublish(exchange: "trigger", routingKey: "", basicProperties: null, body: body);

      Console.WriteLine($"We have sent {message}");
    }

    public void Dispose()
    {
      Console.WriteLine("--> Message Bus Disposed");

      if (_channel.IsOpen)
      {
        _channel.Close();
        _connection.Close();
      }
    }

    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
      Console.WriteLine("--> RabbitMQ Connection Shutdown");
    }
  }
}