// 
// TreeStatus.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using OpenSuse.BuildService;

namespace MoblinTree
{
    public static class TreeStatus
    {
        public static void Main ()
        {
            var api = new ApiRequest () {
                Account = new OscRcAccountCollection ().DefaultAccount
            };
            
            var doc = XDocument.Load (XmlReader.Create (
                api.Get ("/source/Moblin:UI/moblin-branding-opensuse/_history")));
            var md5sum = (from rev in doc.Descendants ("revision")
                orderby (int)rev.Attribute ("rev") descending
                select (string)rev.Element ("srcmd5")).First ();
            
            Console.WriteLine (md5sum);
        }
    }
}
