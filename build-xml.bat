@echo off

call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86

java -jar trang-20091111\trang.jar dotcover.xml dotcover.xsd
xsd dotcover.xsd /classes /n:dotcover

java -jar trang-20091111\trang.jar emma.xml emma.xsd
xsd emma.xsd /classes /n:emma

java -jar trang-20091111\trang.jar bullseye.xml bullseye.xsd
xsd bullseye.xsd /classes /n:bullseye

pause
