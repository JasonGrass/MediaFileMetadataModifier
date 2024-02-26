using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MediaFileMetadataModifier
{
    internal class ModifyHandler
    {
        private readonly ModifyOptions _options;

        public Action<string>? PrintLog { get; set; }

        public ModifyHandler(ModifyOptions options)
        {
            _options = options;
        }

        public async Task Handle()
        {
            var files = ReadFiles(_options.FolderPath);

            foreach (var file in files)
            {
                await Modify(file);

            }
        }

        private async Task Modify(string file)
        {
            try
            {
                var result = await new MetadataModifier(file).Modify();
                if (result != "")
                {
                    PrintLog?.Invoke($"{result} {file}");
                }
            }
            catch (Exception e)
            {
                PrintLog?.Invoke($"[{e.GetType().Name}] {e.Message} {file}");
            }
        }

        private IList<string> ReadFiles(string path)
        {
            var files = Directory.GetFiles(path).ToList();
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                var f = ReadFiles(dir);
                files.AddRange(f);
            }

            return files;
        }

    }
}
