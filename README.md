# Auto-updater

Auto update module for .NET applications

## Auto Update Library



## Update Server

Update server is used to generate *manifest.xml* file which is later used to compare application version and files.

Paste update files to the *updates* directory and generate manifest file with the application. 

*Manifest.xml* has following structure:

```
<?xml version="1.0" encoding="UTF-8"?>
<AutoUpdate>
  <update appId="appID">
    <version value="1.2.3" />
    <url value="path to the app's update directory" />
    <exe value="name of the exe file to be launched after install" />
    <description value="about app" />
    <launchArgs value="launch arguments" />
    <directory name="root">
      <directory name="dir name">
        <directory name="dir name">
          <file name="file name" md5="ddc36f7deac22766a012bef7442dbf9a" />
          <file name="file name" md5="0f4db4016badbd2995f7a216f76173f4" />
          <file name="file name" md5="679bf008d7dcccdd8d4c72316ca9c189" />
        </directory>
        <directory name="dir name">
          <directory name="dir name" />
          <directory name="dir name" />
          <directory name="dir name" />
        </directory>
        <file name="file name" md5="d4879237ba9d9bb4859e33b0b1742cef" />
        <file name="file name" md5="e0c5adf8e536b6663a2bad267bf6dffc" />
      </directory>
    </directory>
  </update>
</AutoUpdate>
```
In order to read existing manifest file and show it in app click at *Load Previous Version*.

If you have pasted new app in the updates directory but you want to save *appID*, *launch args*, etc. just click at *Read directory*. This will create new XmlDocument without this values, that will be added when you click *Generate XML*. At this moment new *manifest.xml* file is created at */app* directory.

Currently for sending files to the client *HFS* is used. Setup it and you are ready to go.

Complete documentation can be found at http://www.rejetto.com/wiki/index.php?title=Main_Page.

Download page: http://www.rejetto.com/hfs/?f=dl.

## TODO
- Add File server functionalities to the *Update server* and change *HFS* with it.
- Finish unit testing
