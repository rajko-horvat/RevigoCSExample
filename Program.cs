using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IRB.Revigo;
using IRB.Revigo.Core;
using IRB.Revigo.Databases;
using IRB.Revigo.Worker;
using static System.Net.WebRequestMethods;

namespace RevigoCSExample
{

	/// <summary>
	/// An example of using a REVIGO core library for your own projects.
	/// To Run this example you need RevigoCore library and a 
	/// set of database files available at: http://revigo.irb.hr/RevigoDatabases.zip
	/// 
	/// Authors:
	///		Rajko Horvat (rhorvat at irb.hr)
	///	
	/// License:
	/// 	MIT License
	///		Copyright(c) 2011-2023 Ruđer Bošković Institute
	///		
	///		Permission is hereby granted, free of charge, to any person obtaining a copy
	///		of this software and associated documentation files (the "Software"), to deal
	///		in the Software without restriction, including without limitation the rights
	///		to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	///		copies of the Software, and to permit persons to whom the Software is
	///		furnished to do so, subject to the following conditions:
	///
	///		The above copyright notice and this permission notice shall be included in all
	///		copies or substantial portions of the Software.
	///
	///		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	///		IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	///		FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	///		AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	///		LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	///		OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	///		SOFTWARE.
	/// </summary>
	internal class Program
	{
		static void Main(string[] args)
		{
			double dCutoff = 0.7;
			ValueTypeEnum eValueType = ValueTypeEnum.PValue;
			int iSpeciesTaxon = 0;
			SemanticSimilarityScoreEnum eMeasure = SemanticSimilarityScoreEnum.SIMREL;
			bool bRemoveObsolete = true;
			Console.WriteLine("Loading Ontology");
			GeneOntology oOntology = new GeneOntology("go.obo");
			Console.WriteLine("Loading Species Annotations");
			SpeciesAnnotationsList oAnnotations = SpeciesAnnotationsList.Deserialize("SpeciesAnnotations.xml");
			string sExample1 = null;
			string sExample2 = null;
			string sExample3 = null;

			// read Example 1 from the file
			using (StreamReader oReader = new StreamReader("Example1.csv"))
			{
				sExample1 = oReader.ReadToEnd();
			}

			// read Example 2 from the file
			using (StreamReader oReader = new StreamReader("Example2.csv"))
			{
				sExample2 = oReader.ReadToEnd();
			}

			// read Example 3 from the file
			using (StreamReader oReader = new StreamReader("Example3.csv"))
			{
				sExample3 = oReader.ReadToEnd();
			}

			// Create worker 1
			RevigoWorker oWorker1 = new RevigoWorker(
				// JobID
				1, 
				// Ontology
				oOntology, 
				// Annotations for a given dataset
				oAnnotations.GetByID(iSpeciesTaxon), 
				// Timeout in minutes
				20, 
				// Job source
				RequestSourceEnum.JubSubmitting,
				// Dataset
				sExample1, 
				// Job parameters
				dCutoff, eValueType, eMeasure, bRemoveObsolete);

			// Create worker 2
			RevigoWorker oWorker2 = new RevigoWorker(2, oOntology, oAnnotations.GetByID(9606), 20, RequestSourceEnum.JubSubmitting,
				sExample2, 0.9, eValueType, SemanticSimilarityScoreEnum.LIN, bRemoveObsolete);

			// Create worker 3
			RevigoWorker oWorker3 = new RevigoWorker(3, oOntology, oAnnotations.GetByID(iSpeciesTaxon), 20, RequestSourceEnum.JubSubmitting,
				sExample3, 0.4, eValueType, eMeasure, bRemoveObsolete);

			// Workers will notify when the are finished processing the data
			oWorker1.OnFinish += OWorker_OnFinish;
			oWorker2.OnFinish += OWorker_OnFinish;
			oWorker3.OnFinish += OWorker_OnFinish;

			// Start Workers and wait for their completion
			// They will automatically be assigned to different CPU core if available
			oWorker1.Start();
			oWorker2.Start();
			oWorker3.Start();

			while (!oWorker1.IsFinished || !oWorker2.IsFinished || !oWorker3.IsFinished)
			{
				Thread.Sleep(100);
			}

			Console.WriteLine("All Jobs have finished processing.");

			// export our results
			Console.WriteLine("Exporting data.");
			ExportTable(oOntology, oWorker1, oWorker1.BPVisualizer, "Example1_BPTable.tsv");
			ExportScatterplot(oOntology, oWorker2, oWorker2.CCVisualizer, "Example2_CCScatterplot.tsv");
			ExportTreeMap(oOntology, oWorker3, oWorker3.MFVisualizer, "Example3_MFTreeMap.tsv");
			ExportCytoscapeXGMML(oWorker1.BPVisualizer, "Example1_BPCytoscape.xgmml");
			ExportSimMat(oWorker1.BPVisualizer, "Example1_BPSimilarityMatrix.tsv");
			ExportWordClouds(oWorker1, "Example1_WordClouds.json");

			// We are finished
			Console.WriteLine("Press a key to exit");
			Console.ReadLine();
		}

		private static void ExportTable(GeneOntology ontology, RevigoWorker oWorker, TermListVisualizer visualizer, string fileName)
		{
			StreamWriter oWriter = new StreamWriter(fileName);

			GOTermList oTerms = new GOTermList(visualizer.Terms);
			oTerms.FindClustersAndSortByThem(ontology, oWorker.AllProperties, oWorker.CutOff);

			oWriter.Write("TermID\tName\tValue\t");
			for (int c = 1; c < oWorker.MinNumColsPerGoTerm; c++)
			{
				oWriter.Write("UserValue_{0}\t", c - 1);
			}
			oWriter.WriteLine("LogSize\tFrequency\tUniqueness\tDispensability\tRepresentative");

			// print the data
			for (int i = 0; i < oTerms.Count; i++)
			{
				GOTerm oTerm = oTerms[i];
				GOTermProperties oProperties = oWorker.AllProperties.GetValueByKey(oTerm.ID);

				oWriter.Write("\"{0}\"\t", oTerm.FormattedID);
				oWriter.Write("\"{0}\"\t", oTerm.Name);
				oWriter.Write("{0}\t", oProperties.Value.ToString(CultureInfo.InvariantCulture));

				for (int c = 1; c < oWorker.MinNumColsPerGoTerm; c++)
				{
					oWriter.Write("{0}\t", oProperties.UserValues[c - 1].ToString(CultureInfo.InvariantCulture));
				}

				oWriter.Write("{0}\t",
					oProperties.LogAnnotationSize.ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t",
					(oProperties.AnnotationFrequency * 100.0).ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t", oProperties.Uniqueness.ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t", oProperties.Dispensability.ToString(CultureInfo.InvariantCulture));

				if (oProperties.Representative > 0)
				{
					oWriter.Write("{0}", oProperties.Representative);
				}
				else
				{
					oWriter.Write("null");
				}

				oWriter.WriteLine();
			}
			oWriter.Flush();
			oWriter.Close();
		}

		private static void ExportScatterplot(GeneOntology ontology, RevigoWorker oWorker, TermListVisualizer visualizer, string fileName)
		{
			StreamWriter oWriter = new StreamWriter(fileName);

			GOTermList oTerms = new GOTermList(visualizer.Terms);
			oTerms.FindClustersAndSortByThem(ontology, oWorker.AllProperties, oWorker.CutOff);

			oWriter.WriteLine("TermID\tName\tValue\tLogSize\tFrequency\tUniqueness\tDispensability\tPC_0\tPC_1\tRepresentative");

			// print the data
			for (int i = 0; i < oTerms.Count; i++)
			{
				GOTerm oTerm = oTerms[i];
				GOTermProperties oProperties = oWorker.AllProperties.GetValueByKey(oTerm.ID);

				oWriter.Write("\"{0}\"\t", oTerm.FormattedID);
				oWriter.Write("\"{0}\"\t", oTerm.Name);
				oWriter.Write("{0}\t", oProperties.Value.ToString(CultureInfo.InvariantCulture));

				oWriter.Write("{0}\t",
					oProperties.LogAnnotationSize.ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t",
					(oProperties.AnnotationFrequency * 100.0).ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t", oProperties.Uniqueness.ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t", oProperties.Dispensability.ToString(CultureInfo.InvariantCulture));

				// 2D
				oWriter.Write("{0}\t", (oProperties.PC.Count > 0) ?
					oProperties.PC[0].ToString(CultureInfo.InvariantCulture) : "null");
				oWriter.Write("{0}\t", (oProperties.PC.Count > 1) ?
					oProperties.PC[1].ToString(CultureInfo.InvariantCulture) : "null");

				if (oProperties.Representative > 0)
				{
					oWriter.Write("{0}", oProperties.Representative);
				}
				else
				{
					oWriter.Write("null");
				}

				oWriter.WriteLine();
			}
			oWriter.Flush();
			oWriter.Close();
		}

		private static void ExportTreeMap(GeneOntology ontology, RevigoWorker oWorker, TermListVisualizer visualizer, string fileName)
		{
			StreamWriter oWriter = new StreamWriter(fileName);

			GOTermList terms = new GOTermList(visualizer.Terms);
			terms.FindClustersAndSortByThem(ontology, oWorker.AllProperties, 0.1);

			oWriter.WriteLine("# WARNING - This exported Revigo data is only useful for the specific purpose of constructing a TreeMap visualization.");
			oWriter.WriteLine("# Do not use this table as a general list of non-redundant GO categories, as it sets an extremely permissive ");
			oWriter.WriteLine("# threshold to detect redundancies (c=0.10) and fill the 'representative' column, while normally c>=0.4 is recommended.");
			oWriter.WriteLine("# To export a reduced-redundancy set of GO terms, go to the Scatterplot or Table tab, and export from there.");

			oWriter.Write("TermID\tName\tFrequency\tValue\t");
			for (int c = 1; c < oWorker.MinNumColsPerGoTerm; c++)
			{
				oWriter.Write("UserValue_{0}\t", c - 1);
			}
			oWriter.WriteLine("Uniqueness\tDispensability\tRepresentative");

			// print the data
			for (int i = 0; i < terms.Count; i++)
			{
				GOTerm curGOTerm = terms[i];
				GOTermProperties oProperties = oWorker.AllProperties.GetValueByKey(curGOTerm.ID);
				bool isTermEliminated = oProperties.Dispensability > oWorker.CutOff;
				if (isTermEliminated)
					continue; // will not output terms below the dispensability threshold at all

				oWriter.Write("\"{0}\"\t", curGOTerm.FormattedID);
				oWriter.Write("\"{0}\"\t", curGOTerm.Name);
				oWriter.Write("{0}\t", (oProperties.AnnotationFrequency * 100.0).ToString(CultureInfo.InvariantCulture));

				oWriter.Write("{0}\t", oProperties.Value.ToString(CultureInfo.InvariantCulture));

				for (int c = 1; c < oWorker.MinNumColsPerGoTerm; c++)
				{
					oWriter.Write("{0}\t", oProperties.UserValues[c - 1].ToString(CultureInfo.InvariantCulture));
				}

				oWriter.Write("{0}\t", oProperties.Uniqueness.ToString(CultureInfo.InvariantCulture));
				oWriter.Write("{0}\t", oProperties.Dispensability.ToString(CultureInfo.InvariantCulture));
				if (oProperties.Representative > 0)
				{
					oWriter.Write("\"{0}\"", ontology.GetValueByKey(oProperties.Representative).Name);
				}
				else
				{
					oWriter.Write("null");
				}
				oWriter.WriteLine();
			}
			oWriter.Flush();
			oWriter.Close();
		}

		private static void ExportCytoscapeXGMML(TermListVisualizer visualizer, string fileName)
		{
			StreamWriter oWriter = new StreamWriter(fileName);
			visualizer.SimpleOntologram.GraphToXGMML(oWriter);
			oWriter.Flush();
			oWriter.Close();
		}

		private static void ExportSimMat(TermListVisualizer visualizer, string fileName)
		{
			StreamWriter oWriter = new StreamWriter(fileName);

			for (int i = 0; i < visualizer.Terms.Length; i++)
			{
				oWriter.Write("\t{0}", visualizer.Terms[i].FormattedID);
			}
			oWriter.WriteLine();
			for (int i = 0; i < visualizer.Terms.Length; i++)
			{
				oWriter.Write(visualizer.Terms[i].FormattedID);
				for (int j = 0; j < visualizer.Terms.Length; j++)
				{
					oWriter.Write("\t{0}", visualizer.Matrix.Matrix[i, j].ToString(CultureInfo.InvariantCulture));
				}
				oWriter.WriteLine();
			}

			oWriter.Flush();
			oWriter.Close();
		}

		private static void ExportWordClouds(RevigoWorker oWorker, string fileName)
		{
			StreamWriter oWriter = new StreamWriter(fileName);

			oWriter.Write("{");
			if (oWorker.Enrichments != null)
			{
				oWriter.Write("\"Enrichments\":[");

				double MIN_UNIT_SIZE = 1;
				double MAX_UNIT_SIZE = 9;
				double RANGE_UNIT_SIZE = MAX_UNIT_SIZE - MIN_UNIT_SIZE;
				double minFreq = 999999.0;
				double maxFreq = 0.0;

				for (int i = 0; i < oWorker.Enrichments.Count; i++)
				{
					double dFrequency = Math.Sqrt(oWorker.Enrichments[i].Value);
					if (dFrequency > 0.0)
					{
						minFreq = Math.Min(minFreq, dFrequency);
						maxFreq = Math.Max(maxFreq, dFrequency);
					}
				}

				if (minFreq > maxFreq)
				{
					double dTemp = minFreq;
					minFreq = maxFreq;
					maxFreq = dTemp;
				}
				if (minFreq == maxFreq)
				{
					maxFreq++;
				}
				double range = maxFreq - minFreq;
				bool bFirst = true;

				for (int i = 0; i < oWorker.Enrichments.Count; i++)
				{
					string sWord = oWorker.Enrichments[i].Key.Replace("'", "");
					double dFrequency = Math.Sqrt(oWorker.Enrichments[i].Value);

					if (dFrequency > 0.0)
					{
						if (!bFirst)
							oWriter.Write(",");

						int size = (int)Math.Ceiling(MIN_UNIT_SIZE + Math.Round(((dFrequency - minFreq) * RANGE_UNIT_SIZE) / range));
						oWriter.Write("{{\"Word\":\"{0}\",\"Size\":{1}}}", Utilities.StringToJSON(sWord), size);
						bFirst = false;
					}
				}
				oWriter.Write("]");
			}

			if (oWorker.Correlations != null)
			{
				if (oWorker.Enrichments != null)
					oWriter.Write(",");

				oWriter.Write("\"Correlations\":[");

				double MIN_UNIT_SIZE = 1;
				double MAX_UNIT_SIZE = 9;
				double RANGE_UNIT_SIZE = MAX_UNIT_SIZE - MIN_UNIT_SIZE;
				double minFreq = 999999.0;
				double maxFreq = 0.0;
				for (int i = 0; i < oWorker.Correlations.Count; i++)
				{
					double dFrequency = oWorker.Correlations[i].Value;
					if (dFrequency > 0.0)
					{
						minFreq = Math.Min(minFreq, dFrequency);
						maxFreq = Math.Max(maxFreq, dFrequency);
					}
				}

				if (minFreq > maxFreq)
				{
					double dTemp = minFreq;
					minFreq = maxFreq;
					maxFreq = dTemp;
				}
				if (minFreq == maxFreq)
				{
					maxFreq++;
				}
				double range = maxFreq - minFreq;
				bool bFirst = true;

				for (int i = 0; i < oWorker.Correlations.Count; i++)
				{
					string sWord = oWorker.Correlations[i].Key.Replace("'", "");
					double dFrequency = oWorker.Correlations[i].Value;

					if (dFrequency > 0.0)
					{
						if (!bFirst)
							oWriter.Write(",");

						int size = (int)Math.Ceiling(MIN_UNIT_SIZE + Math.Round(((dFrequency - minFreq) * RANGE_UNIT_SIZE) / range));
						oWriter.Write("{{\"Word\":\"{0}\",\"Size\":{1}}}", Utilities.StringToJSON(sWord), size);
						bFirst = false;
					}
				}
				oWriter.Write("]");
			}
			oWriter.Write("}");

			oWriter.Flush();
		}

		private static void OWorker_OnFinish(object sender, EventArgs e)
		{
			RevigoWorker oWorker = sender as RevigoWorker;

			Console.WriteLine("Worker {0} has finished processing the data in {1} seconds.", oWorker.JobID, oWorker.ExecutingTime.TotalSeconds);
		}
	}
}
