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

	public class Note : Event
	{
		public const int G = 0, R = 1, Y = 2, B = 3, O = 4;
		public int note;

		public Note(int note, long tick, long sus)
		{
			this.note = note;
			this.tick = tick;
			this.sus = sus;
		}
	}

	public class NoteSection : Event
	{

		public NoteSection(long tick, long sus)
		{
			this.tick = tick;
			this.sus = sus;
		}
	}

	public class Section
	{
		public string name;
		public long tick;

		public Section(long tick, string name)
		{
			this.name = name;
			this.tick = tick;
		}
	}

	public class Sync
	{
		public long tick, num;
		public bool isBPM;

		public Sync(long tick, long num, bool isBPM)
		{
			this.tick = tick;
			this.num = num;
			this.isBPM = isBPM;
		}
	}
}