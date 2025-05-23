Temac – Text Manuscript Compiler

<!--
© Copyright 2022-2025 Magnus Levein.
This file is part of Temac, Text Manuscript Compiler.

Temac is free software: you can redistribute it and/or modify it under the
terms of the GNU General Public License as published by the Free Software
Foundation, either version 3 of the License, or (at your option) any later
version.

Temac is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU General Public License for more
details.

You should have received a copy of the GNU General Public License along
with Temac. If not, see <https://www.gnu.org/licenses/>.
-->

# Temac Reference Manual

> Temac is a powerful tool for generating many similar text files (e.g., HTML pages) based on a shared
> template but with varying content, or for compiling a large file from smaller components. Use cases
> range from cookbook pages to help files.

- [Command Syntax](#command-syntax)
  - [Options](#options)
  - [Return Values](#return-values)
- [Temac Scripting Languages](#temac-scripting-languages)
  - [Comments](#comments)
  - [Variables, Macros, and String Constants](#variables-macros-and-string-constants)
  - [Counting, Comparing, and Looping](#counting-comparing-and-looping)
  - [File System Access](#accessing-the-file-system)
  - [Special Commands](#special-commands)
- [Temac Project Files](#temac-project-files)
- [Troubleshooting](#troubleshooting)


## Command Syntax

<pre>
temac   <em>[compilerOptions ...]</em>   <em>inputFile</em>  <em>[outputPattern]</em>

temac   <em>[projectOptions  ...]</em>   <em>projectFile</em>.temacproj

temac   -? | -h | -help
</pre>

`inputFile` – A Temac script file to compile.

`outputPattern` – A pattern for construction of output filenames. Use `@` to represent the
base name. Defaults to `@.html`. Temac only permits writing to filenames that match this pattern.

`projectFile` – A Temac project file (`.temacproj`) to process. This can be useful in
environments where OS-level batch files are restricted.
See [Temac Project Files](#Temac-project-files).

### Options
If *compiler options* are specified when processing a `.temacproj` file,
they are automatically inherited by all underlying compilation commands.

#### `-?`, `-h`, `-help`
Display command syntax and available options.

#### `-dn`, `-debug-newlines`
Output end-of-line token data in compiled files.
***Not for production use.***

#### `-f`, `-files`
Print a summary of file usage to stdout.

#### `-p <text>`, `-parameter <text>` 
Assign the specified text to the `$parameter` variable.

#### `-s`, `-stop`
Stop execution on the first error. Prints a stack trace to stderr and a variable dump to stdout.

#### `-t`, `-tokens`
Dump tokenization of read files to stdout.

#### `-v`, `-verbose`
Enable verbose output when processing a Temac project file.

#### `-w <limit>`, `-whilemax <limit>`
Set the maximum number of iterations for `while` loops. If not specified, the default limit is 1000.

### Return Values
 * `0` – Success
 * `1` – Warning or important message
 * `2` – Compilation error
 * `3` – Internal error
 * `4` – Argument error



## Temac Scripting Languages

Temac uses a *dual language approach*. The business logic (or "motor scripts") is written in
**Temac syntax**, while definition files or "manuscripts" use a much simpler **definition syntax**.
The main input file to the Temac compiler must always use Temac syntax. (To read a file in
definition syntax, use the `<:include-defs2html "...">` command, described later.)

Temac syntax is based on derivations of the  `<:>` and `<.>` expressions—sequences that are
very unlikely to appear in standard HTML, Javascript, or CSS. It is described in detail below.

The definition syntax consists of only three expressions:
 * **`#`** starts a comment line, but only if it’s the first non-blank character on that line.
 
 * <code><strong>[</strong><em>variable</em><strong>]</strong></code> (when it is not preceded
   by any other text on that line) marks the start of a block of text to be assigned to *variable*.
   The block continues until the next `[...]` statement or the end of the file.

 * <code><strong>{</strong><em>variable</em><strong>}</strong></code> or
   <code><strong>{</strong><em>variable parameters ...</em><strong>}</strong></code> invokes a
   variable (either a data variable or a macro variable). This expression retains its meaning in
   any position in the text.

More details about these three expressions are given below, along with their equivalents in Temac
syntax.


### Comments

In Temac syntax, a comment starts with `<:>` and continues until the end of the line:
<pre>
<strong><:></strong> <em>write your comment here</em>
Hello, World!<strong><:></strong> <em>or here</em>
</pre>

In definition syntax, comments start with `#`, which must be the first non-blank character
on the line:
<pre>
<strong>#</strong> <em>write your comment here</em>
              <strong>#</strong> <em>or here</em>
</pre>


### Variables, Macros, and String Constants

Variables in Temac are typically global. However, as explained later, there is a sandbox
mechanism to limit their scope.

Variable names consist of letters, digits, or underscores (`_`). They must not begin with a digit,
but may begin with a dollar sign (`$`). Temac uses Unicode-aware character classification, so
non-English letters are permitted.

In *definition syntax*, the first character in a `{...}` expression can be *any character*
except control characters and spaces. If it’s not a valid character for variable names, it is
automatically transcoded to the form `_0000_`, where 0000 is a four-digit uppercase hexadecimal
Unicode codepoint. For example, variable `_002F_` can be invoked as `{/}`.

Variable names beginning with `$` are special. Avoid using such names for regular variables.

Temac defines the following special variables:

* **`$null`** – Always empty. Anything written to it disappears.

* **`$err`** – Writing to this variable triggers a compilation error and prints the content
  as a custom error message.

* **`$status`** – Used to display a status message during compilation.

* **`$YYYY`**, **`$MM`**, **`$DD`** – Hold the current year, month, and day.

* **`$FileIn`** – The name of the main input file.<br>
  **`$FileOut`** – The name of the main output file.<br>
  **`$FileName`** – The base filename, without path or extension.<br>
  *(These values are read-only but can be faked in a sandbox.)*

* **`$parameter`** – Can be set via the command line using the `-parameter` option.

* **`$blankline`** – Invoked for each blank line (paragraph break) in a definition file.
  Empty by default.

* **`$1`**, **`$2`**, **`$3`** ... – Refer to macro parameters.<br>
  **`$`** – Holds the total number of parameters passed.<br>
  *(These are local to the macro and not global.)*


#### Assigning Text to a Variable
Temac provides several ways to assign or append text to a variable:
<pre>
<strong><:</strong><em>variable</em><strong>=></strong> <em>assigns text until the end of the line</em>

<strong><:</strong><em>variable</em><strong>.=></strong> <em>appends text until the end of the line</em>

<strong><:</strong><em>variable</em><strong>=:></strong>
<em>assigns text until
the end of the block</em>
<strong><.></strong>

<strong><:</strong><em>variable</em><strong>.=:></strong>
<em>appends text until
the end of the block</em>
<strong><.></strong>
</pre>

If *variable* does not exist, it will be created. If it already exists, it will either be replaced
(variants `=>` and `=:>`) or appended to (variants `.=>` and `.=:>`). The `.=` syntax is inspired
by PHP and used to append content.

In definition syntax, there is only one form of assignment, which always replaces any existing
content:

<pre>
<strong>[</strong><em>variable</em><strong>]</strong>
<em>assigns text until
next assignment starts</em>

<strong>[</strong><em>next_variable</em><strong>]</strong>
<em>... or the file ends</em>
</pre>


#### Assigning Code to a Macro Variable
To assign unevaluated code to a macro variable, use:
<pre>
<strong><:</strong><em>variable</em><strong>:></strong>
<em>assigns code until
the end of the block</em>
<strong><.></strong>
</pre>

If *variable* does not exist, it will be created. If it already exists, it will be replaced.

Temac does not technically distinguish between data and macro variables. The reason for the
different assignment syntax is that code in a macro variable is not evaluated until the macro
is invoked, whereas code in a variable assignment (expressions with `=`) is evaluated immediately
during assignment.

Temac, in fact, does not make any real difference between data variables and macro
variables. The reason for different assignment syntax is, that the code in a macro
definition is not evaluated until the macro is invoked. Any code in a variable assignment
(variants with `=`) is, however, evaluated during the assignment process.


#### Invoking a Variable or Macro Variable
To insert the contents of a variable or invoke a macro, use:
<pre>
<strong><:=</strong><em>variable</em><strong>></strong>
<strong><:=</strong><em>variable parameters ...</em><strong>></strong>
</pre>

This command works for both data and macro variables.

If the *variable* is a macro (i.e., it contains unevaluated code), you can pass parameters to it.
Inside the macro, the special variables `$1`, `$2`, `$3`, etc., provide read-only access to
the passed arguments, and `$` holds the number of parameters.

This command is also available in definition syntax:

<pre>
<strong>{</strong><em>variable</em><strong>}</strong>
<strong>{</strong><em>variable parameters ...</em><strong>}</strong>
</pre>

As noted earlier, the first character of the variable name in a `{...}` expression may be
almost any character. If it is not allowed in regular variable names, it will be transcoded
to the form `_0000_` (a four-digit uppercase hexadecimal Unicode codepoint, surrounded by
underscores).


#### String Constants and Numbers
In most cases, string constants (`"..."`) can be used instead of varibles. A number can be
written without quotation marks, but is still stored as a string. Leading zeros are preserved.
Temac does not have a separate numeric data type.

<pre>
<:=<strong>"Hello, World!"</strong>>
<:include <strong>"definitions.temac"</strong>>
<:=myMacro <strong>007</strong>>
</pre>

To include a literal quotation mark inside a string, double it.


#### Reading Lines from a Data Variable

If a variable contains *data* (i.e. not unevaluated code), you can extract a specific line
using: 

<pre>
<strong><:=</strong><em>variable</em><strong>[</strong><em>line_number</em><strong>]></strong>
<:> Line numbers are 1-based.
</pre>

This is **not** a general bracket syntax. It is a **dedicated command** for retrieving a line
by number. Brackets (`[ ]`) do not work where a variable is expected.


 To count the number of lines in a variable:

<pre>
<strong><:count</strong> <em>variable</em><strong>></strong>
</pre>


#### Unwrapping Prefix and Suffix in Data Variables
Temac can unwrap a data variable by removing a specified prefix and/or suffix, effectively
revealing the content inside:
<pre>
<strong><:unwrap</strong> <em>variable</em> <em>prefix</em> <em>suffix</em><strong>></strong>
</pre>

##### Example:
<pre>
<:file=>index.html
<:name=:><:unwrap file "" ".html"><.>
<:ext=:><:unwrap file name ""><.>
Name: <:=name>  <:> will output Name: index
Ext: <:=ext>    <:> will output Ext: .html
</pre>


#### Sandboxing to Protect the Global Scope
Temac variables are global by default, with the exception of macro parameters (`$`, `$1`, `$2`, ...),
which are always local to the macro. To avoid side effects in shared variables, you can use a
**sandbox**:

* Inside a sandbox, write operations to variables are local and discarded once the block ends.

* Read operations inside a sandbox first check for a local variable. If none is found, the lookup
  proceeds step by step through outer sandbox scopes, until eventually reaching the global scope
  if necessary.

* You can define **pipes**, which are variables explicitly shared between the sandbox and the
  outside. These can be already existing variables, or new ones to be created.

* You can also set a simulated filename for the sandboxed block, which will set local values for
  `$FileIn`, `$FileOut` and `$FileName`. This is useful when a file is included but shall
  believe it was ran directly.

* Sandboxes can be nested.

##### Syntax:
<pre>
<strong><:sandbox</strong> <em>[filename]</em> <em>[</em><strong>%</strong><em>pipe1]</em> <em>[</em><strong>%</strong><em>pipe2]</em> ...<strong>></strong>
  <em>put your sandboxed
  code here</em>
<strong><.></strong>
</pre>

The `%` symbol before each pipe variable hints at “pipes through a wall”.

##### Example:
<pre>
<:a=>alpha
<:b=>beta
<:sandbox %b>
  "a is <:=a>"  <:> will output "a is alpha"
  <:a=>1
  <:b=>2
  "a is <:=a>"  <:> will output "a is 1"
  "b is <:=b>"  <:> will output "b is 2"
<.>
"a is <:=a>"    <:> will output "a is alpha"
"b is <:=b>"    <:> will output "b is 2"
</pre>


### Counting, Comparing, and Looping
#### Increment and Decrement Operators
Temac doesn’t support general arithmetic, but it can increment or decrement variables by one:
<pre>
<strong><:</strong><em>variable</em><strong>++></strong>
<strong><:</strong><em>variable</em><strong>--></strong>
</pre>
If the variable cannot be interpreted as an integer, it is assumed to start at 0. For example,
`++` will then set it to 1.

This operation consists of three internal steps:
1. Read the current value
2. Perform the increment or decrement
3. Store the new value

Because of this, the operation is allowed even on variables defined outside a sandbox.
However, the new value will be stored in a new local variable within the sandboxed environment,
as the following example demonstrates:

<pre>
<:i=>1
<:sandbox>
  <:i++>
  i = <:=i>     <:> will output i = 2
<.>
i = <:=i>       <:> will output i = 1
</pre>

#### Comparing with `if` – `else`
Temac supports conditional branching using `if` and `else`:
<pre>
<strong><:if</strong> <em>variable1 comparison variable2</em><strong>></strong>
  <em>code to run if the
  comparison is true</em>
<strong><:else></strong>
  <em>code to run if the
  comparison is false</em>
<strong><.if></strong>
</pre>

Or without `else`:
<pre>
<strong><:if</strong> <em>variable1 comparison variable2</em><strong>></strong>
  <em>code to run if the
  comparison is true</em>
<strong><.if></strong>
</pre>

Valid comparison operators: `==`, `!=`, `>=`, `<=`, `>`, `<`

If both variables can be interpreted as integers, the comparison is numeric. Otherwise,
it’s a string comparison.

Since all data in Temac is stored as strings, both operands are converted to numbers if possible.
For example, `"007" == "7"` is **true**, because both are interpreted as numeric  7.
If this behaviour is not desired, `switch–case` can be used instead.

#### Comparing with `switch` – `case` – `default`
The `switch` construct works as a number of `if` statements checked at once.
But the comparisons are always done as **string comparisons**, so `"007"` will not match `"7"`.

<pre>
<strong><:switch</strong> <em>variable_to_test</em><strong>></strong>
<strong><:case</strong> <em>test_value [test_value ...]</em><strong>></strong>
  <em>code to run
  if variable_to_test
  equals test_value</em>                     
<strong><:case</strong> <em>test_value [test_value ...]</em><strong>></strong>
  <em>code to run
  if variable_to_test
  equals test_value</em>                     
<strong><:default></strong>
  <em>code to run if none of 
  the cases matched</em>                     
<strong><.switch></strong>
</pre>

At least one `<:case ...>` is required. `<:default>` is optional.

Temac offers several extended features in its `case` handling:

* Case values can be either constants or variables

* Multiple values can be listed in a single `case`

* Duplicate values across `case` blocks are allowed – all matching blocks are executed in order

##### Example:
<pre>
<:January=>01
<:switch $MM>
  <:case January>
    First month of the year.
  <:case 01 02 03>
    First quarter of the year.
  <:case 01 02 03 04 05 06>
    First half of the year.
  <:default>
    Second half of the year.
<.switch>
</pre>



#### The `while` loop
A `while` loop repeats as long as a condition is true:
<pre>
<strong><:while</strong> <em>variable1 comparison variable2</em><strong>></strong>
  <em>code to repeat as long as
  the comparison is true</em>
<strong><.while></strong>
</pre>

The comparison rules are the same as for `if`([see above](#comparing-with-if-else)). 

Temac enforces a maximum number of iterations for `while` loops and stops with an error if
this limit is exceeded. By default, the limit is 1000. You can change it using the `-whilemax`
command-line option.

### File System Access

#### Directory Listing
To retrieve a list of files matching a specific pattern, use:
<pre>
<strong><:getfiles</strong> <em>pattern [directory]</em><strong>></strong>
</pre>

This command returns filenames that match the given *pattern* (e.g. `"*.txt"`), either
in the specified *directory* or in the current working directory if none is provided.

The filenames are listed one per line, and can be processed using
<code><:count <em>filelist</em>></code> and <code><:=<em>filelist</em>[<em>number</em>]></code> as
described earlier. (Remember that line numbers are 1-based.)

#### Inclusion of Files
To include content from external files, Temac provides the following commands:
<pre>
<strong><:include</strong> <em>filename</em><strong>></strong>
<strong><:include-text2html</strong> <em>filename</em><strong>></strong>
<strong><:include-defs2html</strong> <em>filename</em><strong>></strong>
<strong><:include-bin2base64</strong> <em>filename</em><strong>></strong>
</pre>

* `include`<br>
  Reads the file as-is and interprets it as Temac code. Use this to include Temac scripts or
  HTML.

* `include-text2html`<br>
  Converts the characters `<`, `>` and `&` into their HTML entity codes, making it safe
  to show plain text (such as source code) inside HTML.

* `include-defs2html`<br>
  Reads a “manuscript” text file with variable definitions written in *definition syntax*
  described earlier. It also escapes HTML-sensitive characters (i.e., `<`, `>`, `&`).

* `include-bin2base64`<br>
  Includes a binary file as Base64-encoded text. This is useful for creating Base64-encoded
  data URIs.

#### Directing the Output
To send part of the output to a different file, use the `output` command.
<pre>
<em>this goes to the default output file</em>
<strong><:output</strong> <em>filename</em><strong>></strong>
  <em>output from here
  will go to the
  chosen file</em>
<strong><.></strong>
<em>this goes to the default output file too</em>
</pre>

The specified *filename* must match the allowed output pattern (which can be set via command-line).

If Temac detects that it is about to create an almost empty output file (a file with only whitespace
from leading blanks, and maybe some line breaks), it ignores writing to it. If an older file already
exists with that name, a warning will be shown. This mechanism also applies to the default or main
output file.

Temac also prevents writing to any file it has already read, in order to avoid accidental
overwriting of input. This will result in an error.

### Special Commands

#### Context-Aware Strings

The `context-begin` and `context-end` commands allow you to insert **tentative output** – content
that may be automatically removed if it turns out to be unnecessary.

<pre>
<strong><:context-begin</strong> <em>text [name]</em><strong>></strong>
<strong><:context-end</strong> <em>text [name]</em><strong>></strong>
</pre>

Each of these commands writes the specified text to the output, but tentatively. If a `context-begin`
is immediately followed by a matching `context-end`, both are suppressed, and no output is produced.

The optional *name* acts as a label to ensure that only matching pairs interact with each other. This
reduces the risk of unintended removal in more complex templates.

This feature was designed specifically for generating HTML, where certain structural tags (like `<p>`)
may be needed in some cases but not others. Using context-aware strings helps avoid redundant or empty
markup.

##### Example:

<pre>
<:> === article.temac ===
<:header:>
<:context-end "&lt;/p>" "article">
&lt;h1><:=$1>&lt;/h1>
<:context-begin "&lt;p>" "article">
<.>
<:include-defs2html "article.txt">
<:context-begin "&lt;p>" "article">
<:=article>
<:context-end "&lt;/p>" "article">

# === article.txt ===
[article]
{header "First section"}
Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Nunc eu sodales justo. Mauris luctus ornare orci, ut finibus leo luctus.
#
{header "Next section"}
Maecenas lobortis, sapien posuere malesuada cursus, ante massa vestibulum
lacus, ac ornare metus magna id sem. Morbi eget egestas sapien.
</pre>

Without `context-begin` and `context-end`, the first paragraph in the output would
start with an unnecessary empty `<p></p>` before the heading.



## Temac Project Files
A Temac project file (`.temacproj`) acts as a minimal scripting container, similar to
a batch script. It is especially useful in environments where operating system–level
scripts are not permitted.

A project file can contain only:
 * Comments (lines beginning with `#`)
 * Printed messages (lines beginning with `echo`)
 * Temac command lines
 * Calls to other Temac project files

When processing a project file, Temac temporarily changes the current working directory
to the location of the `.temacproj` file, and restores the original directory afterwards.
If run with the `-verbose` option, these directory changes are clearly displayed.

Execution will stop if any included Temac command results in an error. A final result message
indicates whether the processing was successful or not.

<pre>
# === example.temacproj ===
# Lines beginning with '#' are comments, and empty lines are ignored.

echo Compiling the example project

# Compile the main page:
temac -f -s mainpage.temac

# Load and execute the helpfiles project:
temac help\helpfiles.temacproj
</pre>


## Troubleshooting

### Unwanted Empty Lines

Run Temac with the `-debug-newlines` option to get an idea of why empty lines appear.
Try inserting a comment marker (`<:>` in Temac syntax or `#` in definition syntax) at the
position where the unwanted line was generated.


### Which Files Are Actually Read and Written by Temac?

Run Temac with the `-files` option to get a summary of all input and output files.


### How Can I Find Out in Which Definition File a Variable Was Not Found?

Run Temac with the `-stop` option (and optionally `-files`). This will stop compilation
at the first error and print a variable dump, which probably reveals the details you need.


### I Have Another Problem

Run Temac with the `-tokens` option to check that it correctly interprets your commands.

Another useful trick is to insert `<:$err=>something` at the point where you want to
inspect the program state. When used together with the `-stop` option, this lets you
examine the values of your Temac variables at that position – including variables at
different sandbox levels – similar to placing a breakpoint during debugging.

