using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BatPassAnalysisFW
{
    /// <summary>
    /// Universal wait cursor class
    /// </summary>
    public class WaitCursor : IDisposable
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private string _oldStatus = "null";
        private Cursor _previousCursor = Cursors.Arrow;
        private int Depth = 0;


        /// <summary>
        /// creates and displays a wait cursor which will revert when the class instance is disposed.
        /// Allows for nested calls.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="caller"></param>
        /// <param name="linenumber"></param>
        public WaitCursor(string status = "null", [CallerMemberName] string caller = null, [CallerLineNumber] int linenumber = 0)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {

                /* if (status != "null")
                 {
                     //(App.Current.MainWindow as MainWindow).Dispatcher.Invoke((Action)delegate
                     //var currentMainWindow = Application.Current.MainWindow;
                     //MainWindow window = (currentMainWindow as MainWindow);
                     //window.Dispatcher.Invoke(delegate
                     //{

                         Debug.WriteLine("-=-=-=-=-=-=-=-=- "+status+" -=-=-=-=-=-=-=-=-");
                         _oldStatus = MainWindow.SetStatusText(status);
                         //Debug.WriteLine("old Status=" + oldStatus);
                         _previousCursor = Mouse.OverrideCursor;
                         //Debug.WriteLine("old cursor saved");
                         Mouse.OverrideCursor = Cursors.Wait;
                         //Debug.WriteLine("Wait cursor set");

                     //});
                 }
                 else
                 {*/

                if (Mouse.OverrideCursor == null)
                {
                    var mw = Application.Current.MainWindow as Window;
                    if (mw != null)
                    {
                        mw.Dispatcher.Invoke(delegate
                        {
                            _previousCursor = Mouse.OverrideCursor;
                            Mouse.OverrideCursor = Cursors.Wait;
                            Debug.WriteLine(
                                $"%%%%%%%%%%%%%%%%%%%%%%%%%    WAIT - from {caller} at {linenumber} - {DateTime.Now.ToLongTimeString()}");
                        });
                    }
                }
                else
                {
                    Depth = 1;
                    Debug.WriteLine($"No wait cursor set from {caller}");
                }


                //Application.Current.MainWindow.Dispatcher.InvokeAsync(() => { Mouse.OverrideCursor = _previousCursor; },
                //System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("%%%%%%%%%%%%%%%%%  WaitCursor failed for \"" + status + "\":-" + ex.Message);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void Dispose()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        protected void Dispose(bool all)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (Depth == 0)
                {
                    
                    var mw = Application.Current.MainWindow as Window;
                    if (mw != null)
                    {
                        mw.Dispatcher.Invoke(delegate
                        {
                            //Mouse.OverrideCursor = _previousCursor ?? Cursors.Arrow;
                            Mouse.OverrideCursor = null;
                            Debug.WriteLine(
                                $"%-%-%-%-%-%-%_%-%-%-%-%-%-- RESUME {Mouse.OverrideCursor} at {DateTime.Now.ToLongTimeString()}");
                        });

                    }
                    else
                    {
                        Debug.WriteLine("No Main Window, failed to reset cursor");
                    }
                }
                else
                {
                    Debug.WriteLine("No cursor reset");
                }
                /*
                if (_oldStatus != "null")
                    //(App.Current.MainWindow as MainWindow).Dispatcher.Invoke((Action)delegate

                    MainWindow.SetStatusText(_oldStatus);*/


            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error disposing of wait cursor:- " + ex.Message);
            }
        }
    }
}
