using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SoulsIds.Events;

namespace FogMod
{
    public class EventConfig
    {
        public List<NewEvent> NewEvents { get; set; }
        public List<EventSpec> Events { get; set; }

        public class NewEvent
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Comment { get; set; }
            public List<string> Commands { get; set; }
        }

        public class EventSpec : AbstractEventSpec
        {
            public List<EventTemplate> Template { get; set; }
        }

        // TODO: after abyss watchers
        public class EventTemplate
        {
            // Fog gate entity id. The edits are for making it permanent and removing traversal.
            public string Fog { get; set; }
            // Event only manages sfx for fog gate, so is not necessary to do manually
            public string FogSfx { get; set; }
            // Warp exit id, with the area name plus the warp entity id
            public string Warp { get; set; }
            // The sfx that is used for the fog gate, for permanently recreating it
            public string Sfx { get; set; }
            // The flag that is expected to be set going through the fog gate, if SetFlagIf is off
            public string SetFlag { get; set; }
            // The flag that normally disables going through the fog gate
            public string SetFlagIf { get; set; }
            // The area for setting the flag
            public string SetFlagArea { get; set; }
            // Non-null if the warp should be replaced
            public string WarpReplace { get; set; }
            // A repeatable warp which should be added
            public int RepeatWarpObject { get; set; }
            // The flag for the repeatable warp being usable (usually after it has triggered once before)
            public int RepeatWarpFlag { get; set; }
            // If copying this event to another one, the target event id
            public int CopyTo { get; set; }

            // What to remove, or 'all' for the entire event initialization
            public string Remove { get; set; }
            // What to add
            public List<EventAddCommand> Add { get; set; }
            // What to replace
            public string Replace { get; set; }
        }
    }
}
