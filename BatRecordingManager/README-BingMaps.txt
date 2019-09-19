The Bing Maps used in this project are provided under license from Microsoft.
In order to work correctly they need a valid license key, freely available from Microsoft but which cannot be shared.
To make the maps work in this project you will need to obtain your own license and place it in a XAML resource dictionary named 'MapResourceDictionary.xaml'
The file should contain the following code:-

<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:local="clr-namespace:BatRecordingManager"
                    xmlns:core="clr-namespace:Microsoft.Maps.MapControl.WPF.Core;assembly=Microsoft.Maps.MapControl.WPF"
                    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                    xmlns:wpf="clr-namespace:Microsoft.Maps.MapControl.WPF;assembly=Microsoft.Maps.MapControl.WPF"
                    >

    
    <wpf:ApplicationIdCredentialsProvider x:Key="CredentialsProvider1" ApplicationId="INSERT YOUR LICENSE KEY HERE"/>

</ResourceDictionary>


If you wish to continue to work without a license key, the MapResourceDictionary can be removed from the 'Merged Dictionaries' list in App.Xaml

==================================================================================================================================================
From version 8.0.7200 and greater:-
API keys are required for Bing Maps and for DarkSky.Net weather history.  These keys can be obtained for free from Microsoft (Bing Maps)
and from DarkSky.Net but may not be shared with other users.  Therefore they are compiled into the code so that they are not visible in the
installable distribution and are excluded fromthe GitHub source files.
There is a partial class contained in the source called APIKeys.cs which contains entries for the program to retrieve the API Keys as needed.
For local use using your own keys you need to create an additional file - APIKeysLocal.cs containing a partial class as below:-

namespace BatRecordingManager
{
    /// <summary>
    /// Local version of APIKeys which contains my personal API keys
    /// </summary>
    public static partial class APIKeys
    {
        static APIKeys()
        {
            BingMapsLicenseKey = "YOUR BING MAPS LICENSE KEY GOES HERE";
            DarkSkyApiKey = "YOUR DARK SKY API KEY GOES HERE";
        }
    }
}

This local file should not be committed to GitHub or you will be in violation of your registration with Microsoft or DarkSKy.

If you do not create this file, then a file with this name will be created in the pre-build step the first time the project is compiled so that the Visual Studio
build process can complete satisfactorily.

