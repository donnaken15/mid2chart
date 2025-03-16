using System;
using System.Collections.Generic;

namespace mid2chart
{
	public enum Diffs
	{
		Easy,
		Medium,
		Hard,
		Expert,
		Count
	}
	public enum Insts
	{
		Guitar,
		Bass,
		Rhythm,
		Keys,
		Count
	}
	internal class Song {
		public string songname = "", artist = "", year = "", charter = "";
		public long offset = 0;
		public List<Sync> sync = new List<Sync>();
		public List<Section> sections = new List<Section>();
		public List<TrackEvent> eventsGlobal = new List<TrackEvent>();

		public List<Event>[] Tracks = new List<Event>[(int)Diffs.Count*(int)Insts.Count];
		
		public Song()
		{
			for (var i = 0; i < Tracks.Length; i++)
				Tracks[i] = new List<Event>();
		}

		internal void TapToHopo() {
			/*if(tapGuitar.Count > 0) {
				eGuitarForceHOPO = TapToHopo(eGuitarForceHOPO,tapGuitar);
				mGuitarForceHOPO = TapToHopo(mGuitarForceHOPO,tapGuitar);
				hGuitarForceHOPO = TapToHopo(hGuitarForceHOPO,tapGuitar);
				xGuitarForceHOPO = TapToHopo(xGuitarForceHOPO,tapGuitar);
				tapGuitar.Clear();
			}
			if(tapBass.Count > 0) {
				eBassForceHOPO = TapToHopo(eBassForceHOPO,tapBass);
				mBassForceHOPO = TapToHopo(mBassForceHOPO,tapBass);
				hBassForceHOPO = TapToHopo(hBassForceHOPO,tapBass);
				xBassForceHOPO = TapToHopo(xBassForceHOPO,tapBass);
				tapBass.Clear();
			}*/
			for (var i = 0; i < Tracks.Length; i++)
				TapToHopo(Tracks[i]);
		}

		private void TapToHopo(List<Event> track) {
			foreach(Event n in track) {
				if (n is Note)
				{
					if ((n as Note).note == Note.T)
						(n as Note).note = Note.H;
				}
			}
		}

		internal void FixOverlaps()
		{
			for (var i = 0; i < Tracks.Length; i++)
				Tracks[i] = FixOverlaps(Tracks[i]);
		}

		internal List<Event> FixOverlaps(List<Event> notes) {
			foreach (var e in notes) {
				var currentNote = e as Note;
				if (currentNote != null) {
					var nextNote = GetNextNote(currentNote, notes);
					if (nextNote != null && currentNote.tick+currentNote.sus >= nextNote.tick) {
						currentNote.sus = nextNote.tick-currentNote.tick-24;
					}
				}
			}
			return notes;
		}

		internal void FixBrokenChords()
		{
			for (var i = 0; i < Tracks.Length; i++)
				Tracks[i] = FixBrokenChords(Tracks[i], Math.Min(i, 12));
		}

		private List<Event> FixBrokenChords(List<Event> notes, int sec) {
			return notes;
			var fixedNotes = new List<Event>();
			foreach (var n in notes)
			{
				fixedNotes.Add(n);
				if (IsPartOfBrokenChord(n as Note, fixedNotes))
				{
					var previousNote = GetPreviousNote(n as Note, fixedNotes);
					var previousIndex = fixedNotes.IndexOf(previousNote);
					var previousTick = previousNote.tick;
					for (var j = previousIndex; j >= 0; j--, previousNote = fixedNotes[j] as Note)
					{
						//j >= 0 && previousNote.tick == previousTick; - old code incase something goes unexpectedly wrong
						if (previousNote != null)
						{
							if (previousNote.tick != previousTick) break;
							var tickDiff = n.tick - previousNote.tick; if (tickDiff < 96L) tickDiff = 0;
							fixedNotes[j] = new Note(previousNote.note, previousNote.tick, tickDiff);
							fixedNotes.Add(new Note(previousNote.note, n.tick, n.sus));
							if (n.tick - previousTick <= 64)
							{
								switch (sec)
								{
									// uncomment when ready to fix, i feel kind of trapped and don't know what to do :/
									/*case 0: xGuitarForceHOPO = AddForceHopoIfNecessary(n.tick, xGuitarForceHOPO, xGuitarForceStrum); break;
									case 1: hGuitarForceHOPO = AddForceHopoIfNecessary(n.tick, hGuitarForceHOPO, hGuitarForceStrum); break;
									case 2: mGuitarForceHOPO = AddForceHopoIfNecessary(n.tick, mGuitarForceHOPO, mGuitarForceStrum); break;
									case 3: eGuitarForceHOPO = AddForceHopoIfNecessary(n.tick, eGuitarForceHOPO, eGuitarForceStrum); break;
									case 4: xBassForceHOPO = AddForceHopoIfNecessary(n.tick, xBassForceHOPO, xBassForceStrum); break;
									case 5: hBassForceHOPO = AddForceHopoIfNecessary(n.tick, hBassForceHOPO, hBassForceStrum); break;
									case 6: mBassForceHOPO = AddForceHopoIfNecessary(n.tick, mBassForceHOPO, mBassForceStrum); break;
									case 7: eBassForceHOPO = AddForceHopoIfNecessary(n.tick, eBassForceHOPO, eBassForceStrum); break;
									case 8: break;*/
									default: throw new Exception("Invalid diff/instr. value(smaller than 0 or greater than 8)");
								}
							}
							if (previousNote.tick != previousTick) break;
						}
						if (j == 0) break;
					}
				}
			}
			return fixedNotes;
		}

		private List<NoteSection> AddForceHopoIfNecessary(long tick, List<NoteSection> forceHOPO, List<NoteSection> forceStrum) {
			List<NoteSection> added = forceHOPO;
			int i; bool check = true;
			foreach (var ns in forceStrum)
			{
				if (ns.tick > tick) break;
				if (ns.tick == tick) { check = false; break; }
			}
			if (check)
			{
				for (i = 0; i < added.Count; i++)
				{
					NoteSection ns = added[i];
					if (ns.tick > tick) break;
					if (ns.tick + ns.sus > tick) { check = false; break; }
				}
				if (check) added.Insert(i, new NoteSection(tick, 24));
			}
			return added;
		}

		internal void FixForces() {
			for (var i = 0; i < Tracks.Length; i++)
			{
				FixForces(Tracks[i]);
			}
		}

		internal void FixForces(List<Event> track) // N 5, N 6 has 0 length in .chart // ??????????
		{
			if (track.Count < 1)
				return; // ...
			foreach (var n in track)
				if (n is Note && ((n as Note).note == Note.H || (n as Note).note == Note.ohplsno))
					n.sus = Math.Max(1, n.sus);
		}

		internal void FixSp()
		{
			for (var i = 0; i < Tracks.Length; i++)
				Tracks[i] = FixSp(Tracks[i]);
		}

		internal List<Event> FixSp(List<Event> notes)
		{
			if (notes.Count < 1)
				return notes; // ...
			var fixedNotes = new List<Event>();
			foreach (var e in notes) {
				var n = e as Note;
				if (n != null) fixedNotes.Add(new Note(n.note, n.tick, n.sus));
				else {
					var ns = e as NoteSection;
					if (ns != null) fixedNotes.Add(new NoteSection(ns.tick, ns.sus+1));
				}
			}
			return fixedNotes;
		}

		internal void RemoveDuplicates() {
			for (var i = 0; i < Tracks.Length; i++)
				Tracks[i] = RemoveDuplicates(Tracks[i]);
			// if keys are meant to be a guitar track like in world tour, why was it excluded from these lower functions?
		}

		private List<Event> RemoveDuplicates(List<Event> notes) {
			if (notes.Count < 1)
				return notes; // ...
			var fixedNotes = notes;
			for (int i = fixedNotes.Count - 2; i >= 0; i--) {
				if (IsDuplicate(notes[i], notes))
					fixedNotes.RemoveAt(i);
			}
			return fixedNotes;
		}

		private bool IsDuplicate(Event e, List<Event> notes) {
			var n = e as Note;
			if (n != null) {
				for (var i = notes.IndexOf(n)+1; i < notes.Count; i++) {
					var n2 = notes[i] as Note;
					if (n2 != null)
						return n2.tick == n.tick && n2.note == n.note;
				}
			}
			else {
				var ns = e as NoteSection;
				if (ns != null) {
					for (var i = notes.IndexOf(ns)+1; i < notes.Count; i++) {
						var ns2 = notes[i] as NoteSection;
						if (ns2 != null)
							return ns2.tick == ns.tick && ns2.sus == ns.sus;
					}
				}
			}
			return false;
		}

		private bool IsPartOfBrokenChord(Note n, List<Event> notes) {
			if (n == null) return false;
			if (notes[0] == n) return false;
			var previousNote = GetPreviousNote(n, notes);
			return previousNote != null && previousNote.tick != n.tick && previousNote.tick + previousNote.sus > n.tick;
		}

		public static Note GetPreviousNote(Note n, List<Event> notes) {
			if (notes[0] == n) return null;
			for (var i = notes.IndexOf(n)-1; i >= 0; i--) {
				var previousNote = notes[i] as Note;
				if (previousNote != null && n.tick-previousNote.tick != 0)
					return notes[i] as Note;
			}
			return null;
		}

		public static Note GetNextNote(Note n, List<Event> notes) {
			if (notes[notes.Count-1] == n) return null;
			for (var i = notes.IndexOf(n)+1; i < notes.Count; i++) {
				var previousNote = notes[i] as Note;
				if (previousNote != null && n.tick-previousNote.tick != 0)
					return notes[i] as Note;
			}
			return null;
		}
	}
}