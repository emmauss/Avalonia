﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Web.Interop;
using Avalonia.Web.Storage;
using static System.Net.Mime.MediaTypeNames;

namespace Avalonia.Web
{
    [SupportedOSPlatform("browser")]
    internal class BrowserShare : IShare
    {
        public void Share(string text)
        {
            using var jsObject = DomHelper.Create(default);
            jsObject?.SetProperty("title", $"Sending {text}");
            jsObject?.SetProperty("text", text);

            if (ShareHelper.CanShare(jsObject))
            {
                ShareHelper.Share(jsObject);
            }
        }

        public void Share(IStorageFile file)
        {
            Share(new[] { file });
        }

        public void Share(IList<IStorageFile> files)
        {
            ShareHelper.shareFileList($"Sending {files.Count} file{( files.Count > 0 ? "s" : "")}", files.Select(f => (f as JSStorageItem).FileHandle).ToArray());
        }
    }
}
