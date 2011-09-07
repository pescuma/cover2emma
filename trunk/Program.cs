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
using System.IO;
using System.Xml.Serialization;

namespace cover2emma
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += NBug.Handler.UnobservedTaskException;
			NBug.Settings.CustomInfo.Add("Command line: " + string.Join(" ", args));

			if (args.Length != 3)
			{
				Console.WriteLine("Use:");
				Console.WriteLine("    cover2emma -<input type> <input.xml> <emma.xml>");
				Console.WriteLine("Where <input type> can be dotcover or bullseye");

				return 1;
			}

			var inputType = args[0];
			var inputFilename = args[1];
			var outputFilename = args[2];

			NBug.Settings.AdditionalReportFiles.Add(inputFilename);
			NBug.Settings.AdditionalReportFiles.Add(outputFilename);

			emma.report result;

			if (String.Compare(inputType, "-dotcover", true) == 0)
				result = new DotCoverToEmma().ToEmma(inputFilename);

			else if (String.Compare(inputType, "-bullseye", true) == 0)
				result = new BullseyeToEmma().ToEmma(inputFilename);

			else
				throw new ArgumentException();

			WriteEmma(result, outputFilename);

			return 0;
		}

		private static void WriteEmma(emma.report result, string filename)
		{
			TextWriter writter = null;
			try
			{
				writter = new StreamWriter(filename);

				XmlSerializer serializer = new XmlSerializer(typeof (emma.report));
				serializer.Serialize(writter, result);
			}
			finally
			{
				if (writter != null)
					writter.Close();
			}
		}
	}
}
