=== About ===

It converts the xml output from dotCover (http://www.jetbrains.com/dotcover/) and bullseye (http://www.bullseye.com/) to emma format. This allows the output to be tracked by the emma plugin (https://wiki.jenkins-ci.org/display/JENKINS/Emma+Plugin) for Jenkins (http://jenkins-ci.org) / Hudson (http://java.net/projects/hudson/).


=== Using ===

To convert a xml you can use:

   cover2emma -<input type> <input.xml> <emma.xml>

where <input type> can be dotcover or bullseye.

To convert the dotCover output you should call:
   cover2emma -dotCover dotCover.xml emma.xml

To convert the bullseye output you should call:
   cover2emma -bullseye bullseye.xml emma.xml


=== Thanks ===

A big thanks to Audaces (http://www.audaces.com.br) for allowing me to open source this project (it was developed internally because of our needs).


=== Dependencies ===

This projects uses jing-trang (http://code.google.com/p/jing-trang) to generate the DTD for the xmls and the xsd tool from Microsoft the generate the classes to load the xmls.
