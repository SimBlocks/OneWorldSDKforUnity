//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace sbio.OneWorldSDKViewer
{
  public static class ScreenshotManager
  {
    public static FileInfo TakeScreenshot(string screenshotDir)
    {
      return TakeScreenshot(new DirectoryInfo(screenshotDir));
    }

    public static FileInfo TakeScreenshot(DirectoryInfo screenshotDir)
    {
      var renderTexture = new RenderTexture(1920, 1080, 24);
      var screenShot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
      try
      {
        var oldRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        try
        {
          foreach (var camera in Camera.allCameras.Where(c => c.enabled).OrderBy(c => c.depth))
          {
            var oldTexture = camera.targetTexture;
            var oldmsaa = camera.allowMSAA;
            camera.targetTexture = renderTexture;
            camera.allowMSAA = false;

            try
            {
              camera.Render();
            }
            finally
            {
              camera.allowMSAA = oldmsaa;
              camera.targetTexture = oldTexture;
            }
          }

          //NOTE: ReadPixels reads from the currently active RenderTexture
          screenShot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
          screenShot.Apply();
        }
        finally
        {
          RenderTexture.active = oldRT;
        }
      }
      finally
      {
        UnityEngine.Object.Destroy(renderTexture);
      }

      {
        //NOTE: On Unity's runtime, attempting to create certain directories eg 'C:\' will fail even if they already exist
        var dir = screenshotDir;
        if (!dir.Exists)
        {
          dir.Create();
        }
      }

      //Find the last screenshot we took today
      var lastScreenshotNum = -1;
      var dateStr = DateTime.Now.ToString("yyy-MM-dd");
      foreach (var file in screenshotDir.GetFiles(string.Format("{0}_*.png", dateStr)))
      {
        var match = sc_ScreenshotRegex.Match(file.Name);
        if (match.Success)
        {
          lastScreenshotNum = Math.Max(int.Parse(match.Groups[1].Value), lastScreenshotNum);
        }
      }

      var nextScreenshotNum = lastScreenshotNum + 1;

      var bytes = screenShot.EncodeToPNG();
      var screenShotFile = new FileInfo(Path.Combine(screenshotDir.FullName, string.Format("{0}_{1:00000}.png", dateStr, nextScreenshotNum)));
      File.WriteAllBytes(screenShotFile.FullName, bytes);

      return screenShotFile;
    }

    private static readonly Regex sc_ScreenshotRegex = new Regex(@".*_(\d*)");
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
