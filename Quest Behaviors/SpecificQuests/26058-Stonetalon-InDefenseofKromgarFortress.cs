using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


namespace Honorbuddy.Quest_Behaviors.SpecificQuests.InDefenseofKromgarFortress
{
    [CustomBehaviorFileName(@"SpecificQuests\26058-Stonetalon-InDefenseofKromgarFortress")]
    public class q26058 : CustomForcedBehavior
	{
		public q26058(Dictionary<string, string> args)
            : base(args){}
    
        
        public static LocalPlayer me = StyxWoW.Me;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
		public double angle = 0;
		public double CurentAngle = 0;
        public List<WoWUnit> mob1List
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => ((u.Entry == 42017 || u.Entry == 42016 || u.Entry == 42015) && !u.IsDead && u.X > 935 && u.Y > 5 && u.Z >= me.Z))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
		public List<WoWUnit> Turret
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>()
                                    .Where(u => (u.Entry == 41895))
                                    .OrderBy(u => u.Distance).ToList();
            }
        }
        private Composite _root;
        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
					
					
                    new Decorator(ret => me.QuestLog.GetQuestById(26058) !=null && me.QuestLog.GetQuestById(26058).IsCompleted,
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
							new Action(ret => Lua.DoString("VehicleExit()")),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                _isDone = true;
                                return RunStatus.Success;
                                }))
                            )),
					new Decorator(ret => !InVehicle,
						new Action(ret =>
						{
							if (Turret.Count > 0 && Turret[0].Location.Distance(me.Location) <= 5)
							{
								WoWMovement.MoveStop();
								Turret[0].Interact();
							}
							else if (Turret.Count > 0 && Turret[0].Location.Distance(me.Location) > 5)
							{
								Navigator.MoveTo(Turret[0].Location);
							}
						}
					)),		
					new Action(ret =>
					{
						Lua.DoString("CastPetAction({0})", 1);	
						if(mob1List.Count == 0)
							return;
						mob1List[0].Target();
							
						while (me.CurrentTarget != null && me.CurrentTarget.IsAlive && me.CurrentTarget.X > 935 && me.CurrentTarget.Y > 5)
						{
							WoWMovement.ConstantFace(me.CurrentTarget.Guid);
							angle = (me.CurrentTarget.Z - me.Z) / (me.CurrentTarget.Location.Distance(me.Location));
							CurentAngle = Lua.GetReturnVal<double>("return VehicleAimGetAngle()", 0);
							if (CurentAngle < angle)
							{
								Lua.DoString(string.Format("VehicleAimIncrement(\"{0}\")", (angle - CurentAngle)));
							}
							if (CurentAngle > angle)
							{
								Lua.DoString(string.Format("VehicleAimDecrement(\"{0}\")", (CurentAngle - angle)));
							}
							Lua.DoString("CastPetAction({0})", 1);
						}
					}
					)
                )
			);
        }

        

        
        

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

    }
}
