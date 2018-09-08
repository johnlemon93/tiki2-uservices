using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.EventFeed
{
    public class EventStore : IEventStore
    {
        private const string ConnectionString = "discover://http://localhost:2113/";
        private readonly IEventStoreConnection m_DbConnection = EventStoreConnection.Create(ConnectionString);

        public async Task Raise(string eventName, object content)
        {
            // opens the connection to EventStore
            await m_DbConnection.ConnectAsync().ConfigureAwait(false);

            var contentJson = JsonConvert.SerializeObject(content);

            // maps OccurredAt and EventName to metadata to be stored along with the event
            var metaDataJson =
              JsonConvert.SerializeObject(new EventMetadata
              {
                  OccurredAt = DateTimeOffset.Now,
                  EventName = eventName
              });

            // EventData is EventStore's representation of an event
            var eventData = new EventData(
              Guid.NewGuid(),
              "ShoppingCartEvent",
              isJson: true,
              data: Encoding.UTF8.GetBytes(contentJson),
              metadata: Encoding.UTF8.GetBytes(metaDataJson)
            );

            // writes event to EventStore database
            await m_DbConnection.AppendToStreamAsync("ShoppingCart", ExpectedVersion.Any, eventData);
        }

        public async Task<IEnumerable<Event>> GetEvents(long firstEventSequenceNumber, long lastEventSequenceNumber)
        {
            await m_DbConnection.ConnectAsync().ConfigureAwait(false);

            var result = await m_DbConnection.ReadStreamEventsForwardAsync(
              "ShoppingCart",
              start: (int)firstEventSequenceNumber,
              count: (int)(lastEventSequenceNumber - firstEventSequenceNumber),
              resolveLinkTos: false).ConfigureAwait(false);

            return
              result.Events
                .Select(ev =>
                  new
                  {
                      Content = JsonConvert.DeserializeObject(
                      Encoding.UTF8.GetString(ev.Event.Data)),
                      Metadata = JsonConvert.DeserializeObject<EventMetadata>(
                      Encoding.UTF8.GetString(ev.Event.Data))
                  })
                .Select((ev, i) =>
                  new Event(
                    i + firstEventSequenceNumber,
                    ev.Metadata.OccurredAt,
                    ev.Metadata.EventName,
                    ev.Content));
        }

        private class EventMetadata
        {
            public DateTimeOffset OccurredAt { get; set; }
            public string EventName { get; set; }
        }
    }
}