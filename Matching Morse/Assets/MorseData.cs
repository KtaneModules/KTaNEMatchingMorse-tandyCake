using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MorseData
{
    public static readonly Dictionary<char, string> MorseTranslation = new Dictionary<char, string>()
    {
        { 'A', ".-"   },
        { 'B', "...-" },
        { 'C', "-.-." },
        { 'D', "-.."  },
        { 'E', "."    },
        { 'F', "..-." },
        { 'G', "--."  },
        { 'H', "...." },
        { 'I', ".."   },
        { 'J', ".---" },
        { 'K', "-.-"  },
        { 'L', ".-.." },
        { 'M', "--"   },
        { 'N', "-."   },
        { 'O', "---"  },
        { 'P', ".--." },
        { 'Q', "--.-" },
        { 'R', ".-."  },
        { 'S', "..."  },
        { 'T', "-"    },
        { 'U', "..-"  },
        { 'V', "...-" },
        { 'W', ".--"  },
        { 'X', "-..-" },
        { 'Y', "-.--" },
        { 'Z', "--.." },
        { '0', "-----"},
        { '1', ".----"},
        { '2', "..---"},
        { '3', "...--"},
        { '4', "....-"},
        { '5', "....."},
        { '6', "-...."},
        { '7', "--..."},
        { '8', "---.."},
        { '9', "----."},
    };

    public static string GenerateSequence(char ch)
    {
        string output = string.Empty;
        foreach (char unit in MorseTranslation[ch])
        {
            if (unit == '-')
                output += "xxx";
            else output += "x";
            output += ".";
        }
        return output + "..";
    }
}
