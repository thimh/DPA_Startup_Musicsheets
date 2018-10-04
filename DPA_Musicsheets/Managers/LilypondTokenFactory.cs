using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DPA_Musicsheets.Models;

namespace DPA_Musicsheets.Managers
{
    class LilypondTokenFactory
    {
        private string _stringToken;

        public LilypondTokenFactory(string s)
        {
            _stringToken = s;
        }

        public LilypondTokenKind getTokenKind()
        {
            switch (_stringToken)
            {
                case "\\relative":
                    return LilypondTokenKind.Staff;
                case "\\clef":
                    return LilypondTokenKind.Clef;
                case "\\time":
                    return LilypondTokenKind.Time;
                case "\\tempo":
                    return LilypondTokenKind.Tempo;
                case "\\repeat":
                    return LilypondTokenKind.Repeat;
                case "\\alternative":
                    return LilypondTokenKind.Alternative;
                case "{":
                    return LilypondTokenKind.SectionStart;
                case "}":
                    return LilypondTokenKind.SectionEnd;
                case "|":
                    return LilypondTokenKind.Bar;
                default:
                    if (new Regex(@"[~]?[a-g][,'eis]*[0-9]+[.]*").IsMatch(_stringToken))
                    {
                        return LilypondTokenKind.Note;
                    } else if (new Regex(@"r.*?[0-9][.]*").IsMatch(_stringToken))
                    {
                        return LilypondTokenKind.Rest;
                    }
                    else
                    {
                        return LilypondTokenKind.Unknown;
                    }
            }
        }
    }
}
