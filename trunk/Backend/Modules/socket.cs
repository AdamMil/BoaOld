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
using System.Net;
using System.Net.Sockets;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class socket
{ socket() { }

  #region Support classes
  public class error : RuntimeException
  { public error(int code, string desc) { value = new Tuple(code, desc); }
    public error(object value) { this.value = value; }

    public object value;
  }
  
  public class herror : error
  { public herror(int code, string desc) : base(code, desc) { }
    public herror(object value) : base(value) { }
  }
  
  public class gaierror : error
  { public gaierror(int code, string desc) : base(code, desc) { }
    public gaierror(object value) : base(value) { }
  }
  
  public class timeout : error
  { public timeout() : base("timed out") { }
  }
  #endregion

  #region Constants
  public const int AF_INET   = (int)AddressFamily.InterNetwork;
  public const int AF_INET6  = (int)AddressFamily.InterNetworkV6;
  public const int AF_UNIX   = (int)AddressFamily.Unix;
  public const int AF_UNSPEC = (int)AddressFamily.Unspecified;
  
  public const int PF_INET   = (int)ProtocolFamily.InterNetwork;
  public const int PF_INET6  = (int)ProtocolFamily.InterNetworkV6;
  public const int PF_UNIX   = (int)ProtocolFamily.Unix;
  public const int PF_UNSPEC = (int)ProtocolFamily.Unspecified;
  
  public const int SOCK_STREAM = (int)SocketType.Stream;
  public const int SOCK_DGRAM  = (int)SocketType.Dgram;
  public const int SOCK_RAW    = (int)SocketType.Raw;
  public const int SOCK_RDM    = (int)SocketType.Rdm;
  public const int SOCK_SEQPACKET = (int)SocketType.Seqpacket;

  public const int MSG_OOB = (int)SocketFlags.OutOfBand;
  public const int MSG_PEEK = (int)SocketFlags.Peek;
  public const int MSG_DONTROUTE = (int)SocketFlags.DontRoute;

  public const int SOL_SOCKET   = (int)SocketOptionLevel.Socket;
  public const int IPPROTO_ICMP = (int)ProtocolType.Icmp;
  public const int IPPROTO_IP   = (int)SocketOptionLevel.IP;
  public const int IPPROTO_IPV6 = (int)SocketOptionLevel.IPv6;
  public const int IPPROTO_RAW  = (int)ProtocolType.Raw;
  public const int IPPROTO_TCP  = (int)SocketOptionLevel.Tcp;
  public const int IPPROTO_UDP  = (int)SocketOptionLevel.Udp;

  public const int SO_BROADCAST = (int)SocketOptionName.Broadcast;
  public const int SO_DEBUG = (int)SocketOptionName.Debug;
  public const int SO_DONTROUTE = (int)SocketOptionName.DontRoute;
  public const int SO_KEEPALIVE = (int)SocketOptionName.KeepAlive;
  public const int SO_LINGER = (int)SocketOptionName.Linger;
  public const int SO_OOBINLINE = (int)SocketOptionName.OutOfBandInline;
  public const int SO_RCVBUF = (int)SocketOptionName.ReceiveBuffer;
  public const int SO_REUSEADDR = (int)SocketOptionName.ReuseAddress;
  public const int SO_SNDBUF = (int)SocketOptionName.SendBuffer;

  public const int TCP_NODELAY = (int)SocketOptionName.NoDelay;
  
  public const bool has_ipv6 = Socket.SupportsIPv6;
  #endregion

  public static string __repr__() { return "<module 'socket' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static object getdefaulttimeout() { return defaultTimeout<=0 ? null : defaultTimeout; }
  public static void setdefaulttimeout(object value)
  { if(value==null) defaultTimeout = -1;
    else defaultTimeout = Ops.ToFloat(value);
  }

  public static Tuple getaddrinfo(string host, int port)
  { return getaddrinfo(host, port, AF_INET, SOCK_STREAM, 0, 0);
  }
  public static Tuple getaddrinfo(string host, int port, int family)
  { return getaddrinfo(host, port, family, SOCK_STREAM, 0, 0);
  }
  public static Tuple getaddrinfo(string host, int port, int family, int socktype)
  { return getaddrinfo(host, port, family, socktype, 0, 0);
  }
  public static Tuple getaddrinfo(string host, int port, int family, int socktype, int proto)
  { return getaddrinfo(host, port, family, socktype, proto, 0);
  }
  public static Tuple getaddrinfo(string host, int port, int family, int socktype, int proto, int flags)
  { IPHostEntry he = Resolve(host);
    return new Tuple(family, socktype, proto, he.HostName, new Tuple(he.HostName, port));
  }

  public static string getfqdn() { return getfqdn(Dns.GetHostName()); }
  public static string getfqdn(string host)
  { IPHostEntry he = GetHostByName(host);
    if(he.HostName.IndexOf('.')!=-1) return he.HostName;
    foreach(string s in he.Aliases) if(s.IndexOf('.')!=-1) return s;
    return host;
  }

  public static Tuple gethostbyaddr(string address) { return HEntryToTuple(GetHostByAddress(address)); }
  public static Tuple gethostbyaddr(IPAddress address) { return HEntryToTuple(GetHostByAddress(address)); }
  
  public static string gethostbyname(string host) { return GetHostByName(host).AddressList[0].ToString(); }
  public static string gethostbyname_ex(string host) { return HEntryToTuple(GetHostByName(host)); }

  public static string gethostname() { return Dns.GetHostName(); }

  public static Tuple resolve(string ipOrHost) { return HEntryToTuple(Resolve(ipOrHost)); }
  
  public static Boa.Modules.Socket socket() { return socket(AF_INET, SOCK_STREAM, 0); }
  public static Boa.Modules.Socket socket(int family) { return socket(family, SOCK_STREAM, 0); }
  public static Boa.Modules.Socket socket(int family, int type) { return socket(family, type, 0); }
  public static Boa.Modules.Socket socket(int family, int type, int proto)
  { return new Boa.Modules.Socket((AddressFamily)family, (SocketType)type, (ProtocolType)proto);
  }

  public static int htonl(int i)
  { 
    #if BIG_ENDIAN
    return i;
    #else
    return IPAddress.HostToNetworkOrder(i);
    #endif
  }

  public static short htons(short i)
  { 
    #if BIG_ENDIAN
    return i;
    #else
    return IPAddress.HostToNetworkOrder(i);
    #endif
  }

  public static int ntohl(int i)
  { 
    #if BIG_ENDIAN
    return i;
    #else
    return IPAddress.NetworkToHostOrder(i);
    #endif
  }

  public static short ntohs(short i)
  { 
    #if BIG_ENDIAN
    return i;
    #else
    return IPAddress.NetworkToHostOrder(i);
    #endif
  }

  static Tuple HEntryToTuple(IPHostEntry he)
  { List addrs = new List(he.AddressList.Length);
    for(int i=0; i<he.AddressList.Length; i++) addrs.Add(he.AddressList[i].ToString);
    return new Tuple(he.HostName, new List(he.Aliases), addrs);
  }
  
  static double defaultTimeout = -1;
}

} // namespace Boa.Modules
