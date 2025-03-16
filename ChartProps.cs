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
		public const int G = 0, R = 1, Y = 2, B = 3, O = 4, H = 5, T = 6, S = 7, ohplsno = 8; // never heard of the force strum marker i think
		public int note;

		public Note(int note, long tick, long sus)
		{
			this.note = note;
			this.tick = tick;
			this.sus = sus;
		}
	}

	public class Special : Event
	{
		public const int P1 = 0, P2 = 1, SP = 2, Pow = 3;
		public int flag;

		public Special(int flag, long tick, long sus)
		{
			this.flag = flag;
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