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
using dotcover;

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
				dc.Assembly = new Assembly[0];

			ForEachClass(dc, type =>
			                 	{
			                 		if (type.Member == null)
			                 			type.Member = new dotcover.Member[0];

			                 		if (type.Type1 == null)
			                 			type.Type1 = new dotcover.Type[0];
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
				emma.package pkg = new emma.package();

				pkg.name = assembly.Name;
				pkg.coverage = CreateCoverage(assembly);

				List<emma.srcfile> files = new List<emma.srcfile>();

				List<dotcover.Type> types = GetTypes(assembly);

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

				pkgs.Add(pkg);
			}
			ret.package = pkgs.ToArray();

			return ret;
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

			if (type.Type1.Length > 0)
			{
				objects.AddRange(type.Type1.Select(CreateClass));
			}

			result.Items = objects.ToArray();

			List<emma.method> methods = new List<emma.method>();
			foreach (var method in type.Member)
			{
				emma.method m = new emma.method();

				m.name = method.Name;
				m.coverage = CreateCoverate(method);

				methods.Add(m);
			}
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
			ForEachMethod(dc, cov.AddElements);

			return Utils.CreateCoverage(cls, method, cov, cov);
		}

		private static emma.coverage[] CreateCoverage(dotcover.Assembly assembly)
		{
			DotCoverage cls = new DotCoverage();
			ForEachClass(assembly, cls.CountElements);

			DotCoverage method = new DotCoverage();
			ForEachMethod(assembly, method.CountElements);

			DotCoverage cov = new DotCoverage();
			ForEachMethod(assembly, cov.AddElements);

			return Utils.CreateCoverage(cls, method, cov, cov);
		}

		private static emma.coverage[] CreateCoverage(dotcover.Type type)
		{
			DotCoverage cls = new DotCoverage();
			ForEachClass(type, cls.CountElements);

			DotCoverage method = new DotCoverage();
			ForEachMethod(type, method.CountElements);

			DotCoverage cov = new DotCoverage();
			ForEachMethod(type, cov.AddElements);

			return Utils.CreateCoverage(cls, method, cov, cov);
		}

		private static emma.coverage[] CreateCoverate(dotcover.Member member)
		{
			DotCoverage method = new DotCoverage();
			ForEachMethod(member, method.CountElements);

			DotCoverage cov = new DotCoverage();
			ForEachMethod(member, cov.AddElements);

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

			public void CountElements(dotcover.Member member)
			{
				Add(int.Parse(member.CoveredStatements) > 0 ? 1 : 0, 1);
			}

			public void AddElements(dotcover.Member member)
			{
				Add(int.Parse(member.CoveredStatements), int.Parse(member.TotalStatements));
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
			List<dotcover.Type> types = GetTypes(assembly);

			types.ForEach(t => ForEachClass(t, callback));
		}

		private static void ForEachClass(dotcover.Type type, Action<dotcover.Type> callback)
		{
			callback(type);

			foreach (dotcover.Type inner in type.Type1)
				ForEachClass(inner, callback);
		}

		private static void ForEachMethod(dotcover.Root dc, Action<dotcover.Member> callback)
		{
			foreach (dotcover.Assembly assembly in dc.Assembly)
				ForEachMethod(assembly, callback);
		}

		private static void ForEachMethod(dotcover.Assembly assembly, Action<dotcover.Member> callback)
		{
			List<dotcover.Type> types = GetTypes(assembly);

			types.ForEach(t => ForEachMethod(t, callback));
		}

		private static void ForEachMethod(dotcover.Type type, Action<dotcover.Member> callback)
		{
			foreach (dotcover.Member member in type.Member)
				ForEachMethod(member, callback);

			foreach (dotcover.Type inner in type.Type1)
				ForEachMethod(inner, callback);
		}

		private static void ForEachMethod(dotcover.Member member, Action<dotcover.Member> callback)
		{
			callback(member);
		}
	}
}
