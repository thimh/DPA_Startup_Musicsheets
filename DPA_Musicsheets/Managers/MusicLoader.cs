
using DPA_Musicsheets.Models;
using DPA_Musicsheets.ViewModels;
using PSAMControlLibrary;
using PSAMWPFControlLibrary;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Managers
{
    /// <summary>
    /// This is the one and only god class in the application.
    /// It knows all about all file types, knows every viewmodel and contains all logic.
    /// TODO: Clean this class up.
    /// </summary>
    public class MusicLoader
    {
        #region Properties
        public string LilypondText { get; set; }
        public List<MusicalSymbol> WPFStaffs { get; set; } = new List<MusicalSymbol>();
        private static List<Char> notesorder = new List<Char> { 'c', 'd', 'e', 'f', 'g', 'a', 'b' };

        private FileReaderFactory fileReaderFactory = new FileReaderFactory();

        public Sequence MidiSequence { get; set; }
        #endregion Properties

        private int _beatNote = 4;    // De waarde van een beatnote.
        private int _bpm = 120;       // Aantal beatnotes per minute.
        private int _beatsPerBar;     // Aantal beatnotes per maat.

        public MainViewModel MainViewModel { get; set; }
        public LilypondViewModel LilypondViewModel { get; set; }
        public MidiPlayerViewModel MidiPlayerViewModel { get; set; }
        public StaffsViewModel StaffsViewModel { get; set; }

        /// <summary>
        /// Opens a file.
        /// TODO: Remove the switch cases and delegate.
        /// TODO: Remove the knowledge of filetypes. What if we want to support MusicXML later?
        /// TODO: Remove the calling of the outer viewmodel layer. We want to be able reuse this in an ASP.NET Core application for example.
        /// </summary>
        /// <param name="fileName"></param>
        public void OpenFile(string fileName)
        {
            // TODO: CONTINUE WORKING ONT THIS!!!
            fileReaderFactory.AddReaderType("LilypondReader", typeof(LilypondReader));
            fileReaderFactory.AddReaderType("MidiReader", typeof(MidiReader));

            fileReaderFactory.CreateReader("LilypondReader").ReadFile(fileName);

            CheckFileExtension(fileName);

            LoadLilypondIntoWpfStaffsAndMidi(LilypondText);
        }

        //TODO: Factory Method!!!
        private void CheckFileExtension(string fileName)
        {
            if (Path.GetExtension(fileName).EndsWith(".mid"))
            {
                ReadMidiFile(fileName);
            }
            else if (Path.GetExtension(fileName).EndsWith(".ly"))
            {
                ReadLilypondFile(fileName);
            }
            else
            {
                throw new NotSupportedException($"File extension {Path.GetExtension(fileName)} is not supported.");
            }
        }

        private void ReadLilypondFile(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in File.ReadAllLines(fileName))
            {
                sb.AppendLine(line);
            }

            this.LilypondText = sb.ToString();
            this.LilypondViewModel.LilypondTextLoaded(this.LilypondText);
        }

        private void ReadMidiFile(string fileName)
        {
            MidiSequence = new Sequence();
            MidiSequence.Load(fileName);

            MidiPlayerViewModel.MidiSequence = MidiSequence;
            this.LilypondText = LoadMidiIntoLilypond(MidiSequence);
            this.LilypondViewModel.LilypondTextLoaded(this.LilypondText);
        }

        /// <summary>
        /// This creates WPF staffs and MIDI from Lilypond.
        /// TODO: Remove the dependencies from one language to another. If we want to replace the WPF library with another for example, we have to rewrite all logic.
        /// TODO: Create our own domain classes to be independent of external libraries/languages.
        /// </summary>
        /// <param name="content"></param>
        //TODO: Use Converters!!!
        public void LoadLilypondIntoWpfStaffsAndMidi(string content)
        {
            LilypondText = content;
            content = content.Trim().ToLower().Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ");
            LinkedList<LilypondToken> tokens = GetTokensFromLilypond(content);
            WPFStaffs.Clear();

            WPFStaffs.AddRange(GetStaffsFromTokens(tokens));
            this.StaffsViewModel.SetStaffs(this.WPFStaffs);

            MidiSequence = GetSequenceFromWPFStaffs();
            MidiPlayerViewModel.MidiSequence = MidiSequence;
        }

        #region Midi loading (loads midi to lilypond)

        /// <summary>
        /// TODO: Create our own domain classes to be independent of external libraries/languages.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        //TODO: Implement own Domain Classes!!!
        public string LoadMidiIntoLilypond(Sequence sequence)
        {
            StringBuilder lilypondContent = new StringBuilder();
            lilypondContent.AppendLine("\\relative c' {");
            lilypondContent.AppendLine("\\clef treble");

            int division = sequence.Division;
            int previousMidiKey = 60; // Central C;
            int previousNoteAbsoluteTicks = 0;
            double percentageOfBarReached = 0;
            bool startedNoteIsClosed = true;

            BuildTrackFromMidi(sequence,
                lilypondContent,
                division,
                ref previousMidiKey,
                ref previousNoteAbsoluteTicks,
                ref percentageOfBarReached,
                ref startedNoteIsClosed);

            lilypondContent.Append("}");

            return lilypondContent.ToString();
        }

        private void BuildTrackFromMidi(Sequence sequence,
            StringBuilder lilypondContent,
            int division,
            ref int previousMidiKey,
            ref int previousNoteAbsoluteTicks,
            ref double percentageOfBarReached,
            ref bool startedNoteIsClosed)
        {
            for (int i = 0; i < sequence.Count(); i++)
            {
                Track track = sequence[i];

                foreach (var midiEvent in track.Iterator())
                {
                    IMidiMessage midiMessage = midiEvent.MidiMessage;
                    // TODO: Split this switch statements and create separate logic.
                    // We want to split this so that we can expand our functionality later with new keywords for example.
                    // Hint: Command pattern? Strategies? Factory method?
                    //TODO: Command Pattern!!!
                    //TODO: In midiconverter
                    switch (midiMessage.MessageType)
                    {
                        case MessageType.Meta:
                            var metaMessage = midiMessage as MetaMessage;
                            switch (metaMessage.MetaType)
                            {
                                case MetaType.TimeSignature:
                                    SetTimeSignature(metaMessage, lilypondContent);
                                    break;
                                case MetaType.Tempo:
                                    SetTempo(metaMessage, lilypondContent);
                                    break;
                                case MetaType.EndOfTrack:
                                    percentageOfBarReached = SetEndOfTrack(division, previousNoteAbsoluteTicks, percentageOfBarReached, midiEvent, lilypondContent);
                                    break;
                                default: break;
                            }
                            break;
                        case MessageType.Channel:
                            SetChannel(lilypondContent, division, ref previousMidiKey, ref previousNoteAbsoluteTicks, ref percentageOfBarReached, ref startedNoteIsClosed, midiEvent);
                            break;
                    }
                }
            }
        }

        private void SetChannel(StringBuilder lilypondContent,
            int division,
            ref int previousMidiKey,
            ref int previousNoteAbsoluteTicks,
            ref double percentageOfBarReached,
            ref bool startedNoteIsClosed,
            MidiEvent midiEvent)
        {
            var channelMessage = midiEvent.MidiMessage as ChannelMessage;
            if (channelMessage.Command == ChannelCommand.NoteOn)
            {
                if (channelMessage.Data2 > 0) // Data2 = loudness
                {
                    // Append the new note.
                    startedNoteIsClosed = false;
                    AppendNoteTone(lilypondContent, ref previousMidiKey, channelMessage);
                }
                else if (!startedNoteIsClosed)
                {
                    double percentageOfBar;
                    // Finish the previous note with the length.
                    startedNoteIsClosed = true;
                    AppendNoteLength(lilypondContent, division, ref previousNoteAbsoluteTicks, ref percentageOfBarReached, midiEvent, out percentageOfBar);
                    percentageOfBarReached = AppendBarLine(lilypondContent, percentageOfBarReached, percentageOfBar);
                }
                else
                {
                    lilypondContent.Append("r");
                }
            }
        }

        private static double AppendBarLine(StringBuilder lilypondContent, double percentageOfBarReached, double percentageOfBar)
        {
            percentageOfBarReached += percentageOfBar;
            if (percentageOfBarReached >= 1)
            {
                lilypondContent.AppendLine("|");
                percentageOfBarReached -= 1;
            }

            return percentageOfBarReached;
        }

        private void AppendNoteLength(StringBuilder lilypondContent, int division, ref int previousNoteAbsoluteTicks, ref double percentageOfBarReached, MidiEvent midiEvent, out double percentageOfBar)
        {
            lilypondContent.Append(MidiToLilyHelper.GetLilypondNoteLength(previousNoteAbsoluteTicks, midiEvent.AbsoluteTicks, division, _beatNote, _beatsPerBar, out percentageOfBar));
            previousNoteAbsoluteTicks = midiEvent.AbsoluteTicks;
            lilypondContent.Append(" ");
        }

        private static void AppendNoteTone(StringBuilder lilypondContent, ref int previousMidiKey, ChannelMessage channelMessage)
        {
            lilypondContent.Append(MidiToLilyHelper.GetLilyNoteName(previousMidiKey, channelMessage.Data1));

            previousMidiKey = channelMessage.Data1;
        }

        private double SetEndOfTrack(int division, int previousNoteAbsoluteTicks, double percentageOfBarReached, MidiEvent midiEvent, StringBuilder lilypondContent)
        {
            if (previousNoteAbsoluteTicks > 0)
            {
                // Finish the last notelength.
                double percentageOfBar;
                lilypondContent.Append(MidiToLilyHelper.GetLilypondNoteLength(previousNoteAbsoluteTicks, midiEvent.AbsoluteTicks, division, _beatNote, _beatsPerBar, out percentageOfBar));
                lilypondContent.Append(" ");

                percentageOfBarReached += percentageOfBar;
                if (percentageOfBarReached >= 1)
                {
                    lilypondContent.AppendLine("|");
                    percentageOfBar = percentageOfBar - 1;
                }
            }

            return percentageOfBarReached;
        }

        private void SetTempo(MetaMessage metaMessage, StringBuilder lilypondContent)
        {
            byte[] tempoBytes = metaMessage.GetBytes();
            int tempo = (tempoBytes[0] & 0xff) << 16 | (tempoBytes[1] & 0xff) << 8 | (tempoBytes[2] & 0xff);
            _bpm = 60000000 / tempo;
            lilypondContent.AppendLine($"\\tempo 4={_bpm}");
        }

        private void SetTimeSignature(MetaMessage metaMessage, StringBuilder lilypondContent)
        {
            byte[] timeSignatureBytes = metaMessage.GetBytes();
            _beatNote = timeSignatureBytes[0];
            _beatsPerBar = (int)(1 / Math.Pow(timeSignatureBytes[1], -2));
            lilypondContent.AppendLine($"\\time {_beatNote}/{_beatsPerBar}");
        }

        #endregion Midiloading (loads midi to lilypond)

        #region Staffs loading (loads lilypond to WPF staffs)
        private static IEnumerable<MusicalSymbol> GetStaffsFromTokens(LinkedList<LilypondToken> tokens)
        {
            List<MusicalSymbol> symbols = new List<MusicalSymbol>();

            Clef currentClef = null;
            int previousOctave = 4;
            char previousNote = 'c';
            bool inRepeat = false;
            bool inAlternative = false;
            int alternativeRepeatNumber = 0;

            LilypondToken currentToken = tokens.First();
            while (currentToken != null)
            {
                // TODO: There are a lot of switches based on LilypondTokenKind, can't those be eliminated and delegated?
                // HINT: Command, Decorator, Factory etc.
                //TODO: Decorator Pattern!!!
                // TODO: Repeats are somewhat weirdly done. Can we replace this with the COMPOSITE pattern?
                switch (currentToken.TokenKind)
                {
                    case LilypondTokenKind.Unknown:
                        break;
                    case LilypondTokenKind.Repeat:
                        inRepeat = true;
                        symbols.Add(new Barline() { RepeatSign = RepeatSignType.Forward });
                        break;
                    case LilypondTokenKind.SectionEnd:
                        if (inRepeat && currentToken.NextToken?.TokenKind != LilypondTokenKind.Alternative)
                        {
                            inRepeat = false;
                            symbols.Add(new Barline() { RepeatSign = RepeatSignType.Backward, AlternateRepeatGroup = alternativeRepeatNumber });
                        }
                        else if (inAlternative && alternativeRepeatNumber == 1)
                        {
                            alternativeRepeatNumber++;
                            symbols.Add(new Barline() { RepeatSign = RepeatSignType.Backward, AlternateRepeatGroup = alternativeRepeatNumber });
                        }
                        else if (inAlternative && currentToken.NextToken.TokenKind == LilypondTokenKind.SectionEnd)
                        {
                            inAlternative = false;
                            alternativeRepeatNumber = 0;
                        }
                        break;
                    case LilypondTokenKind.SectionStart:
                        if (inAlternative && currentToken.PreviousToken.TokenKind != LilypondTokenKind.SectionEnd)
                        {
                            alternativeRepeatNumber++;
                            symbols.Add(new Barline() { AlternateRepeatGroup = alternativeRepeatNumber });
                        }
                        break;
                    case LilypondTokenKind.Alternative:
                        inAlternative = true;
                        inRepeat = false;
                        currentToken = currentToken.NextToken; // Skip the first bracket open.
                        break;
                    case LilypondTokenKind.Note:
                        // Tied
                        // TODO: A tie, like a dot and cross or mole are decorations on notes. Is the DECORATOR pattern of use here?
                        //TODO: Decorator Pattern!!!
                        NoteTieType tie = NoteTieType.None;
                        if (currentToken.Value.StartsWith("~"))
                        {
                            tie = NoteTieType.Stop;
                            var lastNote = symbols.Last(s => s is Note) as Note;
                            if (lastNote != null) lastNote.TieType = NoteTieType.Start;
                            currentToken.Value = currentToken.Value.Substring(1);
                        }
                        // Length
                        int noteLength = Int32.Parse(Regex.Match(currentToken.Value, @"\d+").Value);
                        // Crosses and Moles
                        int alter = 0;
                        alter += Regex.Matches(currentToken.Value, "is").Count;
                        alter -= Regex.Matches(currentToken.Value, "es|as").Count;
                        // Octaves
                        int distanceWithPreviousNote = notesorder.IndexOf(currentToken.Value[0]) - notesorder.IndexOf(previousNote);
                        if (distanceWithPreviousNote > 3) // Shorter path possible the other way around
                        {
                            distanceWithPreviousNote -= 7; // The number of notes in an octave
                        }
                        else if (distanceWithPreviousNote < -3)
                        {
                            distanceWithPreviousNote += 7; // The number of notes in an octave
                        }

                        if (distanceWithPreviousNote + notesorder.IndexOf(previousNote) >= 7)
                        {
                            previousOctave++;
                        }
                        else if (distanceWithPreviousNote + notesorder.IndexOf(previousNote) < 0)
                        {
                            previousOctave--;
                        }

                        // Force up or down.
                        previousOctave += currentToken.Value.Count(c => c == '\'');
                        previousOctave -= currentToken.Value.Count(c => c == ',');

                        previousNote = currentToken.Value[0];

                        var note = new Note(currentToken.Value[0].ToString().ToUpper(),
                            alter,
                            previousOctave,
                            (MusicalSymbolDuration)noteLength,
                            NoteStemDirection.Up,
                            tie,
                            new List<NoteBeamType>() { NoteBeamType.Single });
                        note.NumberOfDots += currentToken.Value.Count(c => c.Equals('.'));
                        
                        symbols.Add(note);
                        break;
                    case LilypondTokenKind.Rest:
                        var restLength = Int32.Parse(currentToken.Value[1].ToString());
                        symbols.Add(new Rest((MusicalSymbolDuration)restLength));
                        break;
                    case LilypondTokenKind.Bar:
                        symbols.Add(new Barline() { AlternateRepeatGroup = alternativeRepeatNumber });
                        break;
                    case LilypondTokenKind.Clef:
                        currentToken = currentToken.NextToken;
                        if (currentToken.Value == "treble")
                            currentClef = new Clef(ClefType.GClef, 2);
                        else if (currentToken.Value == "bass")
                            currentClef = new Clef(ClefType.FClef, 4);
                        else
                            throw new NotSupportedException($"Clef {currentToken.Value} is not supported.");

                        symbols.Add(currentClef);
                        break;
                    case LilypondTokenKind.Time:
                        currentToken = currentToken.NextToken;
                        var times = currentToken.Value.Split('/');
                        symbols.Add(new PSAMControlLibrary.TimeSignature(TimeSignatureType.Numbers, uint.Parse(times[0]), uint.Parse(times[1])));
                        break;
                    case LilypondTokenKind.Tempo:
                        // Tempo not supported
                        break;
                    default:
                        break;
                }
                currentToken = currentToken.NextToken;
            }

            return symbols;
        }
        
        private static LinkedList<LilypondToken> GetTokensFromLilypond(string content)
        {
            var tokens = new LinkedList<LilypondToken>();

            foreach (string s in content.Split(' ').Where(item => item.Length > 0))
            {
                LilypondToken token = new LilypondToken()
                {
                    Value = s
                };

                //token = CheckTokenKind(s, token);
                //token = CheckTokenKindIsNote(s, token);
                token.TokenKind = new LilypondTokenFactory(s).getTokenKind();

                if (tokens.Last != null)
                {
                    tokens.Last.Value.NextToken = token;
                    token.PreviousToken = tokens.Last.Value;
                }

                tokens.AddLast(token);
            }

            return tokens;
        }

        //private static LilypondToken CheckTokenKindIsNote(string s, LilypondToken token)
        //{
        //    if (token.TokenKind == LilypondTokenKind.Unknown && new Regex(@"[~]?[a-g][,'eis]*[0-9]+[.]*").IsMatch(s))
        //    {
        //        token.TokenKind = LilypondTokenKind.Note;
        //    }
        //    else if (token.TokenKind == LilypondTokenKind.Unknown && new Regex(@"r.*?[0-9][.]*").IsMatch(s))
        //    {
        //        token.TokenKind = LilypondTokenKind.Rest;
        //    }

        //    return token;
        //}

        //private static LilypondToken CheckTokenKind(string s, LilypondToken token)
        //{
        //    token.TokenKind = new LilypondTokenFactory(s).getTokenKind();
        //    return token;
        //}
        #endregion Staffs loading (loads lilypond to WPF staffs)

        #region Saving to files
        internal void SaveToMidi(string fileName)
        {
            Sequence sequence = GetSequenceFromWPFStaffs();

            sequence.Save(fileName);
        }
        
        /// <summary>
        /// We create MIDI from WPF staffs, 2 different dependencies, not a good practice.
        /// TODO: Create MIDI from our own domain classes.
        /// TODO: Our code doesn't support repeats (rendering notes multiple times) in midi yet. Maybe with a COMPOSITE this will be easier?
        /// </summary>
        /// <returns></returns>
        private Sequence GetSequenceFromWPFStaffs()
        {
            List<string> notesOrderWithCrosses = new List<string>() { "c", "cis", "d", "dis", "e", "f", "fis", "g", "gis", "a", "ais", "b" };
            int absoluteTicks = 0;

            Sequence sequence = new Sequence();

            Track metaTrack = new Track();
            sequence.Add(metaTrack);

            // Calculate tempo
            CalculateTempo(ref metaTrack);

            Track notesTrack = new Track();
            sequence.Add(notesTrack);

            for (int i = 0; i < WPFStaffs.Count; i++)
            {
                var musicalSymbol = WPFStaffs[i];
                //TODO: Command Pattern!!!
                switch (musicalSymbol.Type)
                {
                    case MusicalSymbolType.Note:
                        absoluteTicks = CreateMidiNote(notesOrderWithCrosses, absoluteTicks, sequence, ref notesTrack, musicalSymbol);
                        break;
                    case MusicalSymbolType.TimeSignature:
                        CreateMidiTimeSignature(absoluteTicks, ref metaTrack);
                        break;
                    default:
                        break;
                }
            }

            notesTrack.Insert(absoluteTicks, MetaMessage.EndOfTrackMessage);
            metaTrack.Insert(absoluteTicks, MetaMessage.EndOfTrackMessage);
            return sequence;
        }

        private void CalculateTempo(ref Track metaTrack)
        {
            int speed = (60000000 / _bpm);
            byte[] tempo = new byte[3];
            tempo[0] = (byte)((speed >> 16) & 0xff);
            tempo[1] = (byte)((speed >> 8) & 0xff);
            tempo[2] = (byte)(speed & 0xff);
            metaTrack.Insert(0 /* Insert at 0 ticks*/, new MetaMessage(MetaType.Tempo, tempo));
        }

        private void CreateMidiTimeSignature(int absoluteTicks, ref Track metaTrack)
        {
            byte[] timeSignature = new byte[4];
            timeSignature[0] = (byte)_beatsPerBar;
            timeSignature[1] = (byte)(Math.Log(_beatNote) / Math.Log(2));
            metaTrack.Insert(absoluteTicks, new MetaMessage(MetaType.TimeSignature, timeSignature));
        }

        private int CreateMidiNote(List<string> notesOrderWithCrosses, int absoluteTicks, Sequence sequence, ref Track notesTrack, MusicalSymbol musicalSymbol)
        {
            Note note = musicalSymbol as Note;

            // Calculate duration
            double absoluteLength = 1.0 / (double)note.Duration;
            absoluteLength += (absoluteLength / 2.0) * note.NumberOfDots;

            double relationToQuartNote = _beatNote / 4.0;
            double percentageOfBeatNote = (1.0 / _beatNote) / absoluteLength;
            double deltaTicks = (sequence.Division / relationToQuartNote) / percentageOfBeatNote;

            // Calculate height
            int noteHeight = notesOrderWithCrosses.IndexOf(note.Step.ToLower()) + ((note.Octave + 1) * 12);
            noteHeight += note.Alter;
            notesTrack.Insert(absoluteTicks, new ChannelMessage(ChannelCommand.NoteOn, 1, noteHeight, 90)); // Data2 = volume

            absoluteTicks += (int)deltaTicks;
            notesTrack.Insert(absoluteTicks, new ChannelMessage(ChannelCommand.NoteOn, 1, noteHeight, 0)); // Data2 = volume
            return absoluteTicks;
        }

        internal void SaveToPDF(string fileName)
        {
            string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string tmpFileName = $"{fileName}-tmp.ly";
            SaveToLilypond(tmpFileName);

            string lilypondLocation = @"C:\Program Files (x86)\LilyPond\usr\bin\lilypond.exe";
            string sourceFolder = Path.GetDirectoryName(tmpFileName);
            string sourceFileName = Path.GetFileNameWithoutExtension(tmpFileName);
            string targetFolder = Path.GetDirectoryName(fileName);
            string targetFileName = Path.GetFileNameWithoutExtension(fileName);

            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = sourceFolder,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = String.Format("--pdf \"{0}\\{1}.ly\"", sourceFolder, sourceFileName),
                    FileName = lilypondLocation
                }
            };

            process.Start();
            while (!process.HasExited) { /* Wait for exit */ }

            if (sourceFolder != targetFolder || sourceFileName != targetFileName)
            {
                File.Move(sourceFolder + "\\" + sourceFileName + ".pdf", targetFolder + "\\" + targetFileName + ".pdf");
                File.Delete(tmpFileName);
            }
        }

        internal void SaveToLilypond(string fileName)
        {
            using (StreamWriter outputFile = new StreamWriter(fileName))
            {
                outputFile.Write(LilypondText);
                outputFile.Close();
            }
        }
        #endregion Saving to files
    }
}
