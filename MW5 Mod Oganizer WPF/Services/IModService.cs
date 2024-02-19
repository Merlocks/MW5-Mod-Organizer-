using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Services
{
    public interface IModService
    {

        void GetMods();

        void AddMod(ModViewModel mod);

        void ClearConflictWindow();

        void ClearTempList();

        void CheckForConflicts(ModViewModel input);

        Task CheckForAllConflictsAsync();

        void GenerateManifest(ModViewModel input);
    }
}
