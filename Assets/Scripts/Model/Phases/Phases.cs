﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainPhases;
using SubPhases;
using Players;
using System;

public static partial class Phases
{

    private static GameManagerScript Game;

    public static GenericPhase CurrentPhase { get; set; }
    public static GenericSubPhase CurrentSubPhase { get; set; }

    private static bool inTemporarySubPhase;
    public static bool InTemporarySubPhase
    {
        get { return CurrentSubPhase.isTemporary; }
    }

    public static PlayerNo PlayerWithInitiative = PlayerNo.Player1;

    private static PlayerNo currentPhasePlayer;
    public static PlayerNo CurrentPhasePlayer
    {
        get { return CurrentSubPhase.RequiredPlayer; }
    }

    private static List<System.Type> subPhasesToStart = new List<System.Type>();
    private static List<System.Type> subPhasesToFinish = new List<System.Type>();

    // EVENTS
    public delegate void EventHandler();
    public static event EventHandler OnRoundStart;
    public static event EventHandler OnSetupPhaseStart;
    public static event EventHandler OnPlanningPhaseStart;
    public static event EventHandler OnActivationPhaseStart;
    public static event EventHandler OnCombatPhaseStart;
    public static event EventHandler OnEndPhaseStart;

    public static event EventHandler OnActionSubPhaseStart;

    // PHASES CONTROL

    public static void StartPhases()
    {
        Game = GameObject.Find("GameManager").GetComponent<GameManagerScript>();

        CurrentPhase = new SetupPhase();
        Game.UI.AddTestLogEntry("Game is started");
        CurrentPhase.StartPhase();
    }

    public static void FinishSubPhase(System.Type subPhaseType)
    {
        if (CurrentSubPhase.GetType() == subPhaseType)
        {
            Debug.Log("Phase is finished directly");
            Next();
        }
        else
        {
            Debug.Log("Oops! You want to finish wrong phase!");
            if (!subPhasesToFinish.Contains(subPhaseType))
            {
                Debug.Log("Phase is planned to finish");
                subPhasesToFinish.Add(subPhaseType);
            }
        }
    }

    public static void Next()
    {
        CurrentSubPhase.Next();
    }

    public static void NextPhase()
    {
        CurrentPhase.NextPhase();
    }

    public static void CallNextSubPhase()
    {
        CurrentSubPhase.CallNextSubPhase();
    }

    public static void CheckScheduledChanges()
    {
        CheckScheduledFinishes();
        CheckScheduledStarts();
    }

    private static void CheckScheduledFinishes()
    {
        if (subPhasesToFinish.Count != 0)
        {
            List<System.Type> tempList = new List<System.Type>();
            foreach (var subPhaseType in subPhasesToFinish)
            {
                tempList.Add(subPhaseType);
            }

            foreach (var subPhaseType in tempList)
            {
                if (CurrentSubPhase.GetType() == subPhaseType)
                {
                    subPhasesToFinish.Remove(subPhaseType);
                    Next();
                }
            }
        }
    }

    private static void CheckScheduledStarts()
    {
        if (!InTemporarySubPhase)
        {
            if (subPhasesToStart.Count != 0)
            {
                StartTemporarySubPhase("SCHEDULED", subPhasesToStart[0]);
                subPhasesToStart.RemoveAt(0);
            }
        }
    }

    public static IEnumerator WaitForTemporarySubPhasesFinish()
    {
        while (Phases.CurrentSubPhase.isTemporary)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    // TRIGGERS

    public static void CallRoundStartTrigger()
    {
        if (OnSetupPhaseStart != null) OnRoundStart();
    }

    public static void CallSetupPhaseTrigger()
    {
        if (OnSetupPhaseStart != null) OnSetupPhaseStart();
    }

    public static void CallPlanningPhaseTrigger()
    {
        if (OnPlanningPhaseStart != null) OnPlanningPhaseStart();
    }

    public static void CallActivationPhaseTrigger()
    {
        if (OnActivationPhaseStart != null) OnActivationPhaseStart();
    }

    public static void CallCombatPhaseTrigger()
    {
        if (OnCombatPhaseStart != null) OnCombatPhaseStart();
        foreach (var shipHolder in Roster.AllShips)
        {
            shipHolder.Value.CallOnCombatPhaseStart();
        }
        Game.StartCoroutine(ResolveCombatTriggers());
    }

    private static IEnumerator ResolveCombatTriggers()
    {
        yield return Triggers.ResolveAllTriggers(TriggerTypes.OnCombatPhaseStart);
        Debug.Log("All pre-Combat Triggers are resolved, START OF COMBAT!");
        CurrentSubPhase.Initialize();
    }

    public static void CallEndPhaseTrigger()
    {
        if (OnEndPhaseStart != null) OnEndPhaseStart();
    }

    public static void CallOnActionSubPhaseTrigger()
    {
        if (OnActionSubPhaseStart != null) OnActionSubPhaseStart();
    }

    // TEMPORARY SUBPHASES

    public static void StartTemporarySubPhase(string name, System.Type subPhaseType)
    {
        if (!InTemporarySubPhase)
        {
            Debug.Log("Temporary phase is started directly");
            GenericSubPhase previousSubPhase = CurrentSubPhase;
            CurrentSubPhase = (GenericSubPhase)System.Activator.CreateInstance(subPhaseType);
            CurrentSubPhase.Name = name;
            CurrentSubPhase.PreviousSubPhase = previousSubPhase;
            CurrentSubPhase.RequiredPlayer = previousSubPhase.RequiredPlayer;
            CurrentSubPhase.RequiredPilotSkill = previousSubPhase.RequiredPilotSkill;
            CurrentSubPhase.Start();
        }
        else
        {
            Debug.Log("Temporary phase is delayed");
            subPhasesToStart.Add(subPhaseType);
        }
    }

    // INITIATIVE

    public static void DeterminePlayerWithInitiative()
    {
        int costP1 = Roster.GetPlayer(PlayerNo.Player1).SquadCost;
        int costP2 = Roster.GetPlayer(PlayerNo.Player2).SquadCost;

        if (costP1 < costP2)
        {
            PlayerWithInitiative = PlayerNo.Player1;
        }
        else if (costP1 > costP2)
        {
            PlayerWithInitiative = PlayerNo.Player2;
        }
        else
        {
            int randomPlayer = UnityEngine.Random.Range(1, 3);
            PlayerWithInitiative = Tools.IntToPlayer(randomPlayer);
        }

        CurrentSubPhase.RequiredPlayer = PlayerWithInitiative;
        StartTemporarySubPhase("Initiative", typeof(InitialiveDecisionSubPhase));
    }

    private class InitialiveDecisionSubPhase : DecisionSubPhase
    {

        public override void Prepare()
        {
            infoText = "Player " + Tools.PlayerToInt(PlayerWithInitiative) + ", what player will have initiative?";

            decisions.Add("I", StayWithInitiative);
            decisions.Add("Opponent", GiveInitiative);

            defaultDecision = "Opponent";
        }

        private void GiveInitiative(object sender, EventArgs e)
        {
            PlayerWithInitiative = Roster.AnotherPlayer(PlayerWithInitiative);
            ConfirmDecision();
        }

        private void StayWithInitiative(object sender, EventArgs e)
        {
            ConfirmDecision();
        }

        private void ConfirmDecision()
        {
            Messages.ShowInfo("Player " + Tools.PlayerToInt(PlayerWithInitiative) + " has Initiative");
            Phases.Next();
        }

    }

}


