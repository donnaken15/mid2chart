using System;
using System.IO;
using System.Collections.Generic;

namespace mid2chart {
	static class Program {
		internal static bool editable, rbLogic, broken, fixForces, fixSp, fixDoubleHopo, dontForceChords,
			fixOverlaps, eighthHopo, sixteenthStrum, keysOnBass, keysOnGuitar, bassOnGuitar,
			gh1, skipPause, readOpenNotes, openNoteStrum, unforceNAudioStrictMode, tapToHopo; // dontWriteDummy
		public static void Main(string[] args)
		{
			string dir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
			try
			{
				if (args.Length == 0)
				{
					Dictionary<string, string> usagePage = new Dictionary<string, string>()
					{
						{"e",  "export an editable FeedBack file (as in no \"N 5 0\"s and \"N 6 0\"s, but \"E *\"s and \"E T\"s instead)."},
						{"r",  "use Rock Band/Harmonix's HO/PO logic (if a note after a chord is part of the previous chord, then it is a strum."},
						{"b",  "bypass broken chord adaptation, and leave them as they are (if there's any)."},
						{"f",  "use if some notes aren't being properly forced. Be aware, though, that this might force some notes unproperly."},
						{"s",  "use if some notes aren't in their proper star power sections. Be aware, though, that this might put some notes unproperly in star power sections."},
						{"u",  "unforce NAudio's strict midi checking."},
						{"d",  "remove double HO/POs."},
						{"c",  "avoid forcing chords (useful for non-GH3+ users or console players)."},
						{"t",  "convert tap notes as forced HO/POs (useful for non-GH3+ users or console players)."},
						{"o",  "clip overlapping sustains."},
						{"8",  "set HO/PO threshold to 1/8. If the song.ini already specifies a 1/8 HO/PO threshold, use this parameter to set it back to 1/12."},
						{"16", "set 1/16 notes as strums. If the song.ini already specifies that, use this parameter to set it back to default."},
						{"1",  "read midi note 103 as star power instead of 116."},
						{"k",  "skip the \"Press any key to exit\" message, and just exit."},
						//{"m",  "do NOT write a (Dummy).chart file"}, // won't matter these days
						{"p",  "read and write open notes."},
						{"os", "convert open notes as strum by default, unless forced otherwise."},
						{"kb", "swap keys and bass when converting the midi."},
						{"kg", "swap keys and guitar when converting the midi."},
						{"gb", "swap guitar and bass when converting the midi."},
					};
					Console.WriteLine("Usage: drag and drop one or more .mid files into this executable file.");
					foreach (KeyValuePair<string, string> cmd in usagePage)
					{
						Console.WriteLine("    -"+cmd.Key.PadRight(3)+" - "+cmd.Value);
					}
					Console.WriteLine("Be aware that you can only use ONE of the track swapping parameters.");
				}
				else
				{
					Stopwatch.Start("Program started!");
					for (int i = 0; i < args.Length; i++)
					{
						switch (args[i])
						{
							case "-e": editable = true; break;
							case "-r": rbLogic = true; break;
							case "-b": broken = true; break;
							case "-f": fixForces = true; break;
							case "-s": fixSp = true; break;
							case "-u": unforceNAudioStrictMode = true; break;
							case "-d": fixDoubleHopo = true; break;
							case "-c": dontForceChords = true; break;
							case "-t": tapToHopo = true; break;
							case "-o": fixOverlaps = true; break;
							case "-8": eighthHopo = true; break;
							case "-16": sixteenthStrum = true; break;
							case "-1": gh1 = true; break;
							case "-k": skipPause = true; break;
							//case "-m": dontWriteDummy = true; break;
							case "-p": readOpenNotes = true; break;
							case "-os": openNoteStrum = true; break;
							case "-kb":
								if (!keysOnGuitar && !bassOnGuitar) keysOnBass = true;
								else
								{
									if (keysOnGuitar) Console.Write("Swap keys and guitar");
									else if (bassOnGuitar) Console.Write("Swap guitar and bass");
									Console.WriteLine(" is already set. Swap keys and bass will be ignored.");
								}
								break;
							case "-kg":
								if (!keysOnBass && !bassOnGuitar) keysOnGuitar = true;
								else
								{
									if (keysOnBass) Console.Write("Swap keys and bass");
									else if (bassOnGuitar) Console.Write("Swap guitar and bass");
									Console.WriteLine(" is already set. Swap keys and guitar will be ignored.");
								}
								break;
							case "-gb":
								if (!keysOnGuitar && !keysOnBass) bassOnGuitar = true;
								else
								{
									if (keysOnGuitar) Console.Write("Swap keys and guitar");
									else if (keysOnBass) Console.Write("Swap keys and bass");
									Console.WriteLine(" is already set. Swap guitar and bass will be ignored.");
								}
								break;
						}
					}
					for (int i = 0; i < args.Length; i++)
					{
						if (args[i] != "-e" && args[i] != "-r" && (args[i] != "-b")
							&& args[i] != "-f" && (args[i] != "-s") && (args[i] != "-d")
							&& (args[i] != "-c") && (args[i] != "-o") && (args[i] != "-8")
							&& (args[i] != "-16") && (args[i] != "-kb") && (args[i] != "-kg")
							&& (args[i] != "-gb") && (args[i] != "-1") && (args[i] != "-k")
							&& (args[i] != "-p") && (args[i] != "-m") && (args[i] != "-os")
							&& (args[i] != "-u") && (args[i] != "-t"))
						{
							try
							{
								Stopwatch.Step("Reading midi: " + args[i]);
								Song s = MidReader.ReadMidi(args[i], unforceNAudioStrictMode);
								Stopwatch.EndStep();
								Stopwatch.Step("Reading metadata from song.ini (if it exists)");
								ReadMetadata(s, args[i]);
								Stopwatch.EndStep();
								Stopwatch.Step("Removing duplicate events (if any)");
								s.RemoveDuplicates();
								Stopwatch.EndStep();
								if (!broken)
								{
									Stopwatch.Step("Fixing broken chords");
									s.FixBrokenChords();
									Stopwatch.EndStep();
								}
								if (tapToHopo)
								{
									Stopwatch.Step("Converting tap sections to force HO/PO sections");
									s.TapToHopo();
									Stopwatch.EndStep();
								}
								if (fixForces)
								{
									Stopwatch.Step("Fixing forced sections");
									s.FixForces();
									Stopwatch.EndStep();
								}
								if (fixSp)
								{
									Stopwatch.Step("Fixing star power sections");
									s.FixSp();
									Stopwatch.EndStep();
								}
								if (fixOverlaps)
								{
									Stopwatch.Step("Fixing overlapping sustains");
									s.FixOverlaps();
									Stopwatch.EndStep();
								}
								if (editable)
								{
									string filename = args[i].Substring(0, args[i].Length - 4) + " (editable).chart";
									Stopwatch.Step("Writing chart: " + filename);
									ChartWriter.WriteChart(s, filename, false);
									Stopwatch.EndStep();
								}
								else
								{
									string filename;
									/*if (!dontWriteDummy && (s.tapGuitar.Count > 0 || s.tapBass.Count > 0))
									{
										filename = args[i].Substring(0, args[i].Length - 4) + " (Dummy).chart";
										Stopwatch.Step("Writing chart: " + filename);
										ChartWriter.WriteChart(s, filename, true);
										Stopwatch.EndStep();
									}*/
									filename = args[i].Substring(0, args[i].Length - 3) + "chart";
									Stopwatch.Step("Writing chart: " + filename);
									ChartWriter.WriteChart(s, filename, false);
									Stopwatch.EndStep();
									Stopwatch.Stop();
								}
							}
							catch (Exception e)
							{
								Console.WriteLine(e);
								File.WriteAllText(dir + "\\mid2chart.log", e.ToString());
							}
						}
					}
				}
				if (!skipPause)
				{
					Console.Write("Press any key to exit...");
					Console.ReadKey();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				File.WriteAllText(dir + "\\mid2chart.log", e.ToString());
			}
		}

		private static void ReadMetadata(Song s, string path) {
			var pathArray = path.Split('\\');
			var newPath = "";
			for (int i = 0; i < pathArray.Length - 1; i++) {
				newPath += pathArray[i] + "\\";
			}
			newPath += "song.ini";
			if (File.Exists(newPath)) {
				try {
					string line;
					StreamReader file = new StreamReader(newPath);
					bool eighthHopo = false;
					bool sixteenthStrum = false;
					while ((line = file.ReadLine()) != null) {
						var strArray = line.Split('=');
						if (strArray.Length > 0) {
							var attr = strArray[0].Trim();
							switch (attr) {
								case "name": s.songname = strArray[1].Trim(); break;
								case "artist": s.artist = strArray[1].Trim(); break;
								case "year": s.year = strArray[1].Trim(); break;
								case "charter": s.charter = strArray[1].Trim(); break;
								case "delay": s.offset = long.Parse(strArray[1].Trim()); break;
								case "eighthnote_hopo": if (strArray[1].Trim() == "1") eighthHopo = true; break;
								case "hopo_frequency":
									if (strArray[1].Trim() == "250") eighthHopo = true;
									else if (strArray[1].Trim() == "80") sixteenthStrum = true;
									break;
							}
						}
					}
					if (eighthHopo) Program.eighthHopo = !Program.eighthHopo;
					if (sixteenthStrum) Program.sixteenthStrum = !Program.sixteenthStrum;
					file.Close();
				} catch (Exception e) {
					Console.WriteLine("The song.ini file could not be read:");
					Console.WriteLine(e.Message);
					File.WriteAllText(Directory.GetCurrentDirectory() + "\\mid2chart.log", e.ToString());
				}
			}
		}
	}
}
