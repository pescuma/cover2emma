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
using emma;

namespace cover2emma
{
	public class BullseyeToEmma
	{
		public emma.report ToEmma(string filename)
		{
			bullseye.BullseyeCoverage bc = Utils.ReadXML<bullseye.BullseyeCoverage>(filename);

			return ConvertToEmma(bc);
		}

		private emma.report ConvertToEmma(bullseye.BullseyeCoverage dc)
		{
			var folders = new Dictionary<string, emma.package>();

			// Create packages

			ForEachSrc(dc, (path, folder, src) =>
			               	{
			               		if (!folders.ContainsKey(path))
			               		{
			               			var package = new emma.package();
			               			package.name = ConvertPathToPackageName(path);
			               			package.coverage = CreateCoverage(folder);
			               			package.srcfile = new srcfile[0];

			               			folders.Add(path, package);
			               		}

			               		var files = new List<emma.srcfile>(folders[path].srcfile);
			               		files.Add(Convert(src));
			               		folders[path].srcfile = files.ToArray();
			               	});

			// Add classes to the package

			foreach (var package in folders.Values)
			{
				Coverage classesCov = new Coverage();
				foreach (var src in package.srcfile)
				{
					var covs = Utils.ParseCoverages(src.coverage);
					classesCov.Add(covs.ClassCovered, covs.ClassTotal);
				}

				var packageCovs = Utils.ParseCoverages(package.coverage);

				package.coverage = Utils.CreateCoverage(classesCov.Covered, classesCov.Total, packageCovs.MethodCovered,
				                                        packageCovs.MethodTotal, packageCovs.BlockCovered, packageCovs.BlockTotal,
				                                        packageCovs.LineCovered, packageCovs.LineTotal);
			}

			return Utils.CreateReport(folders.Values);
		}

		private string ConvertPathToPackageName(string path)
		{
			var pos = path.IndexOf(':');
			if (pos >= 0)
				path = "." + path.Substring(pos + 1);
			do
			{
				if (path.StartsWith(@"..\") || path.StartsWith("../"))
					path = path.Substring(3);

				else if (path.StartsWith(@".\") || path.StartsWith("./"))
					path = path.Substring(2);

				else
					break;
			} while (true);

			return path.Replace('.', '_').Replace('\\', '.').Replace('/', '.');
		}

		private emma.srcfile Convert(bullseye.src src)
		{
			var classes = new Dictionary<string, ClassBuilder>();

			ForEachFn(src, fn =>
			               	{
			               		string[] names = SplitClassFunctionName(fn.name);
			               		var className = names[0];

			               		if (!classes.ContainsKey(className))
			               			classes.Add(className, new ClassBuilder(className));

			               		classes[className].Add(names[1], fn);
			               	});

			emma.srcfile result = new emma.srcfile();
			result.name = src.name;
			result.@class = (from builder in classes.Values select builder.CreateClass()).ToArray();
			result.coverage = CreateCoverage(src, result.@class);

			return result;
		}

		private string[] SplitClassFunctionName(string name)
		{
			var pos = name.LastIndexOf('(');
			if (pos < 0)
				pos = 0;

			pos = name.LastIndexOf("::", pos);
			if (pos < 0)
				return new[] {"::", name};
			else
				return new[] {name.Substring(0, pos), name.Substring(pos + 2)};
		}

		private emma.coverage[] CreateCoverage(bullseye.folder folder)
		{
			return Utils.CreateCoverage("0", "0", folder.fn_cov, folder.fn_total, folder.cd_cov, folder.cd_total, "0", "0");
		}

		private static coverage[] CreateCoverage(bullseye.src src, emma.@class[] classes)
		{
			Coverage cov = new Coverage();
			foreach (var cls in classes)
			{
				var covs =
					Utils.ParseCoverages((from item in cls.Items where item is emma.coverage select (emma.coverage) item).ToArray());
				cov.Add(covs.ClassCovered, covs.ClassTotal);
			}

			return Utils.CreateCoverage(cov.Covered.ToString(), cov.Total.ToString(), src.fn_cov, src.fn_total, src.cd_cov,
			                            src.cd_total, "0", "0");
		}

		private void ForEachSrc(bullseye.BullseyeCoverage dc, Action<string, bullseye.folder, bullseye.src> callback)
		{
			if (dc.folder == null)
				return;

			foreach (var folder in dc.folder)
				ForEachSrc("", folder, callback);
		}

		private void ForEachSrc(string path, bullseye.folder folder, Action<string, bullseye.folder, bullseye.src> callback)
		{
			if (folder.Items == null)
				return;

			if (path.Length > 0)
				path += "\\";
			path += folder.name;

			foreach (var item in folder.Items)
			{
				if (item is bullseye.folder)
					ForEachSrc(path, (bullseye.folder) item, callback);
				else if (item is bullseye.src)
					callback(path, folder, (bullseye.src) item);
			}
		}

		private void ForEachFn(bullseye.src folder, Action<bullseye.fn> callback)
		{
			if (folder.fn == null)
				return;

			foreach (var fn in folder.fn)
				ForEachFn(fn, callback);
		}

		private void ForEachFn(bullseye.fn fn, Action<bullseye.fn> callback)
		{
			callback(fn);
		}

		private class ClassBuilder
		{
			private readonly string name;
			private readonly Coverage fn = new Coverage();
			private readonly Coverage cd = new Coverage();
			private readonly List<emma.method> methods = new List<emma.method>();

			public ClassBuilder(string name)
			{
				this.name = name;
			}

			public void Add(string methodName, bullseye.fn fn)
			{
				var method = new emma.method();
				method.name = methodName;
				method.coverage = Utils.CreateCoverage(fn.fn_cov, fn.fn_total, fn.cd_cov, fn.cd_total, "0", "0");
				methods.Add(method);

				this.fn.Add(fn.fn_cov, fn.fn_total);
				this.cd.Add(fn.cd_cov, fn.cd_total);
			}

			private emma.coverage[] ToCoverage()
			{
				var cov = new Coverage();
				cov.Add(cd.Covered > 0 ? 1 : 0, 1);
				return Utils.CreateCoverage(cov, fn, cd, new Coverage());
			}

			public emma.@class CreateClass()
			{
				emma.@class cls = new emma.@class();
				cls.name = name;
				cls.Items = ToCoverage();
				cls.method = methods.ToArray();
				return cls;
			}
		}
	}
}
