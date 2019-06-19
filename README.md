BAT RECORDING MANAGER

Bat Recording Manager (BRM) is a piece of software that I have developed largely for my own use, but I make it freely available although with no warranties of any kind.  It is continually being upgraded and new versions will be posted for download.

Bat Recording Manager arose because I was accumulating large numbers of full spectrum recordings of bats using a variety of equipment from laptops with a native sound card capable of digitising at 192ksps, to a Wildlife Acoustics EM3+ with a Neumic microphone, or a Petterson M500-384 recording to Bat Recorder on an Asus 7" Android tablet.  I needed a better way to keep track of all my recordings and to be able to locate recordings of specific bats quickly and easily.

Bat Recording Manager is a database application that stores metadata about all of your recording sessions and has details of each individual recording (i.e. .wav file) and annotations made to that recording when it was analysed.  It can display details of all your recording sessions, or it can display those sessions and recordings in which a specific species of bat was encountered.  It will accept images which can be attached to recordings, labelled segments of a recording or to particular bats.  Data can also be exported in a variety of report formats in .csv format.

Normal use is predicated on the use of Audacity to analyse recordings, using label tracks to store annotations for particular segments of a recording.  This approach is well suited to my own way of recording which is to record continuously into contiguous four minute long files.  However this approach is not so appropriate for long duration monitoring in which recordings are triggered by ultrasound and last just long enough to capture a single pass.  If recordings are made in this way then they can be more easily analysed using Kaleidoscope Viewer (the free version of Kaleidoscope) and the .wav files with their annotations can then be imported into Bat recording Manager.

Version 7.0 implements the ability to analyse and import on the fly.  In this mode the user can select a folder containing a group of .wav files and they will be opened in turn in Audacity for analysis.  As Audacity saves the label track and closes the information is immediately imported into Bat Recording Manager.  This saves considerable effort when analysing large numbers of files, but does require Audacity to be correctly configured in the first place.

Updates:-
BRM 8.0.7088 Download
v8.0.7088

Upgrades to using .NET 4.7.2 and includes data dirtualization which speeds up page loading in some situations.

Instructions:-

The link above will take you to the Figshare page which hosts this and earlier versions for Bat Recording Manager.
Hover over the file you wish to download and click the blue down-arrow on the right to download the file.
OR
Click on the specific setup file that you wish to download (i.e. the one with the highest version number) - a new page will open saying that the file cannot be viewed.
Click on the 'Down File' link ( a small blue down arrow) to download the setup file to your computer.
THEN
Save the file in a known location, and then run it.
The Setup file will install the requisite version of .NET and the LocalSQL database support needed by the program.
Run the program and read the Help section for instructions on how to configure and use the system and for the file storage and naming conventions that it expects.
In the event of any errors, look in C:\BRM-Error for an error log file, and send it with a description of what went wrong and details of your computer and operating system, and I will do what I can to sort out the problem. Send to Justin.halls@echolocation.org.uk
The program does need a reasonably powerful Windows 10 computer with a decent amount of memory in order to perform effectively, especially when the number of recordings is large.

----------------------------------------------------------------------------------------------------------------------------

v7.0.6789 contains bug fixes to prevent crashes when starting up with or switching to an old database which did not contain the Version table.  This bug could cause crashes at start-up or when trying to switch to an alternate database.

v7.0.6799 has cosmetic changes to the way in which folders are selected for analysis or to save images.

v7.0.6844 Allows the addition of fiducial lines to images in the comparison window.  Lines can be adjusted or deleted and their position is stored in the database when the window is closed. Exported images have the lines permanently drawn onto the image.  Exported image numbering is now modified to prevent overwriting pre-existing images in the destination folder.  More changes to try to prevent the program from crashing at start up when trying to identify the database version.  Drop me a line if you have this problem and let me know whether or not this version is an improvement.

7.0.6850 Fixes an occasional bug when saving images through Analyse and Import - using filenames in the caption has priority over bat's names.  Also improvements in file handling when changing databases - now attempts to recognise if a db is the right type.

7.0.6858 Makes some improvements to image handling, including a modification to the database structure to allow long descriptions for images (previously description+caption had to be less than 250 chars) and the ability to copy images within the application (but not to external applications).  A single image may now be used simultaneously as a bat image, a call image or a segment image.  Changes to it in one location will be reflected in all the other locations.  On deletion the link is removed and if there are no remaining links for the image then the image itself will be removed from the database.

7.0.6859 has some improvements to the image handling system.  In the batReference view the COMP button now adds all bat and call images for all selected bats to the comparison window.  Double clicking on a bat adds all bat, call and segment images for all the bats selected to the comparison window.

7.0.6860 removed the COMP button from the bat reference view.  Double-clicking in this view transfers all images of bat, calls and recordings to the comparison window.  Double-clicking in the ListByBats view transfers all recording images but not the bat and call images to the comparison window.  Exported images for recordings use the recording filename plus the start offset of the segment as a filename, or alternatively the image caption.

7.0.6865 Improvements to the grids and to grid scaling and movement especially for the sonagram grids.

7.0.6876 Added the ability to right-click on a labelled segment in the recordings detail list control, to open that recording in Audacity and scroll to the location of that labelled segment.  Only one instance of Audacity may be opened at a time or the scrolling does not work.  Also made some improvements to the scrolling behaviour of the recording detail window.

Version 7.1 makes significant changes to the way in which the recordingSessions list is displayed.  Because this list can get quite large and therefore takes a long time to load, it now loads the data in discrete pages.

At the top of the RecordingSessions List is a new navigation bar with a set of buttons and two combo-boxes.  The rightmost combobox is used to set the number of items that will be loaded and displayed on a page.  The selections are currently 10, 25, 50 and 100.  Slower machines may find it advantageous to use smaller page sizes in order to speed up load times and reduce the demand for memory and cpu-time.

The other combobox allows the selection of a sort field for the session list. 

Sessions are displayed in columns in a DataGrid which allows columns to be re-sized, moved and sorted.  These functions all now only apply to the subset of data that has been loaded as a page.  The Combo-box allows you to sort the full set of data in the database before loading the page.  Thus if the combobox is set to sort on DATE with a Page size of 10, then only the 10 earliest (or the 10 latest depending on the direction of sorting) sessions in the database will be loaded.  The displayed set of sessions can be sorted on the screen by clicking the column headers but this only changes the order on the screen, it does not load any other sessions from the database.

The four buttons can be used to load the next or previous pages or to move to the start or end of the complete database collection.  The Next or Previous buttons move the selection by 2/3 of the Page Size so that there will always be some visual overlap between pages.
The sort combo-box has two entries for each field, one with a suffix of ^ and one with a suffix of v .  These sort the database in Ascending or Descending order.  Selecting a sort field will update the display and sort the display entries on the same field, but the sort direction of the displayed items will be whatever was last used.  Clicking the column header will change the direction of sort for the displayed items.

v7.1.6885 Updates the database to DB version 6.2 by the addition of two link tables between bats and recordings and between bats and sessions.  These tables enable much faster access to bat specific data.  Also various improvements to improve the speed of loading data when switching to List By Bats view, especially with very large databases.

v7.1.6891 Further performance improvements in loading ListByBats and in loading images

v7.1.6901 Has the ability to perform screen grabs of images without needing an external screen grabber program.  Shift-Click on the 'PASTE' button and drag and resize the semi-transparent window to select a screen area, right click in the window to capture that portion of the screen.  For details refer to Import/Import Pictures

v7.1.6915 Fixed some scaling issues with Fiducial lines in the comparison window.

v7.1.6941 Adjustments and improvements to the grid settings and fiducial lines.  The Help file has a summary of all menu, mouse and keystroke commands.

v7.1.6951 Fixes some problems with the Search function

Version 7.2

Version 7.2 introduces the ability to play labeled segments at reduced speed or to play them as though heard with a tuned bat detector.  Selected segments can be played just once, or looped repeatedly until stopped and speed reductions of 1,1/5,1/10 and 1/20 are available. In the initial build, the segments are copied completely into memory and this may cause problems on machines with limited RAM available.
The Audio Play function is accessible from the Recordings Pane of the List Recordings View, or from the Comparison Window.

v7.2.6971 When opening a recording or segment in Audacity the corresponding .txt file will be opened as a label track.  NB this only works if there is only a single copy of Audacity open - subsequent calls with Audacity still open do not open the label track.

v7.2.6984 Improvements to image handling and some bug fixes.  You can now append a time range in seconds after the filename caption of an image and a new dummy segment with that timing will be created if en existing segment cannot be found.

-----------------------------------------------------------------------------------------------------

BRM-Aud-Setup_v7_2_7000.exe

This version includes its only private copy of Audacity 2.3.0 portable, which will be placed in the same folder as BRM and has its own pre-configured configuration file appropriate for use with BRM. This will not interfere with any existing installation of Audacity but provides all the Audacity features required by BRM with no further action by the user. BRM will use this version to display .wav files.

v7.2.7000 also includes a new report format which is tailored to provide data for the Hertfordshire Mammals, Amphibians and Reptiles survey.  It also displays the GPS co-ordinates for the Recording Session as an OS Grid Reference as well as latitude and longitude.

v7.2.7010 Speed improvements and bug-fixes to opening and running Audacity through BRM.  Audacity portable is now located in C:\audacity-win-portable instead of under the BRM program folder.

v7.2.7021 Upgrades the included version of Audacity to 2.3.1 and makes some other minor bug fixes

Version 7.3

Version 7.2 was found to have problems installing on some systems.  These problems have largely been resolved in 7.3 and this version has been demonstrated to install correctly on a virgin installation of Windows 10.

It will not run correctly on Windows 8/8.1 due to problems with installing the database server components.

Version 7.3.7038 has resolved the installation problems encountered with version 7.2 and has been successfully installed on a Virgin Windows 10 computer.  It does have a problem when importing data from recordings with Wildlife Acoustics recorders that store their own metadata in WAMD format and which have been analysed using Kaleidoscope.  In this case the metadata is not imported correctly.  There is no problem in situations where Kaleidoscope stores the analysis data in GUANO format, or when using Audacity.  This problem is currently being worked on.

version 7.3.7045 now works correctly with both WAMD and GUANO metadata or combinations of both.  Also fixes a few other bugs and ensures that file extensions are not case sensitive.

Version 7.3.7056 Adds the ability to link recording or segment images to bats or call types in the Bat reference View.  The Image scroller in Bat Reference View has an IMPORT button which will attach the currently selected image in the Comparison Window to the currently displayed bat or call type.  If the Comparison Window is not open or is empty the button does nothing.

v7.3.7062 fixes some problems with the import pictures function and cosmetic problems in the Import function. Also fixes scrolling problem in recordings list.

version 7.3.7067 corrects some inconsistencies in the open folder dialogs and tidies up some of the UI operations adding COMP-ALL buttons for the three main view panes and allowing a double-click to open a labelled segment in Audacity.

Download Bat Recording Manager v 7.3.7067

Instructions:-

The link above will take you to the Figshare page which hosts this and earlier versions for Bat Recording Manager.
Hover over the file you wish to download and click the blue down-arrow on the right to download the file.

OR

Click on the specific setup file that you wish to download (i.e. the one with the highest version number) - a new page will open saying that the file cannot be viewed.

Click on the 'Down File' link ( a small blue down arrow) to download the setup file to your computer.

THEN

Save the file in a known location, and then run it.

The Setup file will install the requisite version of .NET and the LocalSQL database support needed by the program.

Run the program and read the Help section for instructions on how to configure and use the system and for the file storage and naming conventions that it expects.

In the event of any errors, look in C:\BRM-Error for an error log file, and send it with a description of what went wrong and details of your computer and operating system, and I will do what I can to sort out the problem. Send to Justin.halls@echolocation.org.uk
The program does need a reasonably powerful Windows 10 computer with a decent amount of memory in order to perform effectively, especially when the number of recordings is large.

Bat Recording Manager - Using the Program

Introduction

This program provides a management system for your library of bat (or other) recordings. It can be flexible in its approach but is primarily designed to work with libraries stored and analysed according to a particular protocol which is described below.
The program has four main ‘views’. The default view lists all recording sessions, with details of the currently selected session. Sessions can be added or edited manually or deleted from the database. Similarly, individual recordings within the session can be added, edited or deleted.
The program assumes that the recordings are of bat calls, and incorporates a reference list of bat species, which can also be modified by the user. This reference list also defines a set of ‘tags’ which can be used when analyzing recordings to indicate the presence of each species.
Recordings may also be displayed ‘by bat species’. In this case, the defined species are listed and the recording sessions and recordings which detected the selected species are shown.
An ‘Import’ page allows recording sessions which have been analyzed using Audacity to be imported into the database. A single folder containing a set of recordings may be imported or a selection of folders can be imported sequentially.
Terminology
BAT refers to a single species of bat. The default reference library contains a list of British bats and can be added to by the user. The reference library also contains explicit entries for ‘No Bats’ and ‘Unknown bat’.
TAG refers to an identifier for a particular species. A bat may have several identifiers, but identifiers should be unique to a species. Tags and bats have an order and are searched in that order – the order may be changed by the user. If a recording segment does not contain a recognized tag it will be marked as ‘Unknown’ bat. Tags in mixed case are not case sensitive, but a tag in all upper case IS case sensitive. Thus BLE matches ‘BLE’ but not ‘Probable’.
SESSION or Recording Session refers to a set of recordings made on the same occasion at a particular location. A session should have an identifying session tag which will generally be the name of the folder containing the recordings made during that session.
RECORDING a recording refers to a single .wav file. It should have an associated text file with the same name but the extension .txt instead of .wav. The text file is typically a Label track exported by Audacity and contains a series of entries each with a start time in seconds, and end time in seconds and a comment. The times are offsets into the recording and the comments should contain tags identifying the bats present.
SEGMENT or Labelled Segment is the portion of a recording which is marked with a comment in an Audacity Label track. It corresponds to a single entry in the text file.
PASS or Bat Pass is a measure of the abundance of bats. A bat pass is a contiguous set of pulses from a bat. Usually at least three pulses should be present for reliable identification. A continuous sequence of pulses of more than 7.5s is counted as several pulses. In this case, the duration of the segment is divided by 5 and rounded to the nearest integer to determine the number of passes. If more than one bat is identified in the same segment both bats are credited with the full number of passes.
My Protocol
The protocol described here assumes the use of an EM3+ bat detector/recorder, but maybe simply adapted to other systems.
Recording
The EM3+ is set to trigger at a 0dB threshold, and to record continuously into 4 minute long recordings. Recording mode is for .wav files at 384ksps and the EM3+ uses 16-bit digitisation.
Before each session the date and time are checked and the recording prefix is set. The recording prefix is typically a 3 or 4 character identifier which indicates the location, 2 digits identifying the year, a dash and an ordinal digit identifying the particular session at that location. E.g. BBY15-2 would be the prefix for the second session at Bayfordbury in 2015. Users may define alternative codes of their choice. The EM3+ names each file with the prefix plus the date and the time at which the recording started. The timestamp on the file indicates the time at which the recording ended.
Saving the recordings
When the session is complete the recordings can be moved into a folder on the PC for analysis. I place all of my recordings into a root folder called ‘BatRecordings’, which contains individual folders for each year. The year folder contains folders for each session, each of which contains all the recording .wav files made during that session. The folder is then named using the session prefix and date e.g. BBY15-2__20150903 for a session recorded on the 3rd of September 2015.
A ‘Header’ file may be created to add any extra notes about the recording session. This file should be a plain text file with a .txt extension and will be used by the program to try to extract basic information about the recording session. The first line of the file should consist of ‘[COPY]’, which will prevent the program from attempting to analyse it for bat recording data, and will identify it as a header file.

e.g.
[COPY]
Bayfordbury Field Centre
3 Sep 2015, 18:55 – 22:15
Justin A T Halls
EM3+ 384ksps, 16 bit
Neumic microphone at 5% gain
10°C light cloud, calm
51.758367,-0.023765
Walking round the usual route in a clockwise circuit.
 
If a GPS recorder was in use during the recording session (Ultra GPS on an Android phone works well and is free) then the resultant .gpx file should also be added to the folder containing the .wav files. It will be used by the program to identify the locations for the start and end of each recording.
If the recorder includes GUANO metadata (e.g. Peersonic, Petterson M500) then the header file should include a line containing [GUANO] so that the program will extract the metadata and include it in the recording notes.
Analysing the Recordings
Each .wav file can be analyzed in Audacity (current version 2.3.1) either using the portable version included with the BRM installer, or a version configured as described in the BRM help file.  The specific configuration enables BRM to open files and position them automatically.  After loading the file in spectrogram view, B will generate a new label track (backspace to delete the blank label). Copy the ‘Name’ of the audio track into the ‘Name’ of the label track. Expand the view to show 3-4s of the recording and press ‘J’ to go to the start of the track. Scan through the track, highlighting sections with bats and pressing B to add a label. The label may contain any comments but should include the tag for the identified species of bat or bats. The comment may include more than one bat tag. If there are no bats present in the entire recording select all with A and add a comment of ‘No Bats’.
When the recording has been fully analyzed, export the label track (it will default to the Name of the track, making sure that it is saved into the folder containing the original recording. Then close the wav file without saving. Repeat this process for all the recordings in the session.
The comment may optionally include call statistics enclosed in '{' and '}' .  Parameters that may be specified are: start frequency, end frequency, peak frequency, duration, inter-pulse interval and additional comments which will not be examined for bat tags - thus notes about bats not present may be included in {} to prevent that bat's name being included in the analysis.  The format of the call statistics is {sf=50,ef=20,pf=30,d=3,i=120,these numbers are examples, frequencies in kHz and times in ms}.  The numerical parts may be integer or decimal and may be either single numbers or they may specify a range.  A range of values may be given in the form 34.3+/-2.5 or as a simple range of value such as 45.3-23.4 in which case the range will be re-interpreted as m+/-d where m is the mean of the upper and lower limits and d half the difference between them.
Additionally, a comment may include a confidence level in the form of a single capital letter as the final character.  The letter may H (high confidence), M (medium confidence) or L (low confidence).  This will be used to colour code the comment in the main listings.
Since Audacity 2.1.3 the label text may also include the frequency extents of the selection when the label was created.  This requires spectral selection to be turned on and the selection will need to be terminated within the audio track and not dragged down into the label track so that the label will then be inserted with CTRL-B.  In the label text file, the upper and lower frequencies will be included on a separate line after the label starting with a backslash character. 
If the selection ended in the label track (which produces a lower frequency with a -ve value) the spectral parameters will be ignored.
If the label contains the string "{}" then the spectral parameters will be ignored.
If the label contains a parameter section {s=fff.ff, e=fff.ff, ...} then the spectral parameters will be ignored.
If the label contains a parameter section starting with a number and containing a comma {34.5, ...} then the spectral parameters will be ignored.
Otherwise, if the label contains a parameter section, the string s=fhi,e=flo, will be inserted immediately after the '{'
or if there is no parameter section then a parameter section defining start and end frequencies will be added to the end of the label.
Processing the Analysis
Import Data
Start the Bat Recording Manager program and select ‘Import’ from the ‘View’ menu. The application will display two empty panes with 5 buttons – Import, Sort, Process, Next and Select. Click the ‘Import’ button to select the folder containing the session to be imported. The selected folder should contain .wav and matching .txt files and may optionally container a header .txt file and a .gpx file. If the selected folder does not contain .wav and .txt files, then the subsidiary folder will be searched to find all subfolders which do contain .wav and .txt files.
The first such folder found will be opened and the contents of the .txt files will be displayed in the left-hand pane. Also, the Recording Session form will be displayed with the fields filled in as well as the program was able to from information found in the header file. Check that the data displayed in the form is correct and make any corrections and additions that may be necessary, then click OK.
If there is only one .txt file, or the .txt files have the double extension .log.txt then it is assumed that the .txt file is a header file and that the .log.txt file is a manually generated log file. The program will then attempt to split the log file into separate sections relating to each .wav file in the folder.
If multiple folders were found then the button bar will show the number of folders found and the Next and Select buttons will be enabled. The Select button will bring up a list of the folders that were found. If a folder is selected a Delete button will appear allowing it to be deleted from the list. ‘Add Folder’ and ‘Add Tree’ buttons allow additional folders and folder-trees to be added to the list. Click OK when the list is satisfactory.
Clicking the Next button will advance to the next folder in the list whether or not the currently selected folder has been processed.
Clicking the Sort button will display a list of the files in the current folder. Individual files can be moved up or down the list and additional files may be added or deleted from the list. It is advisable to ensure that the header .txt file is the first file in the list and that the remaining files are in chronological order. Click OK when the list is satisfactory.
Clicking the Process button will do two things. First, the information from the .txt files will be concatenated into a file with a .log.txt extension which will summarise the session information and the recordings data in a standardised format. The .log.txt file will automatically be saved in the folder being processed and the user will be asked if any existing file should be overwritten. The .log.txt file will also be displayed in the right-hand pane. Secondly, the data about the session and recordings will be saved in the program’s main database.
If there are more folders available to process, clicking the Next button will move on to the next folder.
List Recording Sessions
Select ‘List Recordings’ from the ‘View’ menu to display the list of all recording sessions that have been imported so far. The sessions are listed in the left-hand pane headed by a button bar with Add, Edit, Delete, and Export buttons.
Selecting a folder from the list will populate the right-hand pane with the details of the session. The right-hand pane is split into two parts. The upper part shows the overall session details including summary information about each species encountered during the session. The lower part shows details of each recording and each labelled segment in each recording. Each recording also contains summary information about all the bats encountered in that recording.
Double clicking on the GPS co-ordinates in the recording session detail pane will pop-up a window containing a map with the GPS co-ordinates of each recording marked. This map does require an internet connection.
The borders between the upper and lower right-hand panes, and the border between the left and right-hand panes may be dragged to optimise the layout according to the resolution and size of your screen. The three main panes are also equipped with scroll bars to allow all of the data to be viewed.
The Add button in the Sessions panel allows additional sessions to be added to the list without going through the Import process. A blank Recording Session form will be displayed allowing the user to provide session details manually. Recording Device, Microphone, Operator and Location fields are Combo Boxes which allow the user to select from a list pf all previously defined items in those categories, or new information can be typed into the box.
The Edit button uses the same form to allow the user to modify the details of the selected session.
The Export button saves information about the species observed during the selected session to a .csv file which can be imported into an Excel spreadsheet. The format is Date, Place, Grid Ref (GPS co-ordinates), Comment (Equipment details), Observer, Species, Abundance/Passes, Additional Info (Session Notes). This format is compatible with that used for HMBG records.
Recordings
Additional Recordings may be added to a session using the Add button at the top of the list of recordings. A blank form will be displayed allowing the details of the recording to be added manually. The filename should be the name of the .wav file and may be typed in or browsed for using the ‘…’ button. In addition details of labelled segments maybe added to the recording. A labelled segment requires a start time in seconds, and end time in seconds and a comment. The times are offsets from the start of the recording file.
Similarly, existing recordings and segments may be edited or deleted from the list.
List By Bats
Select ‘List By Bats’ from the ‘View’ menu examine recordings and sessions organised by the types of bat present. A list of all known bats will be displayed in the left-hand pane, with the Common Name, Genus, Species, number of sessions, number of recordings and number of passes for each species.
Selecting a species of bat will display two lists in the right-hand pane. In the upper panel a list of all recording sessions featuring the selected bat, and in the lower panel a list of all recordings featuring that bat. If a specific session is selected then the recordings list will only show the recordings for that session.
Double-clicking on a recording will open the corresponding .wav file if it is present at the original location on the computer. Double-clicking on a recording session will switch to the ‘List Recordings’ view with that recording session selected.
Bat Reference
Select ‘Bat Reference’ from the ‘View’ menu to see a list of all bats known to the system. Selecting a bat from the left-hand pane will display the details about that bat in the right-hand pane. The Add/Edit/Delete buttons at the bottom of the list can be used to add new bats or modify details of existing bats. The final three items, NoBats, No Bats, and Unknown should not be changed. The order of the bats may also be changed using the UP and DOWN buttons. When the program is trying to identify a bat from its Tag the list of bats will be searched in the order shown. Thus a tag of ‘Soprano Pip’ will find the entry of Pipistrellus pygmaeus before the ‘Pip’ tag for an unknown Pipistrelle.
Each bat has a common name, which should be unique in the list, a Genus and a Species. For situations where the genus is not known, e.g. for a P50, the species entry should state sp..
Each bat should also have a list of tags by which it may be identified in the comments section of labeled segment. Multiple tags are allowed for each bat to allow for preferred abbreviations and the list of tags will be searched in order, the search stopping once a match is found. Note that while the search through the tag list terminates once a match is found, the search will continue through the rest of the list of bats so tags should not be duplicated.
Bat tags are generally not case sensitive unless the tag is all in upper case when it is case sensitive. This allows the use of tags such as BLE without the tag recognising the substring in probable as a matching tag.
Multiple Databases
There is a limited ability to access multiple databases through the ‘File’ menu. A new database of the correct format can be created by using the File/Create database menu item. A dialog allows the user to select the folder in which to create the database and to give a unique name to the database. The name must end with ‘BatReferenceDatabase.mdf’ or it will be rejected by the program. The file open dialog prompts the user with the name ‘_BatRefeenceDatabase.mdf’ so that it is simple to add a unique prefix as desired.
It is not advisable to delete a database, and even if the database files are deleted the database name cannot be re-used since it is registered in the database management system.
When a new database is created it will become the current database and new bat data can be imported or entered in the usual way.
Access to the original database can be restored by selecting File/Use Default Database, or an alternative database can be accessed by selecting File/Choose Database.
Restrictions
There is a restriction on the use of multiple databases. If an alternative database is either chosen or created and then another database is selected, it will not be possible to return to the originally selected or created database without first closing the program. It should always be possible to return to the original default database.
Created with the Personal Edition of HelpNDoc: Generate Kindle eBooks with ease
Technical Information
The Bat Recording Manager is provided free of charge, but voluntary donations may be made through Paypal to justin.halls@echolocation.org.uk.
The program runs under Windows and requires Microsoft .NET 4.6.2 to be installed.  The setup program includes a copy of .NET 4.6.2 and will install it if it is not already installed on the computer, but it is not guaranteed that the version installed this way will be the latest version or will include all the latest updates.
The program stores the data in a local SQLExpress database in the form of a .mdf file which will also have an associated .ldf file.  The setup program also includes a copy of SQLExpress LocalDB which will be installed on the target computer.
The program will install a default database into C:\Users\username\AppData\Roaming\Echolocation\WinBLP, along with a .xml file containing a basic set of  bat reference definitions.  The contents of the .xml file are used to initialise the database, after which it is renamed with a .bak extension.  This .bak file will be used if a new database is created.
If the database or .xml files already exist when the program is installed they will not be replaced by the new versions so that existing data will not be replaced or deleted when a new version of the program is installed.
The .xml bat reference file contains basic information on British bats but can be easily extended by the user through the program.  At present, it is not possible to revise the reference list by issuing updated .xml files but this feature is planned for a future release.  Since version 7.0 it is possible to define short fully capitalized identification tags for bats which will be replaced during the importation process with the full common name of the bat.  It can, therefore, be advantageous to add such short tags to the bat reference (e.g. CP for common Pipistrelle, SP for Soprano, NN for noctule, MD for Daubenton's etc) which can save a considerable amount of typing during analysis without leaving the annotations full of cryptic abbreviations.  The analysis .txt files remain unaltered, substitutions are only made in the information stored in the database.
