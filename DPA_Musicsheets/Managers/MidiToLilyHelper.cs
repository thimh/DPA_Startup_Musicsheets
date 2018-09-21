using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Managers
{
    /// <summary>
    /// TODO: Static classes with static helper methods are not done. Can a better domain class help with this?
    /// </summary>
    //TODO: Implement Domain Classes!!!
    public static class MidiToLilyHelper
    {
        public static string GetLilypondNoteLength(
            int absoluteTicks,
            int nextNoteAbsoluteTicks,
            int division,
            int beatNote,
            int beatsPerBar,
            out double percentageOfBar)
        {
            int duration = 0;
            int dots = 0;

            double deltaTicks = nextNoteAbsoluteTicks - absoluteTicks;

            if (deltaTicks <= 0)
            {
                percentageOfBar = 0;
                return String.Empty;
            }

            double percentageOfBeatNote = deltaTicks / division;
            percentageOfBar = (1.0 / beatsPerBar) * percentageOfBeatNote;

            for (int noteLength = 32; noteLength >= 1; noteLength -= 1)
            {
                double absoluteNoteLength = (1.0 / noteLength);

                if (percentageOfBar <= absoluteNoteLength)
                {
                    if (noteLength < 2)
                        noteLength = 2;

                    int subtractDuration;

                    if (noteLength == 32)
                        subtractDuration = 32;
                    else if (noteLength >= 16)
                        subtractDuration = 16;
                    else if (noteLength >= 8)
                        subtractDuration = 8;
                    else if (noteLength >= 4)
                        subtractDuration = 4;
                    else
                        subtractDuration = 2;

                    if (noteLength >= 17)
                        duration = 32;
                    else if (noteLength >= 9)
                        duration = 16;
                    else if (noteLength >= 5)
                        duration = 8;
                    else if (noteLength >= 3)
                        duration = 4;
                    else
                        duration = 2;

                    double currentTime = 0;

                    AddDots(beatNote, ref dots, noteLength, subtractDuration, ref currentTime);

                    break;
                }
            }

            return duration + new String('.', dots);
        }

        private static void AddDots(int beatNote, ref int dots, int noteLength, int subtractDuration, ref double currentTime)
        {
            while (currentTime < (noteLength - subtractDuration))
            {
                var addtime = 1 / ((subtractDuration / beatNote) * Math.Pow(2, dots));
                if (addtime <= 0) break;
                currentTime += addtime;
                if (currentTime <= (noteLength - subtractDuration))
                {
                    dots++;
                }
                if (dots >= 4) break;
            }
        }

        public static string GetLilyNoteName(int previousMidiKey, int midiKey)
        {
            int octave = (midiKey / 12) - 1;
            string name = "";
            switch (midiKey % 12)
            {
                case 0:
                    name = "c";
                    break;
                case 1:
                    name = "cis";
                    break;
                case 2:
                    name = "d";
                    break;
                case 3:
                    name = "dis";
                    break;
                case 4:
                    name = "e";
                    break;
                case 5:
                    name = "f";
                    break;
                case 6:
                    name = "fis";
                    break;
                case 7:
                    name = "g";
                    break;
                case 8:
                    name = "gis";
                    break;
                case 9:
                    name = "a";
                    break;
                case 10:
                    name = "ais";
                    break;
                case 11:
                    name = "b";
                    break;
            }

            int distance = midiKey - previousMidiKey;

            SetOctaveHigher(ref name, ref distance);

            SetOctaveLower(ref name, ref distance);

            return name;
        }

        private static void SetOctaveLower(ref string name, ref int distance)
        {
            while (distance > 6)
            {
                name += "'";
                distance -= 8;
            }
        }

        private static void SetOctaveHigher(ref string name, ref int distance)
        {
            while (distance < -6)
            {
                name += ",";
                distance += 8;
            }
        }
    }
}
