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
        private static string[] VideoExts = { ".mp4", ".mov" };

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

        public static async Task SetValueIfEmpty(ExifTool et, IList<Tag> originTagList, string file, string[] names, string value)
        {
            foreach (var name in names)
            {
                var tag = originTagList.FirstOrDefault(t => t.Name == name);
                if (tag == null)
                {
                    await et.OverwriteTagsAsync(file, new[]
                    {
                        new SetOperation(new Tag(name, value))
                    }, FileWriteMode.OverwriteOriginal);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(tag.Value))
                {
                    continue;
                }

                await et.OverwriteTagsAsync(file, new[]
                {
                    new SetOperation(new Tag(name, value))
                }, FileWriteMode.OverwriteOriginal);
            }
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

        protected readonly string File;

        protected BaseMetadataModifier(string file)
        {
            File = file;
        }

        public async Task<string> Modify()
        {
            var et = new ExifTool(new ExifToolOptions());
            var tagList = (await et.GetTagsAsync(File)).ToList();

            var time = GetOriginalDate(tagList);

            if (time == "")
            {
                return "Cannot Found Date Meta Info";
            }

            await Update(et, tagList, time);

            await et.OverwriteTagsAsync(File, new List<Operation>()
            {
                new SetOperation(new Tag("FileModifyDate",time)),
                new SetOperation(new Tag("FileCreateDate",time)),

            }, FileWriteMode.OverwriteOriginal);

            return "";
        }

        protected abstract Task Update(ExifTool et, IList<Tag> originTagList, string time);

        private string GetOriginalDate(IList<Tag> tagList)
        {
            var time = MetadataModifierHelper.GetValue(tagList,
                GetTimeTagNames());

            if (time == "")
            {
                time = MetadataModifierHelper.GetDateFromFilePath(File);
                // time = MetadataModifierHelper.GetValue(tagList, new[] { "FileModifyDate", "FileCreateDate" });
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

        protected override Task Update(ExifTool et, IList<Tag> originTagList, string time)
        {
            return MetadataModifierHelper.SetValueIfEmpty(et, originTagList, File,
                new[] { "DateTimeOriginal", "CreateDate", "DateCreated" }, time);
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

        protected override Task Update(ExifTool et, IList<Tag> originTagList, string time)
        {
            return MetadataModifierHelper.SetValueIfEmpty(et, originTagList, File,
                new[] { "MediaCreateDate", "TrackCreateDate", "MediaModifyDate", "TrackModifyDate", "DateTimeOriginal" }, time);
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
