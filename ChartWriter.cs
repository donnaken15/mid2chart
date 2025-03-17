using System;
using System.Collections.Generic;
using System.IO;

namespace mid2chart {
	public enum TrackNames
	{
		Single,
		DoubleBass,
		DoubleLead,
		DoubleRhythm,
		EnhancedGuitar
	}
	internal static class ChartWriter {
		private static bool dummy;
		private static int hopoThresh = 64;
		internal static void WriteChart(Song s, string path, bool dummy) {
			if (Program.eighthHopo) hopoThresh = 96;
			if (Program.sixteenthStrum) hopoThresh = 32;
			ChartWriter.dummy = dummy;
			using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				using (var file = new StreamWriter(output)) {
					// wtf
					file.WriteLine("[Song]");
					file.WriteLine("{");
					file.WriteLine("\tName = \"" + s.songname + "\"");
					if (s.artist != "") file.WriteLine("\tArtist = \"" + s.artist + "\"");
					if (s.year != "") file.WriteLine("\tYear = \", " + s.year + "\"");
					if (s.charter != "") file.WriteLine("\tCharter = \"" + s.charter + "\"");
					file.WriteLine("\tOffset = " + GetOffsetString(s.offset));
					file.WriteLine("\tResolution = 192");
					file.WriteLine("\tPlayer2 = bass");
					file.WriteLine("\tDifficulty = 0");
					file.WriteLine("\tPreviewStart = 0.00");
					file.WriteLine("\tPreviewEnd = 0.00");
					file.WriteLine("\tGenre = \"rock\"");
					file.WriteLine("\tMediaType = \"cd\"");
					file.WriteLine("\tMusicStream = \"song.ogg\"");
					file.WriteLine("\tGuitarStream = \"guitar.ogg\"");
					file.WriteLine("\tBassStream = \"rhythm.ogg\"");
					file.WriteLine("}");
					file.WriteLine("[SyncTrack]");
					file.WriteLine("{");
					foreach (Sync sync in s.sync) {
							file.WriteLine("\t" + sync.tick + " = " + ((!sync.isBPM) ? "TS" : "B") + ' ' + sync.num);
					}
					file.WriteLine("}");
					file.WriteLine("[Events]");
					file.WriteLine("{");
					foreach (Section ss in s.sections)
					{
						file.WriteLine("\t" + ss.tick + " = E \"section " + ss.name + "\"");
					}
					foreach (TrackEvent ss in s.eventsGlobal)
					{
						file.WriteLine("\t" + ss.tick + " = E \"" + ss.text + "\"");
					}
					file.WriteLine("}");
					for (var i = 0; i < s.Tracks.Length; i++)
					{
						//Console.WriteLine(((Diffs)(i & 3)).ToString() + ' ' + ((Insts)(i >> 2)) + " (" + s.Tracks[i].Count + ')');
						if (s.Tracks[i].Count > 0)
						{
							int ii = i >> 2;
							if (Program.keysOnGuitar)
							{
								// there has to be a way to simplify this
								switch ((TrackNames)i)
								{
									case TrackNames.EnhancedGuitar:
										i = (int)(TrackNames.Single);
										break;
									case TrackNames.Single:
										i = (int)(TrackNames.EnhancedGuitar);
										break;
								}
							}
							else if (Program.bassOnGuitar)
							{
								switch ((TrackNames)i)
								{
									case TrackNames.Single:
										i = (int)(TrackNames.DoubleBass);
										break;
									case TrackNames.DoubleBass:
										i = (int)(TrackNames.Single);
										break;
								}
							}
							file.WriteLine('[' + ((Diffs)(i & 3)).ToString() + ((TrackNames)(ii)).ToString() + ']');
							file.WriteLine("{");
							WriteNotes(file, s.Tracks[(i & 3) + (ii << 2)], false);
							file.WriteLine("}");
						}
					}
				}
			}
		}

		private static string GetOffsetString(long offset) {
			var str = "";
			if (offset < 0) {
				str = "-";
				offset = offset * -1;
			}
			if (offset == 0) str += "0";
			else if (offset < 10) str += "0.00" + offset;
			else if (offset < 100) str += "0.0" + offset;
			else if (offset < 1000) str += "0." + offset;
			else str += offset.ToString().Substring(0, 1) + "." + offset.ToString().Substring(1);
			return str;
		}

		private static void WriteNotes(StreamWriter file, List<Event> notes, bool isKeys) {
			TrackEvent _event = null;
			Note n = null;
			foreach (Event e in notes)
			{
				n = e as Note;
				_event = e as TrackEvent;
				if (_event != null && !_event.text.StartsWith("PART ") && _event.text != "")
				{
					file.WriteLine("\t" + _event.tick + " = E " + _event.text);
				}
				if (n != null)
				{
					if (Program.readOpenNotes && IsTap(n, notes)) {
						if (dummy || Program.editable)
							file.WriteLine("\t" + n.tick + " = E O");
						else
							file.WriteLine("\t" + n.tick + " = N 7 " + n.sus);
					} else {
						if (n.note != Note.H && n.note != Note.T && n.note != Note.ohplsno)
							file.WriteLine("\t" + n.tick + " = N " + n.note + " " + n.sus);
					}
					if (GetNextNoteDiff(n, notes) > 0) {
						if (IsTap(n, notes))
							file.WriteLine("\t" + n.tick + ((dummy || Program.editable) ? " = E T" : " = N 6 0"));
						else if (IsForced(n, notes))
							file.WriteLine("\t" + n.tick + ((Program.editable) ? " = E F" : " = N 5 0"));
					}
					continue;
				}
				var sp = e as Special;
				if (sp != null)
				{
					file.WriteLine("\t" + sp.tick + " = S " + sp.flag + " " + sp.sus);
				}
			}
		}

		private static bool IsForced(Note n, List<Event> notes) {
			bool check = false;
			long tickDiff = GetPreviousNoteDiff(n, notes);
			if (tickDiff <= hopoThresh && !IsChord(n, notes) && !SameNoteAsPrevious(n, notes))
			{
				if ((Program.rbLogic && ContainsNoteFromPreviousChord(n, notes)) || (Program.readOpenNotes && Program.openNoteStrum && IsTap(n, notes))) {
					//applies harmonix's post-chord ho/po logic
					//this means that this note contains a note from the previous chord
					//therefore it's a strum, unless it's forced otherwise
					//so, look on forceHOPO
					//also works when forcing open notes as strum by default
					//unless forced otherwise ofc
					check = true;
					foreach (var hopo in notes)
					{
						if (!(hopo is Note))
							continue;
						if ((hopo as Note).note != Note.H)
							continue;
						if (hopo.tick > n.tick) break;
						if (n.tick >= hopo.tick && n.tick < (hopo.tick + hopo.sus)) {
							check = false;
							break;
						}
					}
				} else {
					//regular HOPO - look on forceStrum
					foreach (var s in notes)
					{
						if (!(s is Note))
							continue;
						if ((s as Note).note != Note.ohplsno)
							continue;
						if (s.tick > n.tick) break;
						if (n.tick >= s.tick && n.tick < (s.tick + s.sus))
						{
							check = true;
							break;
						}
					}
				}
				if (Program.eighthHopo && (tickDiff > 64 && tickDiff <= 96)) check = !check;
			} else { /*if (tickDiff > hopoThresh || IsChord(n, notes) || SameNoteAsPrevious(n, notes))*/
				//not HOPO - look on forceHOPO
				foreach (var hopo in notes)
				{
					if (!(hopo is Note))
						continue;
					if ((hopo as Note).note != Note.H)
						continue;
					if (hopo.tick > n.tick) break;
					if (n.tick >= hopo.tick && n.tick < (hopo.tick + hopo.sus))
					{
						check = true;
						if (Program.fixDoubleHopo && (SameNoteAsPrevious(n, notes) || SameChordAsPrevious(n, notes))) {
							check = false;
							break;
						}
						if (Program.dontForceChords && IsChord(n, notes))
							check = false;
						break;
					}
				}
				if (Program.sixteenthStrum && (tickDiff > 32 && tickDiff <= 64) && !IsChord(n, notes) && !SameNoteAsPrevious(n, notes))
					check = !check;
			}
			return check;
		}

		private static bool IsForcedKeys(Note n, List<Event> notes) {
			var tickDiff = GetPreviousNoteDiff(n, notes);
			if (tickDiff > hopoThresh) if (Program.sixteenthStrum) return tickDiff > 32 && tickDiff <= 64; else return false;
			if (!IsChord(n, notes) && Program.rbLogic && ContainsNoteFromPreviousChord(n, notes))
				if (Program.eighthHopo) return tickDiff <= 64; else return true;
			if (!IsChord(n, notes) || Program.dontForceChords)
				if (Program.eighthHopo) return tickDiff > 64 && tickDiff <= 96; else return false;
			return !Program.fixDoubleHopo || !SameChordAsPrevious(n, notes);
		}

		private static bool SameChordAsPrevious(Note n, List<Event> notes) {
			if (!IsChord(n, notes)) return false;
			var previousNote = new Note(0, -1, 0);
			for (var i = notes.IndexOf(n) - 1; i >= 0; i--) {
				previousNote = notes[i] as Note;
				if (previousNote != null && n.tick - previousNote.tick != 0) {
					break;
				}
			}
			if (!IsChord(previousNote, notes)) return false;
			//if the current note isnt a chord it returns false
			//else it looks for the previous note - if it also isnt a chord, it returns false again
			var currentChord = GetNotesFromChord(n, notes);
			var previousChord = GetNotesFromChord(previousNote, notes);
			if (currentChord.Count != previousChord.Count) return false;
			//it gets the respective chords and compares their length - if they dont match they cannot be the same chord
			var count = currentChord.Count;

			//OLD CODE, LEAVING THIS INCASE NEW ONE FAILS
			/*bool[] check1 = new bool[count], check2 = new bool[count];
			for (var i = 0; i < count; i++) {
				for (var j = 0; j < count; j++) {
					if (check2[j]) break;
					if (currentChord[i].note == previousChord[j].note) {
						check1[i] = true; check2[j] = true; break;
					}
				}
				if (!check1[i]) return false;
			}
			//tries to find the note from the current chord in the previous chord.
			//stores whether it's been found or not in 2 arrays (one for each chord).
			//if the notes match somewhere both their positions are marked as true on their respective arrays
			for (var i = 0; i < count; i++)
				if (!check1[i] || !check2[i]) return false;
			//checks if ALL of the notes were found - if they werent then they arent the same chord
			//if it didnt fail until here then it means both chords are the same*/

			for (var i = 0; i < count; i++) {
				if (currentChord[i].note != previousChord[i].note) return false;
			}
			return true;
		}

		public static List<Note> GetNotesFromChord(Note n, List<Event> notes) {
			var chord = new List<Note> { n };
			var index = notes.IndexOf(n);
			for (var i = index + 1; i < notes.Count; i++) {
				var n2 = notes[i] as Note;
				if (n2 != null && notes[i].tick - n.tick == 0) chord.Add(n2);
				if (n2 != null && notes[i].tick - n.tick != 0) break;
			}
			for (var i = index - 1; i >= 0; i--) {
				var n2 = notes[i] as Note;
				if (n2 != null && notes[i].tick - n.tick == 0) chord.Add(n2);
				if (n2 != null && notes[i].tick - n.tick != 0) break;
			}
			return chord;
		}

		private static bool ContainsNoteFromPreviousChord(Note n, List<Event> notes) {
			if (!Program.rbLogic || notes.IndexOf(n) <= 1 || IsChord(n, notes))
				return false;
			var previousChord = new List<Note>();
			Note previousNote = null;
			for (var i = notes.IndexOf(n) - 1; i >= 0; i--) {
				previousNote = notes[i] as Note;
				if (previousNote != null)
					break;
			}
			if (!IsChord(previousNote, notes)) return false;
			previousChord = GetNotesFromChord(previousNote, notes);
			/*while(true) {
				if (notes.IndexOf(previousNote) == 0)
					break;
				if (previousNote != null && previousNote.tick-notes[(notes.IndexOf(previousNote)-1)].tick != 0)
					break;
				previousNote = notes[(notes.IndexOf(previousNote)-1)] as Note;
				if (previousNote != null)
					previousChord.Add(previousNote);
			}*/
			foreach (var n2 in previousChord) {
				if (n2.note == n.note) return true;
			}
			return false;
		}

		private static bool SameNoteAsPrevious(Note n, List<Event> notes) {
			if (notes.IndexOf(n) == 0) return false;
			for (int i = notes.IndexOf(n) - 1; i >= 0; i--) {
				var n2 = notes[i] as Note;
				if (n2 != null) {
					if (Program.readOpenNotes && IsTap(n, notes)) return IsTap(n2, notes);
					if (Program.readOpenNotes && IsTap(n2, notes)) return IsTap(n, notes);
					else return !IsChord(n2, notes) && n2.note == n.note;
				}
			}
			return false;
		}

		private static bool IsChord(Note n, List<Event> notes) {
			var index = notes.IndexOf(n);
			if (index == 0) {
				for (var i = 1; i < notes.Count; i++) {
					var n2 = notes[i] as Note;
					if (n2 != null) return notes[i].tick - n.tick == 0;
				}
			}
			if (index == notes.Count - 1) {
				for (var i = notes.Count - 2; i >= 0; i--) {
					var n2 = notes[i] as Note;
					if (n2 != null) return notes[i].tick - n.tick == 0;
				}
			}
			bool check = false;
			for (var i = index + 1; i < notes.Count; i++) {
				var n2 = notes[i] as Note;
				if (n2 != null && notes[i].tick - n.tick == 0) check = true;
				if (n2 != null && notes[i].tick - n.tick != 0) break;
			}
			if (check) return check;
			for (var i = index - 1; i >= 0; i--) {
				var n2 = notes[i] as Note;
				if (n2 != null && notes[i].tick - n.tick == 0) check = true;
				if (n2 != null && notes[i].tick - n.tick != 0) break;
			}
			return check;
		}

		private static long GetPreviousNoteDiff(Note n, List<Event> notes) {
			if (notes[0] == n) return 192;
			else {
				for (int i = notes.IndexOf(n) - 1; i >= 0; i--) {
					var previousNote = notes[i] as Note;
					if (previousNote != null && n.tick - previousNote.tick != 0) {
						return n.tick - previousNote.tick;
					}

				}
			}
			return 192;
		}

		private static bool IsTap(Note n, List<Event> notes) {
			foreach (Event t in notes) {
				if (!(t is Note))
					continue;
				if (t.tick > n.tick) break;
				if (n.tick >= t.tick && n.tick < (t.tick + t.sus) && n.note == Note.T) {
					return true;
				}
			}
			return false;
		}

		private static long GetNextNoteDiff(Note n, List<Event> notes) {
			if (notes.IndexOf(n) == notes.Count - 1) return 1;
			for (int i = notes.IndexOf(n) + 1; i < notes.Count; i++) {
				var nextNote = notes[i] as Note;
				if (nextNote != null)
					return notes[i].tick - n.tick;
			}
			return 1;
		}
	}
}