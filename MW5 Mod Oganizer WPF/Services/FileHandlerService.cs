using System;
using System.IO;

namespace MW5_Mod_Organizer_WPF.Services
{
    public class FileHandlerService
    {
        public static void WriteFile(string path, string fileName, string content)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                File.WriteAllText(path + fileName, content);
            } catch (Exception ex)
            {
                LoggerService.AddLog("WriteFileException", ex.Message);
            }
        }

        public static string? ReadFile(string path, string fileName)
        {
            try
            {
                if (File.Exists(path + fileName))
                {
                    string content = File.ReadAllText(path + fileName);
                    return content;
                }
                else
                {
                    LoggerService.AddLog("ReadFileFunction", $"The given path \"{path + fileName}\"does not exist.");
                    return null;
                }
            }
            catch (Exception ex)
            {

                LoggerService.AddLog("ReadFileException", ex.Message);
                return null;
            }
        }

        public static string[]? GetSubDirectories(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    string[] dirs = Directory.GetDirectories(path);
                    return dirs;
                }
                else
                {
                    //LoggerService.AddLog("GetSubDirectoriesFunction", "The given path does not exist.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("GetSubDirectoriesException", ex.Message);
                return null;
            }
        }
    }
}
