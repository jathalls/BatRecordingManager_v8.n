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
