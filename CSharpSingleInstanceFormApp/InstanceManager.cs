/*
  CSharpSingleInstanceFormApp - A single instance C# sample app

 Copyright (c) 2016 Elisha Kendagor kosistudio@live.com

 Permission is hereby granted, free of charge, to any person obtaining
 a copy of this software and associated documentation files (the
 "Software"), to deal in the Software without restriction, including
 without limitation the rights to use, copy, modify, merge, publish,
 distribute, sublicense, and/or sell copies of the Software, and to
 permit persons to whom the Software is furnished to do so, subject to
 the following conditions:

 The above copyright notice and this permission notice shall be
 included in all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

namespace CSharpSingleInstanceFormApp
{
    public static class InstanceManager
    {
        private class InstanceCleanUp
        {
            ~InstanceCleanUp()
                {
                    InstanceManager.CleanupInstance();
                }
        }

        private static string m_SingleAppComEventName = "global\"Test123AppInstance";
        private static string m_NamedPipeServerId = "Test123PipeServer";
        
        private static Guid m_InstanceId = Guid.NewGuid();
        private static BackgroundWorker m_SingleAppComThread = null;
        private static EventWaitHandle m_ThreadComEvent = null;
        private static InstanceCleanUp m_InstanceCleanup = new InstanceCleanUp();
        private static SingleInstanceFormMain m_MainForm = null;

        delegate void SetMainFormVisibleDelegate(Tuple<SingleInstanceFormMain, string> objParam);
        
        private static void StartNamedPipeServer(int numAttempts = 0)
        {
            if (numAttempts == 3)
            {
                return;
            }

            try
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(m_NamedPipeServerId))
                {
                    // Started a named pipe server to send cmdline to the main instance.
                    // Waiting for the main instance to connect...",
                    pipeServer.WaitForConnection();

                    //Main instance established a connection. Sending cmdline...
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        var args = Environment.GetCommandLineArgs();
                        var cmdline = Environment.CommandLine.Replace(args[0], m_InstanceId.ToString());
                        sw.WriteLine(cmdline);
                    }
                }
            }
            catch(InvalidOperationException oe)
            {
                // There are multiple 2nd instances clashing at the same time. Try again
                Thread.Sleep(2000);
                StartNamedPipeServer(numAttempts + 1);
            }
            catch(Exception ex)
            {
                // Unexpected exception managing application named-pipe server
            }

        }

        private static bool InstanceAlreadyExists()
        {
            try
            {
                // Checking for another instance of the application...
                // Open if another instance is already running. It throws if there's none
                m_ThreadComEvent = EventWaitHandle.OpenExisting(m_SingleAppComEventName);

                // Signal that main instance
                m_ThreadComEvent.Set();
                m_ThreadComEvent.Close();

                // Start the Named Pipe server so that the cmdline parameters
                // of this instance can be passed to the main instance.
                // The main instance, once signalled, will initiate the chat with this instance
                StartNamedPipeServer();

                return true;
            }
            catch (WaitHandleCannotBeOpenedException ex)
            {
                // Expected: No other instance of the application was detected
            }
            catch (Exception ex)
            {
                // Unexpected: Assume no other instance anyway.
                // Log or do something with this
            }

            return false;
        }

        private static void SingleAppComThreadDoWorkEventHandler(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Entered the inter-app COM thread DoWork event...
                
                // Only the main instance executes this handler
                
                // The main instance will check every second if it has been signalled
                // by another newly launched instance

                // The main instance will pick up the command-line arguments from the
                // other instance and do the work

                BackgroundWorker worker = sender as BackgroundWorker;
                
                WaitHandle[] waitHandles = new WaitHandle[] {m_ThreadComEvent };

                while (!worker.CancellationPending)
                {
                    // Check every second for a signal if the user started another instance
                    if (WaitHandle.WaitAny(waitHandles, 1000) == 0)
                    {
                        string extCmdline = null;

                        // Wait for command-line arguments to be sent through the named pipe
                        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(m_NamedPipeServerId))
                        {

                            // Named pipe client connecting to the other new instance ...",

                            try
                            {
                                pipeClient.Connect(5000);

                                // Named pipe client connected to the other instance. Waiting to receive cmdline params",

                                using (StreamReader sr = new StreamReader(pipeClient))
                                {
                                    extCmdline = sr.ReadLine();
                                    // Main instance has the new instance command line args
                                }
                            }
                            catch(TimeoutException)
                            {
                                // Problem: Timed-out waiting for the other new instance to create a named pipe server
                                // This can be retried perhaps
                            }
                            catch(Exception ex)
                            {
                                // Problem: Exception managing application named-pipe client
                                // Handle
                            }
                        }

                        if (MainForm != null)
                        {
                            MainForm.Invoke(
                                new SetMainFormVisibleDelegate((Tuple<SingleInstanceFormMain,string> objParam) =>
                                {
                                    SingleInstanceFormMain delMainForm = objParam.Item1;
                                    string cmdLine = objParam.Item2;
                                    if ((string.IsNullOrWhiteSpace(cmdLine))
                                        || (cmdLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length <= 1))
                                    {
                                        // No parameters were passed by the remote instance.
                                        // If the main instance was minimized or hidden to systray
                                        // or not visible otherwise, bring it up

                                        // Make the main form visible
                                        delMainForm.ShowForm();
                                        SetForegroundWindow(delMainForm.Handle);
                                    }
                                    else
                                    {
                                        // Not forcing the main form to show.
                                        // Handling the cmdline arguments received from the remote instance quietly
                                        delMainForm.HandleCmdlineParams(cmdLine);
                                    }

                                }), new Tuple<SingleInstanceFormMain, string>(MainForm, extCmdline));
                        }
                        else
                        {
                            // Problem: No main form detected: m_MainForm is null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Problem: Unexpected
                // Handle
            }
        }

        public static SingleInstanceFormMain MainForm
        {
            get 
            {
                if (m_MainForm == null)
                {
                    var timeout = DateTime.Now;
                    // Give the main form time to initialize
                    while ((m_MainForm == null && DateTime.Now.Subtract(timeout).Seconds < 5)
                           || (m_MainForm != null && m_MainForm.IsHandleCreated == false))
                    {
                        Thread.SpinWait(10000);
                    }
                }

                return m_MainForm; 
            }
            set { m_MainForm = value; }
        }

        public static void CleanupInstance()
        {
            try
            {
                // Cleaning up this main application instance...",

                if (m_SingleAppComThread != null)
                {
                    // Stop the inter-app COM thread:
                    m_SingleAppComThread.CancelAsync();

                    // Close the event handle so that another instance can start again:
                    if (m_ThreadComEvent != null)
                    {
                        m_ThreadComEvent.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // Problem: Unexpected
                // Handle
            }
        }

        public static bool RegisterInstance()
        {
            bool res = false;
            try
            {
                if (!InstanceAlreadyExists())
                {
                    // Registering this as the main instance..,

                    // Create the event handle
                    m_ThreadComEvent = new EventWaitHandle(false, EventResetMode.AutoReset, m_SingleAppComEventName);

                    // Create the inter-app COM thread
                    m_SingleAppComThread = new BackgroundWorker();
                    m_SingleAppComThread.WorkerReportsProgress = false;
                    m_SingleAppComThread.WorkerSupportsCancellation = true;
                    m_SingleAppComThread.DoWork += new DoWorkEventHandler(SingleAppComThreadDoWorkEventHandler);
                    m_SingleAppComThread.RunWorkerAsync(m_InstanceId);
                                        
                    res = true;
                }
            }
            catch (Exception ex)
            {
                // Unexpected
                // Handle
            }

            return res;
        }

        // Win API: Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
