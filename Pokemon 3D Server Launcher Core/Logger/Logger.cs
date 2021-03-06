﻿using Amib.Threading;
using Modules.System;
using Modules.System.IO;
using Pokemon_3D_Server_Launcher_Core.Interfaces;
using System;
using System.IO;
using System.Text;

namespace Pokemon_3D_Server_Launcher_Core.Logger
{
    public sealed class Logger
    {
        private Core Core;
        private StreamWriter Writer;
        private IWorkItemsGroup ThreadPool = new SmartThreadPool().CreateWorkItemsGroup(1, new WIGStartInfo() { StartSuspended = true });
        private bool IsActive = false;

        public event EventHandler<LoggerEventArgs> OnLogMessageReceived;

        internal Logger(Core core)
        {
            Core = core;
            IsActive = false;
            Log("Logger Initialized.");
        }

        internal void Start()
        {
            try
            {
                Writer = new StreamWriter(new FileStream($"{Core.Settings.Directories.LoggerDirectory}/Logger_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.dat".GetFullPath(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ex.CatchError();
            }

            IsActive = true;
            ThreadPool.Start();
        }

        internal void Log(string message, string type = "Info", bool printToConsole = true, bool writeToLog = true)
        {
            ThreadPool.QueueWorkItem(() =>
            {
                if (CanLog(type) && IsActive)
                    InternalLog($"{DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")} [Core] [{type}] {message}", printToConsole, writeToLog);
            });
        }

        public void Log(ICore instance, string message, string type, bool printToConsole = true, bool writeToLog = true)
        {
            ThreadPool.QueueWorkItem(() =>
            {
                if (CanLog(instance, type) && IsActive)
                    InternalLog($"{DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")} [{instance.ModuleName}] [{type}] {message}", printToConsole, writeToLog);
            });
        }

        private bool CanLog(string type)
        {
            if (Core.Settings.LogTypes != null)
            {
                if (Core.Settings.LogTypes.ContainsKey(type))
                    return Core.Settings.LogTypes[type];
            }

            return false;
        }

        private bool CanLog(ICore instance, string type)
        {
            if (instance.Settings.LogTypes != null)
            {
                if (instance.Settings.LogTypes.ContainsKey(type))
                    return instance.Settings.LogTypes[type];
            }

            return false;
        }

        private void InternalLog(string message, bool printToConsole = true, bool writeToLog = true)
        {
            if (printToConsole)
                OnLogMessageReceived.BeginInvoke(this, new LoggerEventArgs(message), null, null);

            if (writeToLog && Writer != null)
            {
                Writer.WriteLine(message);
                Writer.Flush();
            }
        }

        internal void Dispose()
        {
            if (IsActive)
            {
                IsActive = false;
                ThreadPool.WaitForIdle();

                if (Writer != null)
                    Writer.Dispose();
            }
        }
    }
}