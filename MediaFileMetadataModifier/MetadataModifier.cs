using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NExifTool;
using NExifTool.Writer;

namespace MediaFileMetadataModifier
{
    internal class MetadataModifier
    {
        private readonly string _file;

        private static string[] ImageExts = { ".png", ".jpg", ".jpeg", ".gif" };
        private static string[] VideoExts = { ".mp4",".mov" };

        public MetadataModifier(string file)
        {
            _file = file;
        }

        public async Task<string> Modify()
        {
            if (IsFile(_file, ImageExts))
            {
                return await new ImageMetadataModifier(_file).Modify();
            }

            if (IsFile(_file, VideoExts))
            {
                return await new VideoMetadataModifier(_file).Modify();
            }
            
            return "Unknown File Extension";
        }

        private static bool IsFile(string file, string[] extensions)
        {
            var fileExt = Path.GetExtension(file);
            return extensions.Any(e => string.Equals(fileExt, e, StringComparison.OrdinalIgnoreCase));
        }

    }

    class MetadataModifierHelper
    {
        public static string GetValue(IList<Tag> tagList, string[] names)
        {
            foreach (var name in names)
            {
                var tag = tagList.FirstOrDefault(t => t.Name == name);
                if (tag == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(tag.Value))
                {
                    return tag.Value;
                }
            }

            return "";
        }

        /// <summary>
        /// 从文件路径中，提取照片的拍摄日期。
        /// 这里不具备普遍性，后续使用需要根据实际情况修改。
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetDateFromFilePath(string filePath)
        {
            string pattern = @"(\d{4})年(\d{2})月";
            Match match = Regex.Match(filePath, pattern);

            if (match.Success)
            {
                string year = match.Groups[1].Value;
                string month = match.Groups[2].Value;

                return $"{year}:{month}:{01} {00}:{00}:{00}+08:00";
            }
            else
            {
                return "";
            }
        }
    }

    abstract class BaseMetadataModifier
    {

        private readonly string _file;

        protected BaseMetadataModifier(string file)
        {
            _file = file;
        }

        public async Task<string> Modify()
        {
            var et = new ExifTool(new ExifToolOptions());
            var tagList = await et.GetTagsAsync(_file);

            var time = GetOriginalDate(tagList);

            if (time == "")
            {
                return "Cannot Found Date Meta Info";
            }

            await et.OverwriteTagsAsync(_file, new List<Operation>()
            {
                new SetOperation(new Tag("FileModifyDate",time)),
                new SetOperation(new Tag("FileCreateDate",time)),

            }, FileWriteMode.OverwriteOriginal);

            return "";
        }

        private string GetOriginalDate(IEnumerable<Tag> tagList)
        {
            var time = MetadataModifierHelper.GetValue(tagList.ToList(),
                GetTimeTagNames());

            if (time == "")
            {
                time = MetadataModifierHelper.GetDateFromFilePath(_file);
            }

            return time;
        }

        protected abstract string[] GetTimeTagNames();

    }

    class ImageMetadataModifier : BaseMetadataModifier
    {

        public ImageMetadataModifier(string file) : base(file)
        {
        }

        protected override string[] GetTimeTagNames()
        {
            return new[]
            {
                "DateTimeOriginal", "CreateDate", "DateCreated", "GPSDateTime", "MetadataDate",
                "DigitalCreationDateTime", "DateTimeCreated"
            };
        }

  
    }

    class VideoMetadataModifier : BaseMetadataModifier
    {
        public VideoMetadataModifier(string file) : base(file)
        {
        }

        protected override string[] GetTimeTagNames()
        {
            return new[]
            {
                "MediaCreateDate", "TrackCreateDate", "MediaModifyDate", "TrackModifyDate", "DateTimeOriginal",
                "CreateDate", "ModifyDate"
            };
        }
    }

}
