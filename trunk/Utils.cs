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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace cover2emma
{
	public class Utils
	{
		private static Regex values = new Regex(@"\(([0-9]+)/([0-9]+)\)");

		// Helper class to ignore namespaces when de-serializing
		// from http://stackoverflow.com/questions/870293/can-i-make-xmlserializer-ignore-the-namespace-on-deserialization/873281#873281
		private class NamespaceIgnorantXmlTextReader : XmlTextReader
		{
			public NamespaceIgnorantXmlTextReader(TextReader reader)
				: base(reader)
			{
			}

			public override string NamespaceURI
			{
				get { return ""; }
			}
		}

		public static T ReadXML<T>(string filename)
		{
			TextReader textReader = null;
			try
			{
				textReader = new StreamReader(filename);

				XmlSerializer deserializer = new XmlSerializer(typeof (T));
				return (T) deserializer.Deserialize(new NamespaceIgnorantXmlTextReader(textReader));
			}
			finally
			{
				if (textReader != null)
					textReader.Close();
			}
		}

		public static string CoverageString(int covered, int total)
		{
			string ret;
			if (total <= 0)
				ret = "0";
			else
				ret = ((int) Math.Round((covered / (double) total) * 100)).ToString();
			ret += "%";
			for (int i = ret.Length; i < 5; ++i)
				ret += " ";
			ret += "(" + covered + "/" + total + ")";
			return ret;
		}

		public static emma.report CreateReport(IEnumerable<emma.package> packages)
		{
			emma.report ret = new emma.report();

			ret.data = new emma.data();
			ret.data.all = new emma.all();
			ret.data.all.name = "all files";
			ret.data.all.coverage = MergeCoverate(packages);
			ret.data.all.package = packages.ToArray();

			ret.stats = new emma.stats();
			ret.stats.packages = new emma.packages();
			ret.stats.packages.value = packages.Count().ToString();
			ret.stats.classes = new emma.classes();
			ret.stats.classes.value = CountClasses(ret.data.all.coverage).ToString();
			ret.stats.methods = new emma.methods();
			ret.stats.methods.value = CountMethods(ret.data.all.coverage).ToString();
			ret.stats.srcfiles = new emma.srcfiles();
			ret.stats.srcfiles.value = CountSrcFiles(packages).ToString();
			ret.stats.srclines = new emma.srclines();
			ret.stats.srclines.value = CountSrcLines(ret.data.all.coverage).ToString();

			return ret;
		}

		private static int CountSrcLines(emma.coverage[] all)
		{
			var covs = ParseCoverages(all);
			return covs.LineTotal;
		}

		private static int CountSrcFiles(IEnumerable<emma.package> packages)
		{
			int result = 0;
			ForEachSrcFile(packages, m => ++result);
			return result;
		}

		private static int CountMethods(emma.coverage[] all)
		{
			var covs = ParseCoverages(all);
			return covs.MethodTotal;
		}

		private static int CountClasses(emma.coverage[] all)
		{
			var covs = ParseCoverages(all);
			return covs.ClassTotal;
		}

		public static void ForEachSrcFile(IEnumerable<emma.package> packages, Action<emma.srcfile> callback)
		{
			foreach (var package in packages)
				ForEachSrcFile(package, callback);
		}

		public static void ForEachSrcFile(emma.package packages, Action<emma.srcfile> callback)
		{
			foreach (var src in packages.srcfile)
				ForEachSrcFile(src, callback);
		}

		public static void ForEachSrcFile(emma.srcfile src, Action<emma.srcfile> callback)
		{
			callback(src);
		}

		public static void ForEachClass(IEnumerable<emma.package> packages, Action<emma.@class> callback)
		{
			foreach (var package in packages)
				ForEachClass(package, callback);
		}

		public static void ForEachClass(emma.package packages, Action<emma.@class> callback)
		{
			foreach (var src in packages.srcfile)
				ForEachClass(src, callback);
		}

		public static void ForEachClass(emma.srcfile src, Action<emma.@class> callback)
		{
			foreach (var cls in src.@class)
				ForEachClass(cls, callback);
		}

		public static void ForEachClass(emma.@class cls, Action<emma.@class> callback)
		{
			callback(cls);

			if (cls.Items == null)
				return;

			foreach (var item in cls.Items)
			{
				if (item is emma.@class)
					ForEachClass((emma.@class) item, callback);
			}
		}

		public static void ForEachMethod(IEnumerable<emma.package> packages, Action<emma.method> callback)
		{
			foreach (var package in packages)
				ForEachMethod(package, callback);
		}

		public static void ForEachMethod(emma.package packages, Action<emma.method> callback)
		{
			foreach (var src in packages.srcfile)
				ForEachMethod(src, callback);
		}

		public static void ForEachMethod(emma.srcfile src, Action<emma.method> callback)
		{
			foreach (var cls in src.@class)
				ForEachMethod(cls, callback);
		}

		public static void ForEachMethod(emma.@class cls, Action<emma.method> callback)
		{
			foreach (var method in cls.method)
				ForEachMethod(method, callback);

			if (cls.Items == null)
				return;

			foreach (var item in cls.Items)
			{
				if (item is emma.@class)
					ForEachMethod((emma.@class) item, callback);
			}
		}

		public static void ForEachMethod(emma.method method, Action<emma.method> callback)
		{
			callback(method);
		}

		private static emma.coverage[] MergeCoverate(IEnumerable<emma.package> packages)
		{
			Coverage cls = new Coverage();
			Coverage method = new Coverage();
			Coverage block = new Coverage();
			Coverage line = new Coverage();

			foreach (var package in packages)
			{
				var covs = ParseCoverages(package.coverage);
				cls.Add(covs.ClassCovered, covs.ClassTotal);
				method.Add(covs.MethodCovered, covs.MethodTotal);
				block.Add(covs.BlockCovered, covs.BlockTotal);
				line.Add(covs.LineCovered, covs.LineTotal);
			}

			return CreateCoverage(cls, method, block, line);
		}

		public static Coverages ParseCoverages(emma.coverage[] coverages)
		{
			Coverages result = new Coverages();

			foreach (var coverage in coverages)
			{
				var match = values.Match(coverage.value);
				if (!match.Success)
					throw new InvalidOperationException();

				var covered = int.Parse(match.Groups[1].Value);
				var total = int.Parse(match.Groups[2].Value);

				if (coverage.type == "class, %")
				{
					result.ClassCovered = covered;
					result.ClassTotal = total;
				}
				else if (coverage.type == "method, %")
				{
					result.MethodCovered = covered;
					result.MethodTotal = total;
				}
				else if (coverage.type == "block, %")
				{
					result.BlockCovered = covered;
					result.BlockTotal = total;
				}
				else if (coverage.type == "line, %")
				{
					result.LineCovered = covered;
					result.LineTotal = total;
				}
				else
					throw new InvalidOperationException();
			}
			return result;
		}

		public class Coverages
		{
			public int ClassCovered;
			public int ClassTotal;
			public int MethodCovered;
			public int MethodTotal;
			public int BlockCovered;
			public int BlockTotal;
			public int LineCovered;
			public int LineTotal;
		}

		public static emma.coverage[] CreateCoverage(string classCovered, string classTotal, string methodCovered,
		                                             string methodTotal, string blockCovered, string blockTotal,
		                                             string lineCovered, string lineTotal)
		{
			return CreateCoverage(int.Parse(classCovered), int.Parse(classTotal), int.Parse(methodCovered),
			                      int.Parse(methodTotal), int.Parse(blockCovered), int.Parse(blockTotal), int.Parse(lineCovered),
			                      int.Parse(lineTotal));
		}

		public static emma.coverage[] CreateCoverage(Coverage @class, Coverage method, Coverage block, Coverage line)
		{
			return CreateCoverage(@class.Covered, @class.Total, method.Covered, method.Total, block.Covered, block.Total,
			                      line.Covered, line.Total);
		}

		public static emma.coverage[] CreateCoverage(int classCovered, int classTotal, int methodCovered, int methodTotal,
		                                             int blockCovered, int blockTotal, int lineCovered, int lineTotal)
		{
			emma.coverage[] ret = new emma.coverage[4];

			ret[0] = CreateCoverage("class", classCovered, classTotal);
			ret[1] = CreateCoverage("method", methodCovered, methodTotal);
			ret[2] = CreateCoverage("block", blockCovered, blockTotal);
			ret[3] = CreateCoverage("line", lineCovered, lineTotal);

			return ret;
		}

		public static emma.coverage[] CreateCoverage(string methodCovered, string methodTotal, string blockCovered,
		                                             string blockTotal, string lineCovered, string lineTotal)
		{
			return CreateCoverage(int.Parse(methodCovered), int.Parse(methodTotal), int.Parse(blockCovered),
			                      int.Parse(blockTotal), int.Parse(lineCovered), int.Parse(lineTotal));
		}

		public static emma.coverage[] CreateCoverage(Coverage method, Coverage block, Coverage line)
		{
			return CreateCoverage(method.Covered, method.Total, block.Covered, block.Total, line.Covered, line.Total);
		}

		public static emma.coverage[] CreateCoverage(int methodCovered, int methodTotal, int blockCovered, int blockTotal,
		                                             int lineCovered, int lineTotal)
		{
			emma.coverage[] ret = new emma.coverage[3];

			ret[0] = CreateCoverage("method", methodCovered, methodTotal);
			ret[1] = CreateCoverage("block", blockCovered, blockTotal);
			ret[2] = CreateCoverage("line", lineCovered, lineTotal);

			return ret;
		}

		public static emma.coverage CreateCoverage(string name, Coverage coverage)
		{
			return CreateCoverage(name, coverage.Covered, coverage.Total);
		}

		public static emma.coverage CreateCoverage(string name, string covered, string total)
		{
			return CreateCoverage(name, int.Parse(covered), int.Parse(total));
		}

		public static emma.coverage CreateCoverage(string name, int covered, int total)
		{
			emma.coverage ret = new emma.coverage();
			ret.type = name + ", %";
			ret.value = Utils.CoverageString(covered, total);
			return ret;
		}
	}

	public class Coverage
	{
		public int Covered;
		public int Total;

		public void Add(string covered, string total)
		{
			Add(int.Parse(covered), int.Parse(total));
		}

		public void Add(int covered, int total)
		{
			Total += total;
			Covered += covered;
		}

		public override string ToString()
		{
			return Utils.CoverageString(Covered, Total);
		}
	}
}
