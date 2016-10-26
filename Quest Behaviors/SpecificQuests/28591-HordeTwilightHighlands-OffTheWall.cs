// Behavior originally contributed by mastahg.
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

#region Summary and Documentation
#endregion


#region Examples
#endregion


#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


namespace Honorbuddy.Quest_Behaviors.SpecificQuests.OffTheWall
{
    [CustomBehaviorFileName(@"SpecificQuests\28591-HordeTwilightHighlands-OffTheWall")]
    public class OffTheWall : CustomForcedBehavior
    {
        public OffTheWall(Dictionary<string, string> args)
            : base(args)
        {
            QBCLog.BehaviorLoggingContext = this;

            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                QuestId = 28591;
                QuestRequirementComplete = QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = QuestInLogRequirement.InLog;
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                QBCLog.Exception(except);
                IsAttributeProblem = true;
            }
        }

        // DON'T EDIT THIS--it is auto-populated by Git
        public override string VersionId => QuestBehaviorBase.GitIdToVersionId("$Id$");


        // Attributes provided by caller
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private Composite _root;


        // Private properties
        private LocalPlayer Me
        {
            get { return (StyxWoW.Me); }
        }


        #region Overrides of CustomForcedBehavior

        public WoWUnit Marksmen
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 49124 && u.IsAlive).OrderBy(
                        u => u.Distance).FirstOrDefault();
            }
        }



        public WoWUnit Cannoner
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 49025 && u.IsAlive).OrderBy(
                        u => u.Distance).FirstOrDefault();
            }
        }

        public WoWUnit Cannon
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 49060 && u.IsAlive).OrderBy(
                        u => u.Distance).FirstOrDefault();
            }
        }


        private WoWUnit GetTurret()
        {
            return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => (!u.CharmedByUnitGuid.IsValid || u.CharmedByUnitGuid == Me.Guid) && u.Entry == 49135)
                .OrderBy(u => u.DistanceSqr).
                FirstOrDefault();
        }

        protected Composite CreateBehavior_QuestbotMain()
        {
            //return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(ShootArrows,Lazor, BunchUp, new ActionAlwaysSucceed())));
            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(new Action(ret => Loopstuff()))));
        }


        public void Loopstuff()
        {
            while (true)
            {
                ObjectManager.Update();
                if (Me.IsQuestComplete(QuestId))
                {
                    _isBehaviorDone = true;
                    break;
                }

                try
                {
                    if (!Query.IsInVehicle())
                    {
                        var turret = GetTurret();
                        if (turret != null)
                        {
                            if (turret.DistanceSqr > 5 * 5)
                            {
                                //Navigator.MoveTo(turret.Location);
                            }
                            else
                                turret.Interact();
                        }
                        else
                        {
                            QBCLog.Info("Unable to find turret");
                        }
                    }
                    else
                    {
                        if (Me.CurrentTarget != null &&
                            (Me.CurrentTarget.Distance < 60 || Me.CurrentTarget.InLineOfSight))
                        {
                            WoWMovement.ClickToMove(Me.CurrentTarget.Location);
                            //WoWMovement.ClickToMove(Me.CurrentTarget.Location.RayCast(Me.CurrentTarget.Rotation, 20));
                            var x = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(z => z.CharmedByUnit == Me);

                            Vector3 v = Vector3.Normalize(Me.CurrentTarget.Location - Me.Location);
                            Lua.DoString(
                                string.Format(
                                    "VehicleAimIncrement(({0} - VehicleAimGetAngle())); CastPetAction(1);CastPetAction(2);",
                                    Math.Asin(v.Z).ToString()));
                        }
                        else
                        {
                            if (!Me.IsQuestObjectiveComplete(QuestId, 1))
                            {
                                if (Marksmen != null)
                                    Marksmen.Target();
                            }
                            else if (!Me.IsQuestObjectiveComplete(QuestId, 2))
                            {
                                if (Cannoner != null)
                                    Cannoner.Target();
                            }
                            else if (!Me.IsQuestObjectiveComplete(QuestId, 3))
                            {
                                if (Cannon != null)
                                    Cannon.Target();
                            }
                        }
                    }
                }
                catch (Exception except)
                {
                    QBCLog.Exception(except);
                }
            }
        }


        public override void OnFinished()
        {
            TreeHooks.Instance.RemoveHook("Questbot_Main", CreateBehavior_QuestbotMain());
            TreeRoot.GoalText = string.Empty;
            TreeRoot.StatusText = string.Empty;
            base.OnFinished();
        }


        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }


        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Questbot_Main", 0, CreateBehavior_QuestbotMain());

                this.UpdateGoalText(QuestId);
            }
        }

        #endregion
    }
}