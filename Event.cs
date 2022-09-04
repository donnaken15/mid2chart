namespace mid2chart
{
    public abstract class Event
    {
        public long tick, sus;
    }

    public class TrackEvent : Event
    {
        public string text;

        public TrackEvent(long tick, string text)
        {
            this.tick = tick;
            this.text = text;
        }
    }
}