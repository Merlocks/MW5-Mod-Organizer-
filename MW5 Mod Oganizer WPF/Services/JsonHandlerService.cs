using System;
using System.Text.Json;
using MW5_Mod_Organizer_WPF.Models;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class JsonHandlerService
    {
        public static Mod? JsonStringToMod(string jsonString)
        {
            try
            {
                Mod? mod = JsonSerializer.Deserialize<Mod>(jsonString);
                return mod;
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("JsonStringToModException", ex.Message);
                return null;
            }
        }

        public static string? ModToJsonString(Mod mod)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(mod, options);

                return jsonString;
            } catch (Exception ex)
            {
                LoggerService.AddLog("ModToJsonStringException", ex.Message);
                return null;
            }
        }

        public static ModList? JsonStringToModList(string jsonString)
        {
            try
            {
                ModList? modList = JsonSerializer.Deserialize<ModList>(jsonString);
                return modList;
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("JsonStringToModListException", ex.Message);
                return null;
            }
        }

        public static string? ModListToJsonString(ModList modList)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(modList, options);

                return jsonString;
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("ModListToJsonStringException", ex.Message);
                return null;
            }
        }
    }
}
