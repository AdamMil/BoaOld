/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Text;
using System.Text.RegularExpressions;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class _time
{ _time() { }

  public class struct_time : IRepresentable, ISequence
  { public struct_time(double seconds) : this(toDateTime(seconds)) { }
    public struct_time(DateTime dt)
    { tm_year = dt.Year;
      tm_mon  = dt.Month;
      tm_mday = dt.Day;
      tm_hour = dt.Hour;
      tm_min  = dt.Minute;
      tm_sec  = dt.Second;
      tm_wday = (int)dt.DayOfWeek;
      tm_yday = dt.DayOfYear;
      tm_isdst = TimeZone.CurrentTimeZone.IsDaylightSavingTime(dt) ? 1 : 0;
    }
    public struct_time(Tuple tup)
    { if(tup.items.Length != 9) throw Ops.TypeError("time tuple must have 9 items");
      tm_year  = Ops.ToInt(tup.items[0]);
      tm_mon   = Ops.ToInt(tup.items[1]);
      tm_mday  = Ops.ToInt(tup.items[2]);
      tm_hour  = Ops.ToInt(tup.items[3]);
      tm_min   = Ops.ToInt(tup.items[4]);
      tm_sec   = Ops.ToInt(tup.items[5]);
      tm_wday  = Ops.ToInt(tup.items[6]);
      tm_yday  = Ops.ToInt(tup.items[7]);
      tm_isdst = Ops.ToInt(tup.items[8]);
      
      if(tm_year<0 || tm_mon<1 || tm_mday<1 || tm_hour<0 || tm_min<0 || tm_sec<0 || tm_wday<0 || tm_yday<1 ||
         tm_isdst<-1 || tm_mon>12 || tm_mday>31 || tm_hour>23 || tm_min>59 || tm_sec>61 || tm_wday>6 || tm_yday>366)
        throw Ops.ValueError("tuple does not represent a valid time: "+Ops.Repr(tup));
    }

    #region ISequence Members
    public object __add__(object o) { throw new NotImplementedException(); }
    public object __getitem__(int index)
    { switch(index)
      { case 0: return tm_year;
        case 1: return tm_mon;
        case 2: return tm_mday;
        case 3: return tm_hour;
        case 4: return tm_min;
        case 5: return tm_sec;
        case 6: return tm_wday;
        case 7: return tm_yday;
        case 8: return tm_isdst;
        default: throw Ops.IndexError("invalid index for struct_time: "+index);
      }
    }

    object Boa.Runtime.ISequence.__getitem__(Slice slice) { return gettuple().__getitem__(slice); }

    public int __len__() { return 9; }

    public bool __contains__(object value) { return gettuple().__contains__(value); }
    #endregion

    public Tuple gettuple()
    { return new Tuple(tm_year, tm_mon, tm_mday, tm_hour, tm_min, tm_sec, tm_wday, tm_yday, tm_isdst);
    }

    public override string ToString() { return gettuple().ToString(); }
    public string __repr__() { return gettuple().__repr__(); }

    public int tm_year, tm_mon, tm_mday, tm_hour, tm_min, tm_sec, tm_wday, tm_yday, tm_isdst;
  }

  public static string __repr__() { return "<module 'time' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  public static bool accept2dyear = !Ops.IsTrue(Boa.Modules.dotnet.getenv("BOAY2K"));
  public static int altzone { get { throw new NotImplementedException(); } }
  public static int daylight { get { return TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now) ? 1 : 0; } }
  public static int timezone { get { throw new NotImplementedException(); } }
  public static Tuple tzname
  { get { return new Tuple(TimeZone.CurrentTimeZone.StandardName, TimeZone.CurrentTimeZone.DaylightName); }
  }

  public static string asctime() { return asctime(localtime()); }
  public static string asctime(object time)
  { struct_time st = ObjectToStruct(time);
    return string.Format("{0} {1} {2} {3:D2}:{4:D2}:{5:D2} {6}", weekdays[st.tm_wday], months[st.tm_mon-1],
                         st.tm_mday, st.tm_hour, st.tm_min, st.tm_sec, st.tm_year);
  }

  public static double clock() { throw new NotImplementedException(); }

  public static string ctime() { return asctime(); }
  public static string ctime(double secs) { return asctime(localtime(secs)); }
  
  public static struct_time gmtime() { return new struct_time(DateTime.UtcNow); }
  public static struct_time gmtime(double secs) { return new struct_time(toDateTime(secs)); }
  
  public static struct_time localtime() { return new struct_time(DateTime.Now); }
  public static struct_time localtime(double secs) { return new struct_time(toDateTime(secs).ToLocalTime()); }

  public static double mktime(object time)
  { struct_time st = ObjectToStruct(time);
    return fromDateTime(new DateTime(st.tm_year, st.tm_mon, st.tm_mday, st.tm_hour, st.tm_min, st.tm_sec));
  }

  public static void sleep(double secs) { if(secs>=0) System.Threading.Thread.Sleep((int)(secs*1000)); }

  public static string strftime(string format, object time)
  { FormatReplacer fe = new FormatReplacer(ObjectToStruct(time));
    return pctre.Replace(format, new MatchEvaluator(fe.Replace));
  }

  public static struct_time strptime(string time) { return strptime(time, "%a %b %d %H:%M:%S %Y"); }
  public static struct_time strptime(string time, string format) { throw new NotImplementedException(); }

  public static double time() { return fromDateTime(DateTime.UtcNow); }

  public static double fromDateTime(DateTime dt) { return dt.ToFileTime()/10000000.0; }
  public static DateTime toDateTime(double secs) { return DateTime.FromFileTime((long)(secs*10000000)); }
  
  class FormatReplacer
  { public FormatReplacer(struct_time st)
    { this.dt=new DateTime(st.tm_year, st.tm_mon, st.tm_mday, st.tm_hour, st.tm_min, st.tm_sec);
    }

    public string Replace(Match m)
    { System.Globalization.DateTimeFormatInfo df = System.Globalization.DateTimeFormatInfo.CurrentInfo;

      switch(m.Groups[1].Value[0])
      { case 'a': return df.GetAbbreviatedDayName(dt.DayOfWeek);
        case 'A': return df.GetDayName(dt.DayOfWeek);
        case 'b': return df.GetAbbreviatedMonthName(dt.Month);
        case 'B': return df.GetMonthName(dt.Month);
        case 'c': return dt.ToShortDateString() + ' ' + dt.ToShortTimeString();
        case 'd': return Pad(dt.Day);
        case 'H': return Pad(dt.Hour);
        case 'I':
          int hour = dt.Hour%12;
          if(hour==0) hour=12;
          return Pad(hour);
        case 'j': return dt.DayOfYear.ToString("D3");
        case 'm': return Pad(dt.Month);
        case 'M': return Pad(dt.Minute);
        case 'p': return dt.Hour<12 ? df.AMDesignator : df.PMDesignator;
        case 'S': return Pad(dt.Second);
        case 'U': throw new NotImplementedException();
        case 'w': return ((int)dt.DayOfWeek).ToString();
        case 'W': throw new NotImplementedException();
        case 'x': return dt.ToShortDateString();
        case 'X': return dt.ToShortTimeString();
        case 'y': return Pad(dt.Year%100);
        case 'Y': return dt.Year.ToString();
        case 'Z': return TimeZone.CurrentTimeZone.StandardName;
        case '%': return "%";
        default: throw Ops.ValueError("unknown time format code "+m.Value);
      }
    }
    
    string Pad(int i) { return i<10 ? "0"+i.ToString() : i.ToString(); }

    DateTime dt;
  }

  static struct_time ObjectToStruct(object time)
  { if(time is struct_time) return (struct_time)time;
    if(time is Tuple) return new struct_time((Tuple)time);
    throw Ops.TypeError("invalid type used as a time: "+Ops.TypeName(time));
  }
  
  static readonly string[] months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep",
                                                   "Oct", "Nov", "Dec" };
  static readonly string[] weekdays = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
  static Regex pctre = new Regex(@"%(.)", RegexOptions.Singleline);
}

} // namespace Boa.Modules