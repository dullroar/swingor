# swingor - Static Website INterpreter and GeneratOR

My own take at creating a static web site from Markdown files using parallel processing.
A replacement for my current approach of using Pandoc and GNU make. But in reality, this
is more than that. It is a way of invoking parallelized processes on arbitrary directories,
which each directory being able to have a series of processors that will be executed in
order on that directory. Note that internally each processor can also be parallelized if
it fits what is being done.

Right now, the processing is pretty simple - Markdown files get processed to HTML,
everything else is flat copied to the output location. But by having the pipeline processors
in collections all of the following is possible:

* A series of Markdown files gets processed to HTML, then a second processor creates a
  sitemap, a third creates the ATOM feed, etc.

* A series of pictures gets processed for uniform resizing (as an example), then copied
  to an output directory.

* CSS and Javascript files get minimized, merged or otherwise processed (Less, etc.),
  then copied to an output directory.

...and so on. Each directory type is processed in parallel with the other directories, and
within that each processor can also process the files in parallel, or not. The processors
for a given directory type are processed in order to make a sort of pipeline. So, for the
first example above, the Markdown processor already processes all Markdown files to HTML
in parallel (since they are all independent of each other), but the proposed sitemap and
ATOM processors that follow it would each process the files in order, because they are
writing to a single output file as the result of their processing. Meanwhile, the images,
CSS, scripts, static resources are also all getting processed in parallel with the Markdown
files, and with each other.

See the default processor implementations, `ProcessMarkdownFiles` and `ProcessStaticFiles`
for details.

Created by Jim Lehmer in C# using .NET Core. Written and tested on Linux, but should run
anywhere .NET Core is supported. Licensed under the BSD 2-Clause "simplified" license.

Uses the following non-Microsoft packages:

* [markdig](https://github.com/lunet-io/markdig) - converts Markdown to other formats.
  Really well done work.

## How to Use

1. Update the `swingor/appsettings.json` file to your liking. See below for details.

2. `dotnet run` in the `swingor` directory.

You can also run the test suite (which currently doesn't handle the processors themselves,
but instead all the utility methods).

1. Update the `test-swingor/appsettings.json` file to your liking. Check the tests first,
   though, because there are only parts of it being used for testing at this time.

2. `dotnet test` in the `test-swingor` directory.

## Markdown Support

The default Markdown processor supports pretty much everything `Markdig` does, plus a bit
more. The biggest changes are:

* "Prepends" and "Postpends" files, which are typically HTML snippets that get added to
  the beginning and end of the HTML generated from Markdown (although when processing images,
  say, they could be used to add a copyright overlay layer, or copyright comments to all scripts
  and CSS). I use them to add HTML metadata, copyright info, menus, footers, scripts and so on.

* Support for **simple** YAML front matter, which will then be used to replace anything in the output
  HTML that has a Moustaches-like syntax. So, if one of the properties in the YAML front
  matter is `title: This is a title`, then in the output HTML if there is embedded
  `{{title}}`, it will be replaced with `This is a title`. This is especially useful with
  the `Prepends` and `Postpends` files.

## Configuration

The code uses the .NET Core approach to configuration data, with the `appsettings.json`
file holding the main config data, which can be overridden by environment variables, both
of which can be overridden by the command line. Note that for the command line and
environment variables, you have to use the "section syntax" of .NET Core's configuration.
In other words, to specify both "clean" to delete the output directories before running
(the default is false) and "serve" to access the output locally via the Kestrel web server
for testing and QA (also not the default), you would specify the following on the command
line:

```bash
dotnet run AppConfiguration:Serve=true --AppConfiguration:Clean=true
```

If you look in the `appsettings.json` file, you will see that both the `Clean` and `Serve`
properties are children of a property called `AppConfiguration`, hence the above syntax.

**Note:** The parameters can be specified with no, a single or double dashes, as shown
above.

### Configuration Settings

* **AppConfiguration** - root of configuration data. See `AppConfiguration.cs`.

* **AppConfiguration:Clean** - Boolean to decide whether to delete the output directories
  before processing. Defaults to `false` (but currently set to `true` via the command line
  in the `launch.json` file).

* **AppConfiguration:Serve** - Boolean to decide whether to launch a Kestrel server at the
  end of processing to serve the generated files for testing and QA. Defaults to `false`
  (but currently set to `true` via the command line in the `launch.json` file).

* **AppConfiguration:DirectoriesToProcess** - array of objects declaring how to process
  each directory. See `DirectoryToProcess.cs`.

* **AppConfiguration:DirectoriesToProcess:DefaultAuthor** - optional string containing the
  default author to use (for things like replacing `{{author}}` in the HTML metadata).

* **AppConfiguration:DirectoriesToProcess:DefaultTitle** - optional string containing the
  default title to use (for things like replacing `{{title}}` in the HTML metadata).

* **AppConfiguration:DirectoriesToProcess:InputPath** - path to input directory.

* **AppConfiguration:DirectoriesToProcess:OutputPath** - path to output directory.

* **AppConfiguration:DirectoriesToProcess:Postpends** - an array of file names to be
  concatenated after the generated file, in order, after the rest of the contents. For Markdown,
  this is used to define things like the `<footer>` element, script blocks and the like.
  For other processors in the futue, it could be used for other things (like adding
  metadata to generated images).

* **AppConfiguration:DirectoriesToProcess:Prepends** - an array of file names to be
  concatenated before the generated file, in order. For Markdown, this is used to define things
  like contents of the `<head>` element. For other processors in the future, it could
  be used for other things (like prepending a copyright comment to every script file).

* **AppConfiguration:DirectoriesToProcess:Processors** - array of processors to invoke, in
  order, for a given input directory. See `Processor.cs`.

* **AppConfiguration:DirectoriesToProcess:Processors:Class** - class in assembly
  containing processor.

* **AppConfiguration:DirectoriesToProcess:Processors:DLL** - path to DLL of assembly
  containing processor.

* **AppConfiguration:DirectoriesToProcess:Processors:Exclusions** - array of processor-specific
  filename patterns to be used to exclude files from processing, e.g., for building a `sitemap.xml`
  file you may want to `"Exceptions" : [ "401.html", "403.html", "404.html", "500.html" ]`.

* **AppConfiguration:DirectoriesToProcess:Processors:Method** - the method that implements
  the processor ("Where the rubber meets the road"). The method signature should be
  `void ProcessorName(DirectoryToProcess d)`.

* **AppConfiguration:DirectoriesToProcess:Processors:StopAfter** - optional processor-specific
  integer of the number of files to process (in other words, stop **after** this many files are
  processed). Useful for things like RSS feeds.

* **AppConfiguration:DirectoriesToProcess:TargetURL** - optional string containing the target
  URL the files will be served from, e.g., `"TargetURL" : "https://foo.com"`.

* **AppConfiguration:DirectoriesToProcess:SiteDescription** - optional string to use for containing
  a site description (for RSS feeds, etc.)

* **AppConfiguration:DirectoriesToProcess:SiteTitle** - optional string to use for containing a site
  title (for RSS feeds, etc.)

* **AppConfiguration:DirectoriesToProcess:Wildcard** - array of wildcards to use to select
  files in the input directory to process, e.g., `"Wildcard": [ "*.gif", "*.jp*g", "*.png", "*.tif*" ]`

## TODO

* Break out the processors into their own assembly. It's a bit hokey having them all
  be a part of the static `Program.cs` class now (but it works!)

* Write processor to update image metadata (copyright, etc.) Could be tricky given the
  current state of graphics file processing in .NET Core.

* Move Prepends and Postpends to be processor-specific configuration settings.

## Known Bugs

None at this time.