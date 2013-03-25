// 
// Copyright (c) 2010 Ricardo Pescuma Domenecci
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
// 
// A big thanks to Audaces for allowing me to open source this project.
// 
using System;
using System.Collections.Generic;
using System.Linq;

namespace cover2emma
{
	public class DotCoverToEmma
	{
		public emma.report ToEmma(string filename)
		{
			dotcover.Root dc = Utils.ReadXML<dotcover.Root>(filename);

			Fix(dc);

			return ConvertToEmma(dc);
		}

		private static void Fix(dotcover.Root dc)
		{
			if (dc.Assembly == null)
				dc.Assembly = new dotcover.Assembly[0];

			ForEachClass(dc, type =>
			                 	{
			                 		if (type.Items == null)
                                        type.Items = new object[0];
			                 	});
		}

		private static emma.report ConvertToEmma(dotcover.Root dc)
		{
			emma.report ret = new emma.report();

			ret.stats = CreateStats(dc);

			ret.data = new emma.data();
			ret.data.all = CreateAll(dc);

			return ret;
		}

		private static emma.all CreateAll(dotcover.Root dc)
		{
			emma.all ret = new emma.all();

			ret.name = "all classes";
			ret.coverage = CreateCoverage(dc);

			List<emma.package> pkgs = new List<emma.package>();
			foreach (var assembly in dc.Assembly)
			{
                if (assembly.Namespace != null)
                {
                    foreach (var ns in assembly.Namespace)
                    {
                        emma.package pkg = new emma.package();

                        pkg.name = ns.Name;
                        pkg.coverage = CreateCoverage(ns);

                        CreateTypes(pkg, ns.Type);

                        pkgs.Add(pkg);
                    }
                }
                if (assembly.Type != null)
                {
                    emma.package pkg = new emma.package();

                    pkg.name = assembly.Name;
                    pkg.coverage = CreateCoverage(assembly);

                    CreateTypes(pkg, assembly.Type);

                    pkgs.Add(pkg);
                }
            }
			ret.package = pkgs.ToArray();

			return ret;
        }

        private static void CreateTypes(emma.package pkg, dotcover.Type[] types)
        {
			List<emma.srcfile> files = new List<emma.srcfile>();

			foreach (var type in types)
			{
				emma.srcfile file = new emma.srcfile();

				file.name = type.Name + ".cs_guess";
				file.coverage = CreateCoverage(type);

				file.@class = new emma.@class[1];
				file.@class[0] = CreateClass(type);

				files.Add(file);
			}
			pkg.srcfile = files.ToArray();
		}

		private static List<dotcover.Type> GetTypes(dotcover.Assembly assembly)
		{
			List<dotcover.Type> types = new List<dotcover.Type>();
			if (assembly.Namespace != null)
				foreach (var ns in assembly.Namespace)
					types.AddRange(ns.Type);
			if (assembly.Type != null)
				types.AddRange(assembly.Type);
			return types;
		}

		private static emma.@class CreateClass(dotcover.Type type)
		{
			emma.@class result = new emma.@class();

			result.name = type.Name;

			var objects = new List<object>();
			objects.AddRange(CreateCoverage(type));

            foreach (object inner in type.Items)
            {
                if (inner is dotcover.Type)
                    objects.Add(CreateClass((dotcover.Type)inner));
            }

            result.Items = objects.ToArray();

            List<emma.method> methods = new List<emma.method>();
            ForEachMethod(type, true, entry =>
            {
                emma.method m = new emma.method();

                m.name = entry.Name;
                m.coverage = CreateCoverate(entry);

                methods.Add(m);
            });

            result.method = methods.ToArray();

			return result;
		}

        private static emma.coverage[] CreateCoverage(dotcover.Root dc)
		{
			DotCoverage cls = new DotCoverage();
			ForEachClass(dc, cls.CountElements);

			DotCoverage method = new DotCoverage();
			ForEachMethod(dc, method.CountElements);

            DotCoverage cov = new DotCoverage();
            cov.Add(int.Parse(dc.CoveredStatements), int.Parse(dc.TotalStatements));

			return Utils.CreateCoverage(cls, method, cov, cov);
		}

        private static emma.coverage[] CreateCoverage(dotcover.Assembly assembly)
        {
            DotCoverage cls = new DotCoverage();
            ForEachClass(assembly, cls.CountElements);

            DotCoverage method = new DotCoverage();
            ForEachMethod(assembly, method.CountElements);

            DotCoverage cov = new DotCoverage();
            cov.Add(int.Parse(assembly.CoveredStatements), int.Parse(assembly.TotalStatements));

            return Utils.CreateCoverage(cls, method, cov, cov);
        }

        private static emma.coverage[] CreateCoverage(dotcover.Namespace ns)
        {
            DotCoverage cls = new DotCoverage();
            ForEachClass(ns, cls.CountElements);

            DotCoverage method = new DotCoverage();
            ForEachMethod(ns, method.CountElements);

            DotCoverage cov = new DotCoverage();
            cov.Add(int.Parse(ns.CoveredStatements), int.Parse(ns.TotalStatements));

            return Utils.CreateCoverage(cls, method, cov, cov);
        }

        private static emma.coverage[] CreateCoverage(dotcover.Type type)
		{
			DotCoverage cls = new DotCoverage();
			ForEachClass(type, cls.CountElements);

			DotCoverage method = new DotCoverage();
			ForEachMethod(type, method.CountElements);

            DotCoverage cov = new DotCoverage();
            cov.Add(int.Parse(type.CoveredStatements), int.Parse(type.TotalStatements));

			return Utils.CreateCoverage(cls, method, cov, cov);
		}

        private static emma.coverage[] CreateCoverate(Entry member)
        {
            DotCoverage method = new DotCoverage();
            method.CountElements(member);

            DotCoverage cov = new DotCoverage();
            cov.Add(member.CoveredStatements, member.TotalStatements);

            return Utils.CreateCoverage(method, cov, cov);
        }

		private static emma.stats CreateStats(dotcover.Root dc)
		{
			emma.stats ret = new emma.stats();

			int[] count = {0};
			ForEachPackage(dc, _ => count[0]++);
			ret.packages = new emma.packages();
			ret.packages.value = count[0].ToString();

			count[0] = 0;
			ForEachClass(dc, _ => count[0]++);
			ret.classes = new emma.classes();
			ret.classes.value = count[0].ToString();

			count[0] = 0;
			ForEachMethod(dc, _ => count[0]++);
			ret.methods = new emma.methods();
			ret.methods.value = count[0].ToString();

			ret.srcfiles = new emma.srcfiles();
			ret.srcfiles.value = ret.classes.value;

			ret.srclines = new emma.srclines();
			ret.srclines.value = dc.TotalStatements;

			return ret;
		}

		private class DotCoverage : Coverage
		{
			public void CountElements(dotcover.Type type)
			{
				Add(int.Parse(type.CoveredStatements) > 0 ? 1 : 0, 1);
			}

			public void CountElements(Entry member)
			{
				Add(member.CoveredStatements > 0 ? 1 : 0, 1);
			}
		}

        private class Entry
        {
            public string Name;
            public int CoveredStatements;
            public int TotalStatements;

            public Entry(string name, string covered, string total)
            {
                Name = name;
                CoveredStatements = int.Parse(covered);
                TotalStatements = int.Parse(total);
            }
        }

		private static void ForEachPackage(dotcover.Root dc, Action<dotcover.Assembly> callback)
		{
			foreach (dotcover.Assembly assembly in dc.Assembly)
				callback(assembly);
		}

		private static void ForEachClass(dotcover.Root dc, Action<dotcover.Type> callback)
		{
			foreach (dotcover.Assembly assembly in dc.Assembly)
				ForEachClass(assembly, callback);
		}

		private static void ForEachClass(dotcover.Assembly assembly, Action<dotcover.Type> callback)
		{
            if (assembly.Namespace != null)
                foreach (var ns in assembly.Namespace)
                    ForEachClass(ns, callback);

            if (assembly.Type != null)
                foreach (var type in assembly.Type)
                    ForEachClass(type, callback);
		}

        private static void ForEachClass(dotcover.Namespace ns, Action<dotcover.Type> callback)
        {
            foreach (var type in ns.Type)
                ForEachClass(type, callback);
        }

		private static void ForEachClass(dotcover.Type type, Action<dotcover.Type> callback)
		{
			callback(type);

            foreach (object inner in type.Items)
            {
                if (inner is dotcover.Type)
                    ForEachClass((dotcover.Type) inner, callback);
            }
		}

        private static void ForEachMethod(dotcover.Root dc, Action<Entry> callback)
		{
			foreach (dotcover.Assembly assembly in dc.Assembly)
				ForEachMethod(assembly, callback);
		}

        private static void ForEachMethod(dotcover.Assembly assembly, Action<Entry> callback)
        {
            if (assembly.Namespace != null)
                foreach (var ns in assembly.Namespace)
                    ForEachMethod(ns, callback);

            if (assembly.Type != null)
                foreach (var type in assembly.Type)
                    ForEachMethod(type, callback);
        }

        private static void ForEachMethod(dotcover.Namespace ns, Action<Entry> callback)
        {
            foreach (var type in ns.Type)
                ForEachMethod(type, callback);
        }

        private static void ForEachMethod(dotcover.Type type, Action<Entry> callback)
        {
            ForEachMethod(type, false, callback);
        }

		private static void ForEachMethod(dotcover.Type type, Boolean skipInnerTypes, Action<Entry> callback)
		{
            foreach (object inner in type.Items)
            {
                if (inner is dotcover.Type)
                {
                    if (!skipInnerTypes)
                        ForEachMethod((dotcover.Type)inner, false, callback);
                }
                else if (inner is dotcover.Constructor)
                    ForEachMethod((dotcover.Constructor)inner, callback);

                else if (inner is dotcover.Event)
                    ForEachMethod((dotcover.Event)inner, callback);

                else if (inner is dotcover.Member)
                    ForEachMethod((dotcover.Member)inner, callback);

                else if (inner is dotcover.Method)
                    ForEachMethod((dotcover.Method)inner, callback);

                else if (inner is dotcover.Property)
                    ForEachMethod((dotcover.Property)inner, callback);
            }
		}

        private static void ForEachMethod(dotcover.Constructor constructor, Action<Entry> callback)
        {
            if (constructor.Items == null)
                callback(ToEntry(constructor));
            else
                ForEachMethodWithAnonymous(constructor.Items, callback, constructor.Name);
        }

        private static void ForEachMethod(dotcover.Event evt, Action<Entry> callback)
        {
            foreach (dotcover.Method method in evt.Method)
                ForEachMethod(method, callback, evt.Name);
        }

        private static void ForEachMethod(dotcover.Member member, Action<Entry> callback)
        {
            callback(ToEntry(member));
        }

        private static void ForEachMethod(dotcover.Method method, Action<Entry> callback, String parent = null)
        {
            if (method.Items == null)
                callback(ToEntry(method, parent));
            else
                ForEachMethodWithAnonymous(method.Items, callback, JoinName(parent, method.Name));
        }

        private static void ForEachMethod(dotcover.Property property, Action<Entry> callback)
        {
            foreach (dotcover.Method method in property.Method)
                ForEachMethod(method, callback, property.Name);
        }

        private static void ForEachMethod(dotcover.OwnCoverage ownCoverage, Action<Entry> callback, String parent)
        {
            callback(ToEntry(ownCoverage, parent));
        }

        private static void ForEachMethod(dotcover.AnonymousMethod method, Action<Entry> callback, String parent)
        {
            if (method.Items == null)
                callback(ToEntry(method, parent));
            else
                ForEachMethodWithAnonymous(method.Items, callback, JoinName(parent, method.Name));
        }

        private static void ForEachMethodWithAnonymous(object[] items, Action<Entry> callback, String parent)
        {
            foreach (object item in items)
            {
                if (item is dotcover.OwnCoverage)
                    ForEachMethod((dotcover.OwnCoverage)item, callback, parent);

                else if (item is dotcover.AnonymousMethod)
                    ForEachMethod((dotcover.AnonymousMethod)item, callback, parent);
            }
        }

        private static string JoinName(string parent, string name)
        {
            if (parent == null)
                return name;
            else if (name == null)
                return parent;
            else
                return parent + "  " + name; 
        }

        private static Entry ToEntry(object entry, String parent = null)
        {
            if (entry == null)
                return null;

            string name;
            var nameProp = entry.GetType().GetProperty("Name");
            if (nameProp != null)
                name = (string)nameProp.GetValue(entry, null);
            else
                name = null;

            string covered = (string) entry.GetType().GetProperty("CoveredStatements").GetValue(entry, null);
            string total = (string) entry.GetType().GetProperty("TotalStatements").GetValue(entry, null);

            if (covered == null || total == null)
                return null;

            return new Entry(JoinName(parent, name), covered, total);
        }
	}
}
