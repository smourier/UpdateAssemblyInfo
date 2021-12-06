# UpdateAssemblyInfo
Updates `AssemblyFileVersionAttribute` assembly attribute (and only that) from a .cs file. For example this file :

	[assembly: AssemblyVersion("1.0.0.0")]
	[assembly: AssemblyFileVersion("1.0.0.100")]

Will be changed into this file:

	[assembly: AssemblyVersion("1.0.0.0")] // this is not changed
	[assembly: AssemblyFileVersion("1.0.0.101")]

It can be used with a git pre-commit hook https://stackoverflow.com/questions/17101473/change-version-file-automatically-on-commit-with-git

Exemple: of a pre-commit hook (the file must be named "pre-commit" and put in the .git/hooks folder, and copy UpdateAssemblyInfo.exe somewhere in the PATH):

	#!/bin/sh
	#
	# update AssemblyInfoVersion.cs
	# Called by "git commit" with no arguments. The hook should
	# exit with non-zero status after issuing an appropriate message if
	# it wants to stop the commit.
	#

	UpdateAssemblyInfo.exe "Properties/AssemblyInfo.cs"
	git add "Properties/AssemblyInfo.cs"

