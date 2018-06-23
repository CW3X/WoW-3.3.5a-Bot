﻿using AmeisenAI;
using AmeisenCore;
using AmeisenCore.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AmeisenBotGUI
{
    /// <summary>
    /// Interaktionslogik für mainscreenForm.xaml
    /// </summary>
    public partial class mainscreenForm : Window
    {
        private WoWExe wowExe;
        private DispatcherTimer uiUpdateTimer;

        public mainscreenForm(WoWExe wowExe)
        {
            InitializeComponent();
            this.wowExe = wowExe;
        }

        private void mainscreen_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "AmeisenBot - " + wowExe.characterName + " [" + wowExe.process.Id + "]";
            UpdateUI();

            uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Tick += new EventHandler(uiUpdateTimer_Tick);
            uiUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            uiUpdateTimer.Start();

            AmeisenAIManager.GetInstance().StartAI(4);
        }

        private void uiUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateUI();

            if (checkBoxFollowGroupLeader.IsChecked == true)
                AmeisenAIManager.GetInstance().AddActionToQueue(new AmeisenAction(AmeisenActionType.FOLLOW_GROUPLEADER, 8.0));
        }

        private void UpdateUI()
        {
            AmeisenManager.GetInstance().RefreshMe();
            Me me = AmeisenManager.GetInstance().GetMe();

            try
            {
                labelName.Content = me.name + " - lvl." + me.level + " - ";

                labelHP.Content = "HP [" + me.health + "/" + me.maxHealth + "]";
                progressBarHP.Maximum = me.maxHealth;
                progressBarHP.Value = me.health;

                labelEnergy.Content = "Energy [" + me.energy + "/" + me.maxEnergy + "]";
                progressBarEnergy.Maximum = me.maxEnergy;
                progressBarEnergy.Value = me.energy;

                labelXP.Content = "XP [" + me.exp + "/" + me.maxExp + "]";
                progressBarXP.Maximum = me.maxExp;
                progressBarXP.Value = me.exp;

                labelPosition.Content =
                    "X: " + me.posX +
                    "\nY: " + me.posY +
                    "\nZ: " + me.posZ +
                    "\nR: " + me.rotation;

                StringBuilder sb = new StringBuilder();

                foreach (Target t in me.partymembers)
                    sb.Append(t.ToShortString() + ",\n");

                labelDebugInfo.Content =
                    "- DebugInfo -\nTargetGUID: " + me.targetGUID +
                    "\nFactionTemplate: " + me.factionTemplate +
                    "\nMapID: " + me.mapID +
                    "\nZoneID: " + me.zoneID +
                    "\nPartyMembers { " + sb + "\n}";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                labelNameTarget.Content = me.target.name + " - lvl." + me.target.level + " - ";

                labelHPTarget.Content = "HP [" + me.target.health + "/" + me.target.maxHealth + "]";
                progressBarHPTarget.Maximum = me.target.maxHealth;
                progressBarHPTarget.Value = me.target.health;

                labelEnergyTarget.Content = "Energy [" + me.target.energy + "/" + me.target.maxEnergy + "]";
                progressBarEnergyTarget.Maximum = me.target.maxEnergy;
                progressBarEnergyTarget.Value = me.target.energy;

                labelDistance.Content = "Distance: " + me.target.distance + "m";

                labelPositionTarget.Content =
                    "X: " + me.target.posX +
                    "\nY: " + me.target.posY +
                    "\nZ: " + me.target.posZ +
                    "\nR: " + me.target.rotation;

                labelDebugInfoTarget.Content =
                    "- DebugInfo -\nTargetGUID:" + me.target.targetGUID +
                    "\nFactionTemplate:" + me.target.factionTemplate;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                labelThreadsActive.Content = "Active AI Threads: " + AmeisenAIManager.GetInstance().GetBusyThreadCount() + "/" + AmeisenAIManager.GetInstance().GetActiveThreadCount();
                progressBarBusyAIThreads.Maximum = AmeisenAIManager.GetInstance().GetActiveThreadCount();
                progressBarBusyAIThreads.Value = AmeisenAIManager.GetInstance().GetBusyThreadCount();
                listboxCurrentQueue.Items.Clear();
                foreach (AmeisenAction a in AmeisenAIManager.GetInstance().GetQueueItems())
                    listboxCurrentQueue.Items.Add(a.GetActionType() + " [" + a.GetActionParams() + "]");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void buttonMoveToTarget_Click(object sender, RoutedEventArgs e)
        {
            AmeisenAIManager.GetInstance().AddActionToQueue(new AmeisenAction(AmeisenActionType.LOOT_TARGET, null));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
            AmeisenAIManager.GetInstance().StopAI();
        }

        private void mainscreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
