using Nancy;

namespace ShoppingCart.EventFeed
{
    public class EventFeedModule : NancyModule
    {
        public EventFeedModule(IEventStore eventStore) : base("/events")
        {
            Get("/", _ =>
            {
                if (!long.TryParse(Request.Query.start.Value, out long firstEventSequenceNumber))
                {
                    firstEventSequenceNumber = 0;
                }

                if (!long.TryParse(Request.Query.end.Value, out long lastEventSequenceNumber))
                {
                    lastEventSequenceNumber = long.MaxValue;
                }

                return eventStore.GetEvents(firstEventSequenceNumber, lastEventSequenceNumber);
            });
        }
    }
}
