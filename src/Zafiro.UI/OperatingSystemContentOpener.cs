using System.Diagnostics;
using Zafiro.Reactive;
using Process = System.Diagnostics.Process;

#if ANDROID
using AndroidX.Core.Content;
using Android.Content;
using Android.Webkit;
using Java.Lang;
#endif

namespace Zafiro.UI;

public class OperatingSystemContentOpener : IContentOpener
{
    public Task<Result> Open(IObservable<byte> contents, string name)
    {
        return Result.Try(async () =>
        {
            var tempFileName = Path.Combine(Path.GetTempPath(), name);
            await using (var fileStream = File.Create(tempFileName))
            {
                await contents.WriteTo(fileStream);
            }

            return tempFileName;
        }).Bind(Open);
    }

    private Result Open(string filename)
    {
#if ANDROID
        return OpenAndroid(filename);
#else
        return OpenDesktop(filename);
#endif
    }

    private static Result OpenDesktop(string tempFileName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = tempFileName,
            UseShellExecute = true
        });

        return Result.Success();
    }

#if ANDROID
    private static Result OpenAndroid(string documentPath)
    {
        var file = new Java.IO.File(documentPath);

        var uri = Result.Try(() => FileProvider.GetUriForFile(Application.Context, Application.Context.PackageName + ".fileprovider", file), exception =>
        {
            if (exception is IllegalArgumentException)
            {
                return "The OperatingSystemContentOpener requires a File Provider to be added to you AndroidManifest.xml. See https://github.com/SuperJMN-Zafiro/Zafiro.UI/README.md";
            }

            return exception.Message;
        });

        return uri.Bind(uri1 =>
        {
            Intent intent = new(Intent.ActionView);
            intent.SetDataAndType(uri1, GetMimeType(uri1));
            intent.SetDataAndTypeAndNormalize(uri1, GetMimeType(uri1));
            intent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.NewTask);

            Application.Context.StartActivity(intent);
            return Result.Success();
        });
    }

    public static string? GetMimeType(Android.Net.Uri uri)
    {
        string? mimeType;
        if (ContentResolver.SchemeContent.Equals(uri.Scheme))
        {
            var cr = Application.Context.ContentResolver!;
            mimeType = cr.GetType(uri)!;
        }
        else
        {
            var fileExtension = MimeTypeMap.GetFileExtensionFromUrl(uri.ToString());
            mimeType = MimeTypeMap.Singleton!.GetMimeTypeFromExtension(fileExtension.ToLower());
        }
        return mimeType;
    }
#endif
}