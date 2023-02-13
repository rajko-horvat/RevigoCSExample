## Repository description
<p>This is the example CS project that illustrates how to use a REVIGO core library for your own projects.</p>

## How to compile and run this example
<p>To run this example you need the compiled RevigoCore library and a set of precompiled databases:
	<a href="http://revigo.irb.hr/Databases/GeneOntology.xml.gz" target="_blank">Gene Ontology</a> and 
	<a href="http://revigo.irb.hr/Databases/SpeciesAnnotations.xml.gz" target="_blank">Species annotations</a>, 
	or build your own databases with <a href="https://github.com/rajko-horvat/RevigoGenerateDatabases">RevigoGenerateDatabases</a> command line utility.</p>

<p>To compile and use this example: 
<ul>
	<li>Optional: Install <a href="https://visualstudio.microsoft.com/">Visual Studio Code</a> or <a href="https://visualstudio.microsoft.com/">Visual Studio for Windows</a> (You can also compile from Visual Studio for Windows)</li>
	<li>Install .NET core 6.0 from Microsoft (<a href="https://dotnet.microsoft.com/download">Install .NET for Windows</a>, <a href="https://learn.microsoft.com/en-us/dotnet/core/install/linux">Install .NET for Linux</a>)</li>
	<li>git clone https://github.com/rajko-horvat/RevigoCore</li>
	<li>git clone https://github.com/rajko-horvat/RevigoCSExample</li>
	<li>dotnet build --configuration Release --os win-x64 RevigoCSExample.csproj (For Linux use --os linux. See <a href="https://learn.microsoft.com/en-us/dotnet/core/rid-catalog">list of OS RIDs</a> for --os option)</li>
	<li>Run generated binary file (under RevigoCSExample/bin/net6.0/) and enjoy.</li>
</ul></p>

## About REVIGO (REduce + VIsualize Gene Ontology) project
<p>Outcomes of high-throughput biological experiments are typically interpreted by statistical testing
for enriched gene functional categories defined by the Gene Ontology (GO). The resulting lists of GO terms 
may be large and highly redundant, and thus difficult to interpret.<p>
<p>REVIGO is a successful project to summarize long, unintelligible lists of Gene Ontology terms by finding a representative subset 
of the terms using a simple clustering algorithm that relies on semantic similarity measures.</p>
<p>For any further information about REVIGO project please see  
<a href="https://dx.doi.org/10.1371/journal.pone.0021800" target="_blank">published paper</a> and  
<a href="http://revigo.irb.hr/FAQ.aspx" target="_blank">Frequently Asked Questions page</a></p>
