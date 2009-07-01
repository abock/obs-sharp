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
using System.Collections.Generic;

using OpenSuse.BuildService;

namespace MoblinTree
{
    public static class TreeStatus
    {
        private class Package
        {
            public string Project { get; set; }
            public string Name { get; set; }
            public string SrcMd5 { get; set; }
        }
    
        private static ApiRequest api;
    
        public static void Main ()
        {
            api = new ApiRequest () {
                Account = new OscRcAccountCollection ().DefaultAccount
            };
            
            Console.Write ("\rLoading revision data... ");
            
            var devel_packages = new Dictionary<string, Package> ();
            var factory_packages = new Dictionary<string, Package> ();
            
            LoadPackages (devel_packages, "Moblin:Base", "Moblin:UI");
            LoadPackages (factory_packages, "Moblin:Factory");
            
            int total_count = devel_packages.Count + factory_packages.Count;
            int completed_count = 0;
            var start_time = DateTime.Now;
            
            foreach (var package_set in new Dictionary<string, Package> [] { devel_packages, factory_packages }) {
                foreach (var entry in package_set) {
                    var package = package_set[entry.Key];
                    package.SrcMd5 = GetPackageCurrentRevision (package);        
                    UpdateStatus (start_time, total_count, ++completed_count);
                }
            }
            
            Console.WriteLine ();
            Console.WriteLine ("Loaded revision data.");
            
            // Find packages that are not up-to-date in Moblin:Factory
            foreach (var package in devel_packages.Values) {
                if (!factory_packages.ContainsKey (package.Name) || 
                    factory_packages[package.Name].SrcMd5 != package.SrcMd5) {
                    Console.WriteLine (package.Name);
                }
            }
        }
        
        private static void LoadPackages (Dictionary<string, Package> packages, params string [] projects)
        {
            foreach (var project in projects) {
                var doc = XDocument.Load (XmlReader.Create (api.Get ("/source/" + project)));
                foreach (var package in 
                    from entry in doc.Descendants ("entry")
                    select (string)entry.Attribute ("name")) {
                    packages.Add (package, new Package () {
                        Project = project,
                        Name = package
                    });
                }
            }
        }
        
        private static string GetPackageCurrentRevision (Package package)
        {
            return (from rev in XDocument.Load (XmlReader.Create (api.Get (
                "/source/" + package.Project + "/" + package.Name + "/_history"))).Descendants ("revision")
                orderby (int)rev.Attribute ("rev") descending
                select (string)rev.Element ("srcmd5")).First ();
        }
        
        private static void UpdateStatus (DateTime startTime, int totalCount, int completedCount)
        {
            double progress = completedCount / (double)totalCount;
            int remaining_count = totalCount - completedCount;
            
            var ellapsed_time = DateTime.Now - startTime;
            var remaining_time = TimeSpan.FromMilliseconds ((double)remaining_count
                * (ellapsed_time.TotalMilliseconds / (double)completedCount));
            
            string status = String.Format (" {0} / {1} [] {2:0}% ({3}{4:00}:{5:00} ETA)",
                completedCount, totalCount, progress * 100,
                remaining_time.Hours > 0 ? remaining_time.Hours.ToString () + ":" : String.Empty,
                remaining_time.Minutes,
                remaining_time.Seconds);
                
            int bar_size = (Console.BufferWidth > 0 ? Console.BufferWidth : 80) - status.Length - 2;
            int progress_size = (int)Math.Round (progress * bar_size);
            string bar = String.Empty.PadRight (progress_size, '=') + String.Empty.PadRight (bar_size - progress_size);
            
            Console.Error.Write ("\r{0} ", status.Replace ("[]", "[" + bar + "]"));
        }
    }
}

