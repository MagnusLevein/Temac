Temac – Text Manuscript Compiler

<!--
© Copyright 2022 Magnus Levein.
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

> Temac is a great tool if you want to produce a lot of similar text files (e.g. HTML), built on the same skeleton but with different contents, or create a long file compiled together from smaller parts. Examples span from cookbook sheets to help files.

- [Command syntax](#command-syntax)
  - [Options](#options)
  - [Return values](#return-values)
- [Temac scripting languages](#temac-scripting-languages)
  - [Comments](#comments)
  - [Variables, macros and string constants](#variables-macros-and-string-constants)
  - [Counting, comparing and looping](#counting-comparing-and-looping)
  - [Accessing the file system](#accessing-the-file-system)
  - [Special commands](#special-commands)
- [Troubleshooting](#troubleshooting)


## Command syntax
<pre>
temac   <em>[options ...]</em>   <em>inputFile</em>  <em>[outputPattern]</em>
</pre>

**inputFile** - A Temac text file to read.

**outputPattern** - Pattern for construction of output filenames, with `@` indicating its base name. Defaults to `@.html`. Temac will only allow writing to file names maching this criteria.

### Options

#### <pre><strong>-?</strong>, <strong>-h</strong>, <strong>-help</strong></pre>
Show command syntax and options.

#### <pre><strong>-dn</strong>, <strong>-debug-newlines</strong></pre>
Print end-of-line token data in output files.
***Do not use this in production.***

#### <pre><strong>-f</strong>, <strong>-files</strong></pre>
List file usage summary to stdout.

#### <pre><strong>-p</strong> <em>text</em>, <strong>-parameter</strong> <em>text</em></pre>
Set variable `$parameter` to the specified text.

#### <pre><strong>-s</strong>, <strong>-stop</strong></pre>
Stop on first error, and print stack trace (to stderr) and variable dump (to stdout).

#### <pre><strong>-t</strong>, <strong>-tokens</strong></pre>
Dump tokenization of read files to stdout.

#### <pre><strong>-w</strong> <em>limit</em>, <strong>-whilemax</strong> <em>limit</em></pre>
Set max limit for while loops (default limit value is 1000).

### Return values
0) Ok
1) Warning or important information
2) Compilation error
3) Internal error
4) Argument error



## Temac scripting languages

Temac uses a *dual language approach*. The business logic or "motor scripts" are written with the *Temac syntax*, but definition files or "manuscripts" are written in a very simple *definition syntax*. The first file Temac reads is always a file with Temac syntax. (To read a file with definition syntax, utilize the `<:include-defs2html "...">` command described later.)

The Temac syntax is built on derivations of the expressions `<:>` and `<.>`, as these virtually never are used in normal HTML, Javascript or CSS code. It is described in detail in the following sections.

The definition syntax consists of only three expressions:
 * **`#`** starts a comment line, but only if it is the first non-blank character on that line.
 
* <code><strong>[</strong><em>variable</em><strong>]</strong></code> tells, as long as it is not preceded by any other text on that line, that what follows is to be loaded into variable *variable*. It could be a single line or several paragraphs, until next `[...]`-statement or end-of-file.

 * <code><strong>{</strong><em>variable</em><strong>}</strong></code> or <code><strong>{</strong><em>variable parameters ...</em><strong>}</strong></code> is used to invoke a variable (either a data variable or a macro variable). This expression retains its meaning in all positions.

A few more details about these three commands are given below, together with the Temac syntax counterparts.


### Comments
A comment in Temac syntax starts with `<:>` and continues until the end of the line.
<pre>
<strong><:></strong> <em>write your comment here</em>
Hello, World!<strong><:></strong> <em>or here</em>
</pre>

In definition syntax, the start-of-comment character is `#`, and it must be the first non-blank character of that line:
<pre>
<strong>#</strong> <em>write your comment here</em>
              <strong>#</strong> <em>or here</em>
</pre>


### Variables, macros and string constants

Variables in Temac are normally global. As we will see later, there is a sandboxing mechanism that
can be used to limit the scope.

Variable names are words that consists of letters, digits or underscore (`_`). The first
character cannot be a digit, but it can be a dollar sign (`$`). Temac uses the Unicode
character classification, which means that non-English characters are accepted too.

As a special feature in *definition synax*, the first character in the variable name of a `{...}`-expression, can be *anything* except control characters and blanks. This first character is transcoded to the form `_0000_` if it is not in the valid character range for variable names, where 0000 stands for a four-digit uppercase hexadecimal Unicode codepoint. Hence, a definition of variable `_002F_` could be invoked with the expression `{/}` in an included definition text file.

Variable names beginning with the dollar sign (`$`) are special variables. Do not create
normal variables with $-names.

* **`$null`** is always empty. Everything written to it will disappear.

* Writing to **`$err`** causes a compilation error, and prints its contents as a user-defined
error message.

* Writing to **`$status`** shows up as a progress status message during the compilation.

* **`$YYYY`**, **`$MM`**, **`$DD`** holds the current date (year, month and day in month).

* **`$FileIn`** is the name of the main input file, **`$FileOut`** is the name of the main output file,
and **`$FileName`** is the naked name without path or extension. These values are read-only
but can, however, be 'faked' during setup of a sandbox block.

* **`$parameter`** can be set from the command line, with option `-parameter`.

* **`$blankline`** is invoked for every paragraph break (blank line) in a
definition file. It is empty by default.

* **`$1`**, **`$2`**, **`$3`** (and so on) referes to the parameters sent to an invoked macro. **`$`** is
the number of parameters passed. (These variables are of course not global.)


#### Assigning text to a variable
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

If *variable* does not exist, it is created. If it already exists, it is either replaced
(variants `=>` and `=:>`) or appended to (variants `.=>` and `.=:>`). The use of `.=` as
append operator is inspired from PHP.

In definition syntax, there is just one variant, which replaces any old contents:

<pre>
<strong>[</strong><em>variable</em><strong>]</strong>
<em>assigns text until
next assignment starts</em>

<strong>[</strong><em>next_variable</em><strong>]</strong>
<em>... or the file ends</em>
</pre>


#### Assigning code to a macro variable
<pre>
<strong><:</strong><em>variable</em><strong>:></strong>
<em>assigns code until
the end of the block</em>
<strong><.></strong>
</pre>

If *variable* does not exist, it is created. If it already exists, it is replaced.

Temac, in fact, does not make any real difference between data variables and macro
variables. The reason for different assignment syntax is, that the code in a macro
definition is not evaluated until the macro is invoked. Any code in a variable assignment
(variants with `=`) is, however, evaluated during the assignment process.


#### Invoke a variable or macro variable
<pre>
<strong><:=</strong><em>variable</em><strong>></strong>
<strong><:=</strong><em>variable parameters ...</em><strong>></strong>
</pre>

This command interprets and inserts the contents of *variable*, and it works for data variables
as well as macro variables.

If *variable* is a macro variable (i.e. if it contains unprocessed code),
it is possible to send parameters to the macro. In the macro, the special variables
`$1`, `$2`, `$3` (and so on) gives read-only access to these parameters. The special variable `$`
holds the number of passed parameters.

This command is also available in definition syntax:

<pre>
<strong>{</strong><em>variable</em><strong>}</strong>
<strong>{</strong><em>variable parameters ...</em><strong>}</strong>
</pre>

As was mentioned earlier, this syntax accepts the first character of the variable name to be almost anything – any illegal character is transcoded to a `_0000_` expression (i.e. four uppercase hexadecimal digits, surronded by underscores).


#### String constants and numbers
In most cases, string constants (`"..."`) can be used instead of varibles. A number can be written without quotation marks, but is still stored as a string constant (and any leading zeros are stored too). Temac has no 'numeric' data type.

<pre>
<:=<strong>"Hello, World!"</strong>>
<:include <strong>"definitions.temac"</strong>>
<:=myMacro <strong>007</strong>>
</pre>

To include a literal quotation mark in the string, double it.


#### Read line by line from a data variable

If the variable is a data variable (i.e. it contains no unprocessed code), it is possible to fetch a specific line from it: 

<pre>
<strong><:=</strong><em>variable</em><strong>[</strong><em>line_number</em><strong>]></strong>
<:> Line numbers are 1-based.
</pre>


But please note that this is *not* a general syntax. It is a specific command. Hence, square brackets `[ ]` will not work in an expression where a variable is expected.

 It is also possible to count the number of lines:

<pre>
<strong><:count</strong> <em>variable</em><strong>></strong>
</pre>


#### Unwrapping prefix and suffix in data variables
<pre>
<strong><:unwrap</strong> <em>variable</em> <em>prefix</em> <em>suffix</em><strong>></strong>
</pre>


For a data variable it is also possible to remove a specified prefix and/or suffix from its content, effectively unwrapping the inner value.

<pre>
<:file=>index.html
<:name=:><:unwrap file "" ".html"><.>
<:ext=:><:unwrap file name ""><.>
Name: <:=name>  <:> will output Name: index
Ext: <:=ext>    <:> will output Ext: .html
</pre>


#### Sandboxing to protect the global scope

As mentioned above, Temac variables are global. The obvious exception is the parameter variables `$`, `$1`, `$2`, ... which of course only work in the invoked macro.

 To help protecting the global variable scope, it is possibile to set up a *sandbox* environment:

* In sandboxed code, all write operations to variables are local to the sandboxed environment, and once the sandboxed block is finished, those values disappear.

* Read operations in a sandbox environment are primarily local, but if no local variable exists with that name, the scope is increased to the outside of the sandbox.

* When the sandbox is initialized, it is possible to define specific variables to act as *pipes*. These can be already existing variables, or new ones which will be created. Writing to them from the sandboxed environment is possible, and their values will stay also when the sandboxed block finishes.

* Additionaly, it is possible to define a 'faked' file name for the sandboxed environment, which will set local values for `$FileIn`, `$FileOut` and `$FileName`. The intended usage for this is when a file is included, and it shall believe it was ran directly.

* Sandboxes can be nested.

<pre>
<strong><:sandbox</strong> <em>[filename]</em> <em>[</em><strong>%</strong><em>pipe] ...</em><strong>></strong>
  <em>put your sandboxed
  code here</em>
<strong><.></strong>
</pre>

The sandbox command can be used without parameters, or with a filename and/or with one or more pipe variables. The names of the pipe variables are preceded with a `%` sign before each of them. (The % symbol resembles two pipes on each side of a wall.)

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


### Counting, comparing and looping
#### Increment and decrement operators
<pre>
<strong><:</strong><em>variable</em><strong>++></strong>
<strong><:</strong><em>variable</em><strong>--></strong>
</pre>
Although Temac does not have mathematical functions, it can count up and down one step at a time. With these commands, *variable* is incresed or decreased by one, respectively. If *variable* cannot be converted to an integer, the start value is assumed to be 0 (and hence the resulting value will be 1 or -1).

This operation is internally done in three steps: *1)* read the old value, *2)* count up or down, and *3)* store the new value. Because of this, it is possible to use this operation on a variable defined outside of a sandbox scope. However, the new value will be stored in a new variable in the sandboxed environment.

<pre>
<:i=>1
<:sandbox>
  <:i++>
  i = <:=i>     <:> will output i = 2
<.>
i = <:=i>       <:> will output i = 1
</pre>

#### Comparing with if – else
<pre>
<strong><:if</strong> <em>variable1 comparition variable2</em><strong>></strong>        <strong><:if</strong> <em>variable1 comparition variable2</em><strong>></strong>
  <em>code to run if the                           <em>code to run if the
  comparition is true</em>                          comparition is true</em>
<strong><:else></strong>                                      <strong><.if></strong>
  <em>code to run if the
  comparition is false</em>
<strong><.if></strong>
</pre>

The **if** command can be used with or without **else**. Valid comparition operators are `==`, `!=`, `>=`, `<=`, `>` and `<`. If both *variable1* and *variable2* are integers, the comparition is numeric. Otherwise, it is a string comparition.

Since Temac stores all data variables as strings, it tries to convert the strings to integers prior to the comparition. Thus, `"007" == "7"` is **true**, as both of these strings will be converted to numeric 7, and compared as numbers. If this behaviour is not desired, using **switch – case** could be a remedy.

#### Comparing with switch – case – default
The **switch–case**-construction works as a number of **if** operations checked at once. (But the comparitions are always done as string comparitions, so 007 will not match 7.)

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

There must be at least one `<:case ...>` instruction, but `<:default>` is optional.

Compared to other languages, Temac has some special features when it comes to **case**:

* Temac does not require the test case values to be constants, they can be variables.

* Each case instruction accepts multiple test case values.

* The same test case value can appear in many case instuctions. In such a situation, they will be executed in the order they are written.

<pre>
<:January=>01
<:switch $MM>
  <:case January>
    First month of the year.
  <:case 01 02 03>
    First quarter if the year.
  <:case 01 02 03 04 05 06>
    First half of the year.
  <:default>
    Second half of the year.
<.switch>
</pre>



#### The while loop
<pre>
<strong><:while</strong> <em>variable1 comparition variable2</em><strong>></strong>
  <em>code to repeat as long as
  the comparition is true</em>
<strong><.while></strong>
</pre>

The comparition works exactly as in the `<:if ... >` command described above, with `==`, `!=`, `>=`, `<=`, `>` and `<` as valid comparition operators. 

As a protection against infinite loops, this command will break (with an error message) after too many turns. The default limit is 1000 iterations, but it can be changed with command line parameter **-whilemax** described earlier.


### Accessing the file system

#### Directory listing

<pre>
<strong><:getfiles</strong> <em>pattern [directory]</em><strong>></strong>
</pre>

This command returns filenames for files that match the *pattern* criteria (e.g. `"*.txt"`), in the *directory* given, or in the current working directory if no directory is specified.

The filenames are given one on each line, and can easily be managed with <code>‹:count <em>filelist</em>></code> and <code><:=<em>filelist</em>[<em>number</em>]></code> described above. (Remember that *number* is 1-based.)

#### Inclusion of files

<pre>
<strong><:include</strong> <em>filename</em><strong>></strong>
<strong><:include-text2html</strong> <em>filename</em><strong>></strong>
<strong><:include-defs2html</strong> <em>filename</em><strong>></strong>
<strong><:include-bin2base64</strong> <em>filename</em><strong>></strong>
</pre>

* **include** reads the file as it is, and interprets it as a Temac text file. Use this to include Temac code or HTML.

* **include-text2html** translates the characters `<`, `>` and `&` to their HTML entity codes (hence the '2html' part of the suffix in the command name). This can be used to show plain text in HTML, including source codes.

* **include-defs2html** reads a 'manuscript' text file with variable definitions, using the *definition syntax* described above. As the '2html' suffix indicates, it also translates the characters `<`, `>` and `&` to HTML entity codes. Use this to utilize the benefits of the definition syntax.

* **include-bin2base64** includes a binary file as a base64 encoded text. Use this to create base64-encoded data URIs.

#### Directing the output

<pre>
<em>this goes to the default output file</em>
<strong><:output</strong> <em>filename</em><strong>></strong>
  <em>output from here
  will go to the
  chosen file</em>
<strong><.></strong>
<em>this goes to the default output file too</em>
</pre>

Use this command to direct the output to a non-default output file. The given *filename* must satisfy the pattern for output files (which can be set on the command line).

If Temac detects that is is about to create an almost empty output file (a file with only whitespace from leading blanks, and maybe some line breaks), it ignores writing to it. If there already exist an older file with that name, a warning message is shown. This mechanism also applies to the default or 'main' output file.

Speaking of output files, Temac won't write to a file it already has read. This prevents accidental destruction of source files. (An error message appears if you try.)


### Special commands

#### Context-aware strings

This is a special feature primarily intended for HTML.

<pre>
<strong><:context-begin</strong> <em>text [name]</em><strong>></strong>
<strong><:context-end</strong> <em>text [name]</em><strong>></strong>
</pre>

Each of these commands inserts *text* in the output stream, but only *tentative*. If a **context-begin** text and a **context-end** text ends up one after the other (in that order), they both will disappear. The *name* parameter can be set (to an arbitrary string constant) to prevent unmaching pairs, which reduces the risk of difficult-to-find bugs.

The benefit of these commands are best explained with an example:

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

If this example had been written without *context-begin* and *context-end*, there had been an empty set of <code>&lt;p>&lt;/p></code> right before the first heading.



## Troubleshooting

### Unwanted empty lines

Run Temac with parameter `-debug-newlines` to get an idea of why empty lines appear. Try putting a start
of comment command (`<:>` in Temac syntax, and `#` in definition syntax) in the source code where the empty line was generated.


### Which files are actually read and written by Temac?

Run Temac with parameter `-files` to get a summary of read and written files.


### How can I find out in which of my included definition files a variable was not found?

Run Temac with parameter `-stop`, and possibly `-files`. This will stop compilation at the first error, and print a variable dump which probalby will reveal the details you need.


### I have another problem

You can try to run Temac with parameter `-tokens`, and make sure that Temac understands all your commands.

Another trick is to put a <code><:$err=><em>something</em></code> at a strategical position, and then run Temac with parameter `-stop`. This will let you see all of Temac inner thoughts at that place.


