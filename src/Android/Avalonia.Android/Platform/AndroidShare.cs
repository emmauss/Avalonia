﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Webkit;
using AndroidX.Core.Content;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using AndroidFile = Java.IO.File;

namespace Avalonia.Android.Platform
{
    internal class AndroidShare : IShareProvider
    {
        private readonly Context _context;

        public AndroidShare(Context context)
        {
            _context = context;
        }

        public bool CanShareAsync(IDataObject dataObject)
        {
            return dataObject is DataObject data && (data.Contains(DataFormats.Text) || data.Contains(DataFormats.Files));
        }

        private async Task ShareAsync(string text)
        {
            var intent = new Intent(Intent.ActionSend);
            intent.SetType("text/plain");
            intent.SetAction(Intent.ActionSend);
            intent.PutExtra(Intent.ExtraText, text);

            var shareIntent = Intent.CreateChooser(intent, "Sharing Text");
            _context.StartActivity(shareIntent);
        }

        private async Task ShareAsync(IList<IStorageFile> files)
        {
            IList<IParcelable> uris = new List<IParcelable>();

            string mimeType = null;

            foreach (var file in files)
            {
                if(file == null)
                {
                    continue;
                }

                if (file.Path != null)
                {
                    uris.Add(Uri.Parse(file.Path.AbsoluteUri));
                }

                var fileMimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(Path.GetExtension(file.Name).Remove(0, 1));
                if (mimeType == null)
                {
                    mimeType = fileMimeType;
                }
                else if(mimeType != fileMimeType)
                {
                    mimeType = "application/octet-stream";
                }
            }
            var urmi = (uris.FirstOrDefault() as Uri);

            var intent = new Intent(Intent.ActionSend);
            intent.SetType(mimeType);
            intent.SetAction(Intent.ActionSendMultiple);
            intent.PutParcelableArrayListExtra(Intent.ExtraStream, uris);
            intent.SetFlags(ActivityFlags.GrantReadUriPermission);

            var shareIntent = Intent.CreateChooser(intent, "Sharing File");
            _context.StartActivity(shareIntent);
        }

        private async Task ShareAsync(Stream? stream, string? tempName = "")
        {
            if (stream == null || (stream.CanSeek && stream.Length == 0) || !stream.CanRead)
            {
                return;
            }

            var cachePath = _context.GetExternalCacheDirs().FirstOrDefault()?.ToString();

            if(cachePath != null)
            {
                cachePath = Path.Combine(cachePath, "temp");
                Directory.CreateDirectory(cachePath);

                if (string.IsNullOrEmpty(tempName))
                {
                    var randomBuffer = new byte[8];
                    System.Random.Shared.NextBytes(randomBuffer);

                    tempName = System.BitConverter.ToString(randomBuffer).ToLower().Replace("-", "");
                }
                var tempFile = Path.Combine(cachePath, tempName);

                using var fileStream = File.Open(tempFile, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream);

                fileStream.Close();

                using var file = new AndroidFile(tempFile);

                var uri = FileProvider.GetUriForFile(_context, $"{_context.PackageName}.fileprovider", file);

                var intent = new Intent(Intent.ActionSend);
                intent.SetType("application/octet-stream");
                intent.SetAction(Intent.ActionSend);
                intent.SetData(uri);
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);

                var shareIntent = Intent.CreateChooser(intent, "Sharing File");
                _context.StartActivity(shareIntent);
            }
        }

        public async Task ShareAsync(IDataObject dataObject)
        {
            if(dataObject.Contains(DataFormats.Stream))
            {
                await ShareAsync(dataObject.GetStream(), dataObject.GetText());
            }
            else if (dataObject.Contains(DataFormats.Text))
            {
                await ShareAsync(dataObject.GetText());
            }
            else if (dataObject.Contains(DataFormats.Files))
            {
                var files = dataObject.GetFiles().Select( x => x as IStorageFile);
                await ShareAsync(files.ToList());
            }
        }
    }
}