// 
// FactoryStatus.cs
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

namespace OpenSuse.BuildService.Tools
{
    public static class FactoryStatus
    {
        private class Package
        {
            public string Project { get; set; }
            public string Name { get; set; }
            public string SrcMd5 { get; set; }
            public Account Account { get; set; }
        }
        
        private static OscRcAccountCollection accounts;
    
        public static void Main (string [] args)
        {
            if (args.Length < 2) {
                Console.Error.WriteLine ("factory-status: [https://API_URL/]DEVEL_PROJECTS [https://API_URL/]FACTORY_PROJECT");
                return;
            }

            accounts = new OscRcAccountCollection ();
            
            Console.Error.Write ("\r");
            Console.Write ("Loading revision data... ");
            
            var devel_packages = new Dictionary<string, Package> ();
            var factory_packages = new Dictionary<string, Package> ();
            
            for (int i = 0; i < args.Length - 1; i++) {
                LoadPackages (devel_packages, args[i]);
            }

            LoadPackages (factory_packages, args[args.Length - 1]);
            
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
            
            var to_update = new List<Package> ();
            var to_remove = new List<Package> ();
            
            // Find packages that are not up-to-date
            foreach (var package in devel_packages.Values) {
                if (!factory_packages.ContainsKey (package.Name) || 
                    factory_packages[package.Name].SrcMd5 != package.SrcMd5) {
                    to_update.Add (package);
                }
            }
            
            to_update.Sort ((a, b) => 
                a.Project == b.Project 
                    ? a.Name.CompareTo (b.Name)
                    : a.Project.CompareTo (b.Project));
            
            // Find packages that are obsolete
            foreach (var package in factory_packages.Values) {
                if (!devel_packages.ContainsKey (package.Name)) {
                    to_remove.Add (package);
                }
            }
            
            Console.WriteLine ();
            Console.WriteLine ("Loaded revision data.");
            Console.WriteLine ();
            
            if (to_update.Count == 0 && to_remove.Count == 0) {
                Console.WriteLine ("The Factory project is up-to-date. Congratulations!");
                return;
            }
            
            if (to_update.Count > 0) {
                Console.WriteLine ("Packages that need updating in Factory ({0}):", to_update.Count);
                foreach (var package in to_update) {
                    Console.WriteLine ("  - {0} ({1}, {2})", package.Name, package.Project, package.SrcMd5);
                }
            }
            
            if (to_remove.Count > 0) {
                Console.WriteLine ("Packages not in a devel project ({0}):", to_remove.Count);
                foreach (var package in to_remove) {
                    Console.WriteLine ("  - {0}", package.Name);
                }
            }
        }
        
        private static void LoadPackages (Dictionary<string, Package> packages, string project)
        {
            var account = accounts.DefaultAccount;
            if (project.StartsWith ("http:") || project.StartsWith ("https:")) {
                string api = project.Substring (0, project.LastIndexOf ("/"));
                project = project.Substring (api.Length + 1);
                account = accounts[api];
            }

            var doc = XDocument.Load (XmlReader.Create (account.ApiRequest.Get ("/source/" + project)));
            foreach (var package in 
                from entry in doc.Descendants ("entry")
                select (string)entry.Attribute ("name")) {
                packages.Add (package, new Package () {
                    Project = project,
                    Name = package,
                    Account = account
                });
            }
        }
        
        private static string GetPackageCurrentRevision (Package package)
        {
            return (from rev in XDocument.Load (XmlReader.Create (package.Account.ApiRequest.Get (
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

