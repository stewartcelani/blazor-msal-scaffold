using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace Blazor.Msal.Maui;

[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataHost = "auth",
    DataScheme = "msalb668e2c4-a6cc-47a0-ac6a-08d02f798579")]
public class MsalActivity : BrowserTabActivity
{
}