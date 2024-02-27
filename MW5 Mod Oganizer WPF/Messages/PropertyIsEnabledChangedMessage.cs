using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MW5_Mod_Organizer_WPF.Messages
{
    public sealed class PropertyIsEnabledChangedMessage : ValueChangedMessage<bool>
    {
        public PropertyIsEnabledChangedMessage(bool value) : base(value)
        {
            
        }
    }
}
