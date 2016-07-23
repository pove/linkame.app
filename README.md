# linkame.app
Linkame Android App with [Visual Studio Community](https://www.visualstudio.com/products/visual-studio-community-vs) and [Xamarin Platform](https://www.xamarin.com/platform)

## Install Prerequisites
Download and install Visual Studio Community and Xamarin Platform from [Xamarin download page](https://www.xamarin.com/download).

## Setup
You only need to set the Rest service URL. Open `linkame/RestService.cs` and change the next code with your own:

```c#
...
public static class RestService
  {
    // Links rest api url
    private const string url = "http://yourlinkamerestserviceurl/";
    // Or use your local virtual host: "http://192.168.0.X/linkame/api/public/";
...
```
