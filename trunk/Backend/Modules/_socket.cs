/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

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
public sealed class _socket
{ _socket() { }

  #region Support classes
  public class error : RuntimeException
  { public error(int code, string message) : base(message) { this.code=code; }
    public error(string message) : base(message) { }

    public int code;
  }
  
  public class herror : error
  { public herror(int code, string message) : base(code, message) { }
    public herror(string message) : base(message) { }
  }
  
  public class gaierror : error
  { public gaierror(int code, string message) : base(code, message) { }
    public gaierror(string message) : base(message) { }
  }

  public class timeout : error
  { public timeout() : base("timed out") { }
  }

  // TODO: support IPv6 addresses
  public class socket
  { public socket() : this(AF_INET, SOCK_STREAM, 0) { }
    public socket(int family) : this(family, SOCK_STREAM, 0) { }
    public socket(int family, int type) : this(family, type, 0) { }
    public socket(int family, int type, int proto) :
      this((AddressFamily)family, (SocketType)type, (ProtocolType)proto) { }
    public socket(AddressFamily family, SocketType type, ProtocolType proto)
    { try { Socket = new Socket(family, type, proto); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public socket(Socket socket) { Socket=socket; }

    public Tuple accept()
    { try
      { socket s = new socket(Socket.Accept());
        return new Tuple(s, s.getpeername());
      }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }
    
    public void bind(Tuple address)
    { try { Socket.Bind(AddressToEndpoint(address)); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }
    
    public void close() { Socket.Close(); }

    public void connect(Tuple address)
    { try { Socket.Connect(AddressToEndpoint(address)); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public Tuple getpeername()
    { try
      { IPEndPoint ep = (IPEndPoint)Socket.RemoteEndPoint;
        return new Tuple(ep.Address.ToString(), ep.Port);
      }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }
    
    public Tuple getsockname()
    { try
      { IPEndPoint ep = (IPEndPoint)Socket.LocalEndPoint;
        return new Tuple(ep.Address.ToString(), ep.Port);
      }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public object getsockopt(int level, int name)
    { try { return Socket.GetSocketOption((SocketOptionLevel)level, (SocketOptionName)name); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public void listen() { listen(5); }
    public void listen(int backlog)
    { try { Socket.Listen(backlog); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public BoaFile makefile() { return new BoaFile(new NetworkStream(Socket, false)); }
    public BoaFile makefile(string mode) { return makefile(); }
    public BoaFile makefile(string mode, int bufsize) { return makefile(); }

    public byte[] recv(int bufsize) { return recv(bufsize, 0); }
    public byte[] recv(int bufsize, int flags)
    { try
      { byte[] arr = new byte[Math.Min(bufsize, 4096)];
        int read = Socket.Receive(arr, (SocketFlags)flags);
        if(read==arr.Length) return arr;
        byte[] ret = new byte[read];
        if(read>0) Array.Copy(arr, ret, read);
        return ret;
      }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public string recvstr(int bufsize) { return recvstr(bufsize, 0); }
    public string recvstr(int bufsize, int flags)
    { return System.Text.Encoding.Default.GetString(recv(bufsize, flags));
    }

    public Tuple recvfrom(int bufsize) { return recvfrom(bufsize, 0); }
    public Tuple recvfrom(int bufsize, int flags)
    { try
      { EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        byte[] arr = new byte[Math.Min(bufsize, 4096)];
        int read = Socket.ReceiveFrom(arr, (SocketFlags)flags, ref ep);
        if(read!=arr.Length)
        { byte[] narr = new byte[read];
          if(read>0) Array.Copy(arr, narr, read);
          arr = narr;
        }
        IPEndPoint iep = (IPEndPoint)ep;
        return new Tuple(arr, new Tuple(iep.Address.ToString(), iep.Port));
      }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public Tuple recvstrfrom(int bufsize) { return recvstrfrom(bufsize, 0); }
    public Tuple recvstrfrom(int bufsize, int flags)
    { Tuple tup = recvfrom(bufsize, flags);
      tup.items[0] = System.Text.Encoding.Default.GetString((byte[])tup.items[0]);
      return tup;
    }

    public int send(byte[] data) { return send(data, 0); }
    public int send(byte[] data, int flags)
    { try { return Socket.Send(data, (SocketFlags)flags); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public int send(string str) { return send(str, 0); }
    public int send(string str, int flags) { return send(System.Text.Encoding.Default.GetBytes(str), flags); }

    public void sendall(byte[] data) { sendall(data, 0); }
    public void sendall(byte[] data, int flags)
    { try
      { int sent=0;
        while(sent<data.Length) sent += Socket.Send(data, sent, data.Length-sent, (SocketFlags)flags);
      }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public void sendall(string str) { sendall(str, 0); }
    public void sendall(string str, int flags) { sendall(System.Text.Encoding.Default.GetBytes(str), flags); }

    public int sendto(byte[] data, Tuple address) { return sendto(data, 0, address); }
    public int sendto(byte[] data, int flags, Tuple address)
    { try { return Socket.SendTo(data, (SocketFlags)flags, AddressToEndpoint(address)); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }
    
    public void setblocking(object value) { Socket.Blocking = Ops.IsTrue(value); }

    public void setsockopt(int level, int name, object value)
    { try { Socket.SetSocketOption((SocketOptionLevel)level, (SocketOptionName)name, value); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }

    public void shutdown(int how)
    { try { Socket.Shutdown((SocketShutdown)how); }
      catch(SocketException e) { throw new error(e.ErrorCode, e.Message); }
      catch(Exception e) { throw new error(e.Message); }
    }
    
    public override string ToString() { return "<socket object>"; }

    public Socket Socket;
    
    static IPEndPoint AddressToEndpoint(Tuple address)
    { IPHostEntry he = Resolve(Ops.ToString(address.__getitem__(0)));
      return new IPEndPoint(he.AddressList[0], Ops.ToInt(address.__getitem__(1)));
    }
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
  
  public const int SHUT_RD   = (int)SocketShutdown.Receive;
  public const int SHUT_WR   = (int)SocketShutdown.Send;
  public const int SHUT_RDWR = (int)SocketShutdown.Both;

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
  
  public static readonly bool has_ipv6 = Socket.SupportsIPv6;
  #endregion
  
  public static readonly ReflectedType linger = ReflectedType.FromType(typeof(LingerOption));

  public static string __repr__() { return "<module 'socket' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static List getaddrinfo(string host, int port)
  { return getaddrinfo(host, port, AF_INET, SOCK_STREAM, 0, 0);
  }
  public static List getaddrinfo(string host, int port, int family)
  { return getaddrinfo(host, port, family, SOCK_STREAM, 0, 0);
  }
  public static List getaddrinfo(string host, int port, int family, int socktype)
  { return getaddrinfo(host, port, family, socktype, 0, 0);
  }
  public static List getaddrinfo(string host, int port, int family, int socktype, int proto)
  { return getaddrinfo(host, port, family, socktype, proto, 0);
  }
  public static List getaddrinfo(string host, int port, int family, int socktype, int proto, int flags)
  { IPHostEntry he = Resolve(host);
    List list = new List(he.AddressList.Length);
    foreach(IPAddress addr in he.AddressList)
      list.Add(new Tuple((int)addr.AddressFamily, socktype, proto, he.HostName, new Tuple(addr.ToString(), port)));
    return list;
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
  public static Tuple gethostbyname_ex(string host) { return HEntryToTuple(GetHostByName(host)); }

  public static string gethostname() { return Dns.GetHostName(); }

  public static Tuple resolve(string ipOrHost) { return HEntryToTuple(Resolve(ipOrHost)); }
  
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

  static IPHostEntry GetHostByAddress(string address)
  { try { return Dns.GetHostByAddress(address); }
    catch(Exception e) { throw new herror(e.Message); }
  }

  static IPHostEntry GetHostByAddress(IPAddress address)
  { try { return Dns.GetHostByAddress(address); }
    catch(Exception e) { throw new herror(e.Message); }
  }

  static IPHostEntry GetHostByName(string host)
  { try { return Dns.GetHostByName(host); }
    catch(Exception e) { throw new herror(e.Message); }
  }

  static Tuple HEntryToTuple(IPHostEntry he)
  { List addrs = new List(he.AddressList.Length);
    for(int i=0; i<he.AddressList.Length; i++) addrs.Add(he.AddressList[i].ToString());
    return new Tuple(he.HostName, new List(he.Aliases), addrs);
  }

  static IPHostEntry Resolve(string ipOrHost)
  { try { return Dns.Resolve(ipOrHost); }
    catch(Exception e) { throw new herror(e.Message); }
  }
}

} // namespace Boa.Modules
