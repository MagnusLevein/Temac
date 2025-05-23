# Temac – Text Manuscript Compiler
> Temac is a powerful tool for generating many similar text files (e.g., HTML pages) based on a shared
> template but with varying content, or for compiling a large file from smaller components. Use cases
> range from cookbook pages to help files.

Temac uses a *dual language approach*. The business logic (or "motor scripts") is written in
**Temac syntax**, while definition files or "manuscripts" use a much simpler **definition syntax**,
that is nearly plain text. This approach makes it possible for non-programmers to contribute to the
definition files (once an initial example is in place).

The Temac script syntax is an improvement of a similar scripting language I invented in 2006, and that has been
used in production for over fifteen years. It is based on derivations of the  `<:>` and `<.>` expressions—sequences
that are very unlikely to appear in standard HTML, Javascript, or CSS.

The project includes a handful of **pedagogical examples**, and a comprehensive **[Reference Manual](MANUAL.md)**.


## Getting started

### Download

Temac is currently only distributed as source code. It is written in C# .Net 8 with Visual Studio Community 2022 on
Windows, but it should work cross-platform.

Clone the repository:
```shell
git clone https://github.com/MagnusLevein/Temac.git
```

### Run the examples
Open `Temac.sln` in Visual Studio and build the solution. Open `Examples/ReadMe.temac` in Visual Studio and read
further instructions there.

### Read the manual
Read [Temac Reference Manual](MANUAL.md) included in the project.


## Licensing

The code in this project is licensed under GNU General Public License version 3.
