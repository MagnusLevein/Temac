# Temac – Text Manuscript Compiler
> A great tool if you want to produce a lot of similar text files (e.g. HTML), built on the same skeleton but with different contents, or create a long file compiled together from smaller parts. Examples span from cookbook sheets to help files.

Temac uses a dual syntax approach. The business logic or "motor scripts" are written with the Temac syntax, but definition files or "manuscripts" are written in a very simple definition syntax, that is almost pure text. This approach makes it possible to let people with no coding skills write or contribute to the definition files (as long as you create the first example).

The Temac script syntax is an improvement of a similar scripting language I invented in 2006, and that has been used in production for over fifteen years by now. It is built on derivations of the expressions `<:>` and `<.>`, as these are virtually never used in normal HTML, Javascript or CSS code.

The project includes a handful of **pedagogic examples**, and a great **[Reference Manual](MANUAL.md)**.


## Getting started

### Download

Temac is currently only distributed as source code. It is written in C# .Net 8 with Visual Studio Community 2022 on Windows, but it should work cross-platform.

Download to your computer:
```shell
git clone https://github.com/MagnusLevein/Temac.git
```

### Run the examples
Open `Temac.sln` in Visual Studio and build the solution. Open `Examples/ReadMe.temac` in Visual Studio and read further instructions there.

### Read the manual
Read [Temac Reference Manual](MANUAL.md) included in the project.


## Licensing

The code in this project is licensed under GNU General Public License version 3.
