/*
Boaide is a simple IDE for working with the Boa language.

Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targetting the Microsoft .NET Framework.

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
using System.IO;

namespace Boa.IDE
{

class App
{ static void Main()
  { MemoryStream ms = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("hello, world!"));
    PieceTable pt = new PieceTable(ms, System.Text.Encoding.ASCII);

    
  }
}

} // namespace Boa.IDE