﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace P2VBL
{
    public static class RssHelpers
    {
        public static TimeSpan ToTimeSpan(this string offset)
        {
            int ms = 0;
            var offsetSplit = offset.Split(":");
            if (offsetSplit.Length < 3) throw new Exception($"Cannot parse '{offset}'");

            bool parseSuccess=int.TryParse(offsetSplit[0], out int hour);
            parseSuccess &= int.TryParse(offsetSplit[1], out int minute);

            var secondSplit = offsetSplit[2].Split(".");
            parseSuccess &= int.TryParse(secondSplit[0], out int second);
            if (secondSplit.Length>1)
            {
                parseSuccess &= int.TryParse(secondSplit[1], out ms);
            }

            if (!parseSuccess) throw new Exception($"Cannot parse '{offset}'");
            return new TimeSpan(0, hour, minute, second, ms);
        }
    }
}