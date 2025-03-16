using System;
using NAudio.Midi;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace mid2chart {
	internal static class MidReader {
		private static Song s;
		private static MidiFile midi;
		private static double scaler;
		internal static Song ReadMidi(string path, bool unforceNAudioStrictMode) {
			s = new Song();
			midi = new MidiFile(path, !unforceNAudioStrictMode);
			var trackCount = midi.Events.Tracks; // ??????????????????????
			scaler = 192.0D / midi.DeltaTicksPerQuarterNote;
			WriteSync(midi.Events[0]);
			for (var i = 1; i < trackCount; i++) {
				var trackName = midi.Events[i][0] as TextEvent;
				if (trackName == null) continue;
				switch (trackName.Text.Trim().ToLower()) {
					case ("events"):
						//WriteSongSections(midi.Events[i]);
						WriteSongEvents(midi.Events[i]);
						break;
					case ("part guitar"):
						WriteNoteSection(midi.Events[i], 0);
						WriteTapSection(midi.Events[i], 0);
						for (var j = 0; j < 4; j++)
						{
							WriteOpenNotes(midi.Events[i], 0, j);
							s.Tracks[j + (0 << 2)].Sort(delegate (Event a, Event b) {
								return a.tick.CompareTo(b.tick);
							});
						}
						break;
					case ("t1 gems"):
						WriteNoteSection(midi.Events[i], 0);
						WriteTapSection(midi.Events[i], 0);
						for (var j = 0; j < 4; j++)
						{
							WriteOpenNotes(midi.Events[i], 0, j);
							s.Tracks[j + (0 << 2)].Sort(delegate (Event a, Event b) {
								return a.tick.CompareTo(b.tick);
							});
						}
						break;
					case ("part bass"):
						WriteNoteSection(midi.Events[i], 1);
						WriteTapSection(midi.Events[i], 1);
						for (var j = 0; j < 4; j++)
						{
							WriteOpenNotes(midi.Events[i], 1, j);
							s.Tracks[j + (1 << 2)].Sort(delegate (Event a, Event b) {
								return a.tick.CompareTo(b.tick);
							});
						}
						break;
					case ("part rhythm"):
						WriteNoteSection(midi.Events[i], 2);
						WriteTapSection(midi.Events[i], 2);
						for (var j = 0; j < 4; j++)
						{
							WriteOpenNotes(midi.Events[i], 2, j);
							s.Tracks[j + (2 << 2)].Sort(delegate (Event a, Event b) {
								return a.tick.CompareTo(b.tick);
							});
						}
						break;
					case ("part keys"):
						WriteNoteSection(midi.Events[i], 3);
						break;
				}
			}
			return s;
		}

		private static void WriteOpenNotes(IList<MidiEvent> track, int sec, int diff) {
			for (int i = 0; i < track.Count; i++) {
				var se = track[i] as SysexEvent;
				if (se != null) {
					var b = se.GetData();
					if (b.Length == 8 && b[5] == diff && b[7] == 1) {
						long tick = RoundToValidValue((long)Math.Floor(se.AbsoluteTime*scaler));
						long sus = 0;
						for (int j = i; j < track.Count; j++) {
							var se2 = track[j] as SysexEvent;
							if (se2 != null) {
								var b2 = se2.GetData();
								if (b2.Length == 8 && b2[5] == diff && b2[7] == 0) {
									sus = RoundToValidValue((long)Math.Floor(se2.AbsoluteTime*scaler))-tick;
									break;
								}
							}
						}
						if (sus == 0) sus = 1;
						s.Tracks[diff + (sec << 2)].Add(new Note(Note.S, tick, sus));
					}
				}
				// apparently note 59 is a thing, why is this "standard" all over the place for what dictates where things go and what format
			}
		}

		private static void WriteTapSection(IList<MidiEvent> track, int sec) {
			for (int i = 0; i < track.Count; i++) {
				var se = track[i] as SysexEvent;
				if (se != null) {
					var b = se.GetData();
					if (b.Length == 8 && b[5] == 255 && b[7] == 1) {
						long tick = RoundToValidValue((long)Math.Floor(se.AbsoluteTime*scaler));
						long sus = 0;
						for (int j = i; j < track.Count; j++) {
							var se2 = track[j] as SysexEvent;
							if (se2 != null) {
								var b2 = se2.GetData();
								if (b2.Length == 8 && b2[5] == 255 && b2[7] == 0) {
									sus = RoundToValidValue((long)Math.Floor(se2.AbsoluteTime*scaler))-tick;
									break;
								}
							}
						}
						if (sus == 0) sus = 1;
						//s.Tracks[sec << 2].Add(new Note(Note.T, tick, sus));
					}
				}
			}
		}

		private static void WriteNoteSection(IList<MidiEvent> track, int sec)
		{
			for (int i = 0; i < track.Count; i++)
			{
				var note = track[i] as NoteOnEvent;
				int newSolo = 0;
				if (note != null && note.OffEvent != null)
				{
					var tick = RoundToValidValue((long)Math.Floor(note.AbsoluteTime * scaler));
					var sus = RoundToValidValue((long)Math.Floor(note.OffEvent.AbsoluteTime * scaler)) - tick;
					int n = note.NoteNumber;
					if (n >= 60 && n < 103)
					{
						byte fret = (byte)(n - 60);
						if (fret % 12 > 6)
							Console.Error.WriteLine("Got MIDI note out of range: " + n + " @ " +
								((Diffs)(sec & 3)).ToString() + ' ' + ((Insts)(sec >> 2)).ToString());
						byte diff = (byte)(Math.Floor((double)(fret / 12))); // fractional bitshift when (floor(x/y) is it)
						fret %= 12;
						switch (fret)
						{
							case 5:
							case 6:
								if (sus == 0)
									sus = 1;
								fret = (byte)(Note.H + (fret - 5));
								break;
							default:
								if (sus < 64)
									sus = 0;
								break;
						}
						s.Tracks[diff + (sec << 2)].Add(new Note(fret, tick, sus));
					}
					else
					{
						switch (n)
						{
							// https://github.com/TheNathannator/GuitarGame_ChartFormats/blob/main/doc/FileFormats/.mid/Standard/5-Fret%20Guitar.md
							// also, look into for ChartEdit class: [song] hopo_frequency and sustain_cutoff_threshold
							// but the game already trims the note to just a normal note if too short
							// i just realized too I GOT CALLED OUT! ("More than one freestyle phrase may be placed during a BRE")
							case 103:
							case 104:
							case 105:
							case 106:
							case 116:
								if (n == 103 && !Program.gh1)
								{
									// treat this note as a solo marker when <RB1
									newSolo = 1;
									for (var j = 0; j < (int)Diffs.Count; j++)
										s.Tracks[j + (sec << 2)].Add(new TrackEvent(tick, "solo"));
								}
								else if (n == 104)
								{
									for (var j = 0; j < (int)Diffs.Count; j++)
										s.Tracks[j + (sec << 2)].Add(new Note(Note.T, tick, sus));
								}
								else
								{
									int fret = -1;
									switch (n)
									{
										case 103:
											if (Program.gh1)
												fret = Special.SP;
											else
												throw new Exception("wtf " + n);
											break;
										case 105:
											fret = Special.P1;
											break;
										case 106:
											fret = Special.P2;
											break;
										case 116:
											fret = Special.SP;
											break;
										default:
											throw new Exception("wtf " + n);
									}
									for (var j = 0; j < (int)Diffs.Count; j++)
										s.Tracks[j + (sec << 2)].Add(new Special(fret, tick, sus));
								}
								break;
						}
					}
				}
				var off = track[i] as NoteEvent;
				if (off != null)
				{
					if (newSolo == 0)
					{
						var tick = RoundToValidValue((long)Math.Floor(off.AbsoluteTime * scaler));
						var sus = RoundToValidValue((long)Math.Floor(off.AbsoluteTime * scaler)) - tick;
						int n = off.NoteNumber;
						switch (n)
						{
							case 103:
								if (!Program.gh1)
								{
									for (var j = 0; j < (int)Diffs.Count; j++)
										s.Tracks[j + (sec << 2)].Add(new TrackEvent(tick + sus, "soloend"));
								}
								break;
						}
					}
					else
						newSolo = 0;
				}
				var text = track[i] as TextEvent;
				if (text != null)
				{
					if (text.MetaEventType != MetaEventType.SequenceTrackName)
					{
						bool brackets = text.Text.StartsWith("[") && text.Text.EndsWith("]");
						TrackEvent newEvent = new TrackEvent((long)Math.Floor(text.AbsoluteTime * scaler), brackets ? text.Text.Substring(1, text.Text.Length - 2) : text.Text);
						for (var j = 0; j < (int)Diffs.Count; j++)
							s.Tracks[j + (sec << 2)].Add(newEvent);
					}
				}
			}
		}

		private static void WriteSongSections(IList<MidiEvent> track)
		{
			for (int i = 0; i < track.Count; i++)
			{
				var text = track[i] as TextEvent;
				if (text != null && text.Text.Trim().StartsWith("[section "))
					s.sections.Add(new Section((long)Math.Floor(text.AbsoluteTime * scaler), text.Text.Substring(9, text.Text.Length - 10)));
				else if (text != null && text.Text.Trim().StartsWith("[prc_"))
					s.sections.Add(new Section((long)Math.Floor(text.AbsoluteTime * scaler), text.Text.Substring(5, text.Text.Length - 6)));
			}
		}

		// GPT calls most of the C3 section keys redundant KV pairing
		private static Dictionary<string, string> uniquePRC = new Dictionary<string, string>() {
			{ "ah", "Ah!" },
			{ "yeah", "Yeah!" },
			{ "bre", "Big rock ending!" },
			{ "dj_break", "DJ break" },
			{ "spacey", "Spacey part" },
		};
		private static Dictionary<string, string> uniquePRC_ex = new Dictionary<string, string>() {
			{ "oohs", "Oohs and Ahs" },
			{ "lo_melody", "Low melody" },
			{ "hi_melody", "High melody" },
			{ "perc_solo", "Percussion solo" },
			{ "dj_intro", "DJ intro" },
			{ "dj_solo", "DJ solo" },
		};

		private static void WriteSongEvents(IList<MidiEvent> track)
		{
			// [0] == track name :/
			for (int i = 1; i < track.Count; i++)
			{
				var text = track[i] as TextEvent;
				if (text != null)
				{
					text.Text = text.Text.Trim();
					bool brackets = text.Text.StartsWith("[") && text.Text.EndsWith("]");
					long tick = (long)Math.Floor(text.AbsoluteTime * scaler);
					if (text.Text.StartsWith("[prc_"))
					{
						string name = text.Text.Substring(5, text.Text.Length - 6);
						if (uniquePRC.ContainsKey(name))
							name = uniquePRC[name];
						else
						{
							foreach (KeyValuePair<string, string> kv in uniquePRC_ex)
								name = Regex.Replace(name, "^"+Regex.Escape(kv.Key)+"(_|$)", kv.Value+' ').TrimEnd();
							name = Regex.Replace(name, "^([a-k])([1-9])?$", "$1 section $2").TrimEnd();
							name = Regex.Replace(name, "^(pre|post)(chorus|verse)", "$1-$2").Replace("_", " ");
							// preverse becomes Pre-verse in Rock Band
						}
						name = new string(name[0], 1).ToUpper() + name.Substring(1);
						s.sections.Add(new Section(tick, name)); // are you dumb
					}
					else
					{
						TrackEvent newEvent = new TrackEvent(tick, brackets ? text.Text.Substring(1, text.Text.Length - 2) : text.Text);
						s.eventsGlobal.Add(newEvent);
					}
				}
			}
		}

		private static void WriteSync(IList<MidiEvent> track) {
			foreach(var me in track) {
				var ts = me as TimeSignatureEvent;
				if (ts != null) {
					var tick = RoundToValidValue((long)Math.Floor(ts.AbsoluteTime*scaler));
					s.sync.Add(new Sync(tick, ts.Numerator, false));
					continue;
				}
				var tempo = me as TempoEvent;
				if (tempo != null) {
					var tick = RoundToValidValue((long)Math.Floor(tempo.AbsoluteTime*scaler));
					s.sync.Add(new Sync(tick, (int)Math.Floor(tempo.Tempo*1000), true));
					continue;
				}
				var text = me as TextEvent;
				if (text != null) {
					s.songname = text.Text;
				}
			}
		}

		private static long RoundToValidValue(long tick) {
			long a = tick+(16-(tick%16));
			long b = tick-(tick%16);
			long c = tick+(12-(tick%12));
			long d = tick-(tick%12);
			long ab; long cd;
			if (a-tick < -(b-tick))
				ab = a;
			else
				ab = b;
			if (c-tick < -(d-tick))
				cd = c;
			else
				cd = d;
			long abd; long cdd;
			if (tick-ab < 0)
				abd = -(tick-ab);
			else
				abd = tick-ab;
			if (tick-cd < 0)
				cdd = -(tick-cd);
			else
				cdd = tick-cd;
			long tickd;
			if (abd < cdd)
				tickd = tick-(tick-ab);
			else
				tickd = tick-(tick-cd);
			return tickd;
		}
	}
}