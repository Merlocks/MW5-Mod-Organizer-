using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MW5_Mod_Organizer_WPF.Facades
{
    public sealed class JsonConverterFacade
    {
        public static Mod? JsonToMod(string path)
        {
            try
            {
                string? jsonString = FileHandlerService.ReadFile(path, @"\mod.json");
                if (jsonString != null) 
                {
                    Mod? mod = JsonHandlerService.JsonStringToMod(jsonString);

                    return mod;
                } else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("JsonToModException", ex.Message);
                throw;
            }
        }

        public static void ModToJson(string path, Mod mod)
        {
            try
            {
                string? jsonString = JsonHandlerService.ModToJsonString(mod);
                if (jsonString != null) 
                {
                    FileHandlerService.WriteFile(path, @"\mod.json", jsonString);
                }
                
            } catch (Exception ex)
            {
                LoggerService.AddLog("ModToJsonException", ex.Message);
                throw;
            }
        }

        public static void Createbackup(string path)
        {
            try
            {
                string? jsonString = FileHandlerService.ReadFile(path, @"\mod.json");

                if (jsonString != null)
                {
                    FileHandlerService.WriteFile(path, @"\backup.json", jsonString);
                }

            }
            catch (Exception ex)
            {
                LoggerService.AddLog("ModToJsonException", ex.Message);
                throw;
            }
        }

        public static Mod? ReadBackup(string path)
        {
            try
            {
                string? jsonString = FileHandlerService.ReadFile(path, @"\backup.json");
                if (jsonString != null)
                {
                    Mod? mod = JsonHandlerService.JsonStringToMod(jsonString);

                    return mod;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("JsonToModException", ex.Message);
                throw;
            }
        }

        public static ModList? JsonToModList(string path)
        {
            try
            {
                string? jsonString = FileHandlerService.ReadFile(path, @"\modlist.json");
                if (jsonString != null)
                {
                    ModList? modList = JsonHandlerService.JsonStringToModList(jsonString);

                    return modList;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("JsonToModListException", ex.Message);
                throw;
            }
        }

        public static void ModListToJson(string path, ModList modList)
        {
            try
            {
                string? jsonString = JsonHandlerService.ModListToJsonString(modList);
                if (jsonString != null)
                {
                    FileHandlerService.WriteFile(path, @"\modlist.json", jsonString); 
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("ModListToJsonException", ex.Message);
                throw;
            }
        }
    }
}
