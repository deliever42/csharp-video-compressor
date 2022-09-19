using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;

static class FileUtils
{
    public static string GetCurrentDirectory() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    public static DirectoryInfo CreateFolder(string path) => Directory.CreateDirectory(path);

    public static void Delete(string path) => Directory.Delete(path, true);

    public static void ExtractZipFile(string from, string to) => new FastZip().ExtractZip(from, to, null);
}
