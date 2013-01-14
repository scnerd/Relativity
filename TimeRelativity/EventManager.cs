/*
 * David Maxson
 * Relativity Engine
 * 1/13/13
 * scnerd@gmail.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TimeRelativity
{
    /// <summary>
    /// This class manages events that occur on a relativistic timeline
    /// Essentially, the program can, in real-time, send "events" into an
    ///  Event Manager, which will record both the event and the time that
    ///  it occured, "time" being relative to when the Manager was started.
    /// An event can be anything, whether a keypress, mouse movement,
    ///  destruction of a unit (in an RTS), or really anything that changes
    ///  the flow of the program.
    /// The user code must be able to do two things:
    ///  1) In real-time, calculate the status of the program based on
    ///  events in the past. Both the time of the events and the current
    ///  time are available from the manager.
    ///  2) Be able to set the status of the program (and any relevant
    ///  variables) from an object that it creates itself. An example of
    ///  such a state might be a string containing the current text in a
    ///  console; or a object array of the variables in the master class.
    /// This library allows the user code to reset itself to any point in
    ///  time. It'll do this by doing a cold reset of the program to the
    ///  most recent "snapshot" given (see the MachineState variables
    ///  below), and then processing any events that happened between that
    ///  snapshot and the time requested by the user.
    /// Note that the user code may jump not only into the past, but also
    ///  into the future, and back to the present. Since these points in
    ///  time are totally relative, it's up to the user code to remember
    ///  where these are: the Manager will only record the current time-
    ///  perspective of the program.
    ///  
    /// I know these descriptions are vague and nondescript. For a better
    ///  understanding of the use of this library, see the attached
    ///  examples.
    /// 
    /// TODO: Incorporate non-constant flow of time
    /// TODO: Allow cloning of timelines
    /// </summary>
    public class EventManager
    {
        //This function, when given an event and the time (in MilliSeconds) when it happened, acts upon that event
        private Func<object, long, bool> mEventHandler;

        //These lists hold the user-given events as well as the times (in ticks) when they happened
        private List<object> mEvents = new List<object>();
        private List<long> mTimeStamps = new List<long>();

        //These values and timer (all in ticks) help locate the current moment in time and when events were last acted upon
        private long mPreviousTime = -1;
        private long mCurrentOffset = 0;
        private Stopwatch mTimer = new Stopwatch();

        //These lists contain snapshots of the user program as well as timestamps for each snapshot.
        //Note that even if snapshots are disabled, an initial state will still be captured for time 0
        //Since the user code decides how to act upon these snapshots and what they should contain,
        // a program that already knows its own initial state can safely return null from the retriever,
        // but should still implement valid (if empty) functions for both the retriever and setter.
        private bool mMachineStatesEnabled;
        private List<object> mMachineStates = new List<object>();
        private List<long> mMachineStateTimes = new List<long>();
        private Func<object> mMachineStateRetriever;
        private Action<object> mMachineStateSetter;
        private long mSaveStateInterval; //How long (in ticks) (minimum) between snapshots

        /// <summary>
        /// An event manager maintains and allows recreation of events across a timeline
        /// </summary>
        /// <param name="HandleEvents">A function that takes an event and the time of that event (in milliseconds) and returns whether or not that event can still happen successfully</param>
        /// <param name="GetMachineState">A function that returns all information needed to recreate the present state of the program</param>
        /// <param name="SetMachineState">A function that, given the state of the program at some point in time, handles anything needed to move the program to the provided state</param>
        /// <param name="SaveInterval">How frequently (in milliseconds) to save the state of the program</param>
        public EventManager(Func<object, long, bool> HandleEvents, 
            Func<object> GetMachineState, Action<object> SetMachineState, long SaveInterval = 1000)
        {
            mEventHandler = HandleEvents;
            mMachineStatesEnabled = SaveInterval > 0;
            mMachineStateRetriever = GetMachineState;
            mMachineStateSetter = SetMachineState;
            if (SaveInterval > 0)
                mSaveStateInterval = MStoTick(SaveInterval);
            else
                mSaveStateInterval = 1;
            
        }

        /// <summary>
        /// Call this function to start the event timeline
        /// </summary>
        public void Start()
        {
            mTimer.Start();
        }

        /// <summary>
        /// Call this function to pause the event timeline
        /// </summary>
        public void Stop()
        {
            mTimer.Stop();
        }

        /// <summary>
        /// Add an event to the timeline (the time is automatically detected)
        /// </summary>
        /// <param name="Event">The event to mark on the timeline</param>
        public void AddEvent(object Event)
        {
            long CurrentMoment = mCurrentOffset + mTimer.ElapsedTicks;
            int Index = mTimeStamps.FindIndex(i => i > CurrentMoment);
            if (Index == -1)
            {
                mEvents.Add(Event);
                mTimeStamps.Add(CurrentMoment);
            }
            else
            {
                mEvents.Insert(Index, Event);
                mTimeStamps.Insert(Index, CurrentMoment);
            }
            if ((mMachineStateTimes.Count > 0 && CurrentMoment < mMachineStateTimes.Last()) ||
                (mTimeStamps.Count > 0 && CurrentMoment < mTimeStamps.Last()))
            {
                int i = mMachineStateTimes.FindIndex(t => t > CurrentMoment);
                if (i > 0)
                {
                    mMachineStateTimes.RemoveRange(i, mMachineStateTimes.Count - i);
                    mMachineStates.RemoveRange(i, mMachineStates.Count - i);
                }
            }
        }

        /// <summary>
        /// Removes an event from the timeline
        /// </summary>
        /// <param name="Event">The event to remove</param>
        public void DeleteEvent(object Event)
        {
            int Index = mEvents.IndexOf(Event);
            if (Index < 0)
                return;
            DeleteEvent(Index);
        }

        /// <summary>
        /// Internal method to remove events at a certain index
        /// </summary>
        /// <param name="EventIndex">The index of the event to delete</param>
        private void DeleteEvent(int EventIndex)
        {
            //TODO: Delete all machine states after this event
            mEvents.RemoveAt(EventIndex);
            mTimeStamps.RemoveAt(EventIndex);
        }

        /// <summary>
        /// Call this function to act upon all events that have happened since you last called this function
        /// </summary>
        public void ProcessEvents()
        {
            ProcessEvents(-1, -1);
        }

        /// <summary>
        /// Acts upon all events between the start time and the end times, or on the interval of time since this function was last called
        /// </summary>
        /// <param name="StartTime">The beginning of the interval of time to run events in. If default value is used, start time is the end time from the previous function call</param>
        /// <param name="EndTime">The end of the interval of time to run events in. If default value is used, end time is the current point in time</param>
        private void ProcessEvents(long StartTime = -1, long EndTime = -1)
        {
            if(EndTime < StartTime)
                return;
            if(EndTime < 0)
                EndTime = mCurrentOffset + mTimer.ElapsedTicks;
            if (StartTime < 0)
                StartTime = mPreviousTime;

            int CurInd = mTimeStamps.FindIndex(i => i >= StartTime);

            lock (mEvents)
            {
                int StateIndex = 0;
                for (; CurInd >= 0 && CurInd < mTimeStamps.Count && mTimeStamps[CurInd] < EndTime; CurInd++)
                {
                    if (
                        mMachineStates.Count == 0 ||
                        (mMachineStatesEnabled && CurInd > 0 &&
                        mTimeStamps[CurInd] > mMachineStateTimes[
                            StateIndex = mMachineStateTimes.FindLastIndex(t => t < mTimeStamps[CurInd])
                        ] + mSaveStateInterval)
                        )
                    {
                        //if (mMachineStates.Count > 0)
                        //    StateIndex++;
                        //if (mMachineStatesEnabled)
                        //{
                        //    if (mMachineStates.Count > 0 && mMachineStates.Count > StateIndex)
                        //    {
                        //        mMachineStates.RemoveRange(StateIndex, mMachineStates.Count - StateIndex - 1);
                        //        mMachineStateTimes.RemoveRange(StateIndex, mMachineStates.Count - StateIndex - 1);
                        //    }
                        //}
                        mMachineStateTimes.Add(mMachineStates.Count > 0 ? mTimeStamps[CurInd] : 0);
                        mMachineStates.Add(mMachineStateRetriever());
                    }

                    if (!mEventHandler(mEvents[CurInd], TicktoMS(mTimeStamps[CurInd])))
                        DeleteEvent(CurInd);
                }
            }

            mPreviousTime = EndTime;
        }

        /// <summary>
        /// Sets the current state of the machine to any point in time, past, present, or future
        /// </summary>
        /// <param name="Timems">The time (in milliseconds) to set the machine state to</param>
        public void JumpToTime(long Timems)
        {
            //TODO: Figure out why the program doesn't seem to save enough states for high-frequency save speeds
            Timems = MStoTick(Timems);
            int State = Math.Max(0,
                Math.Min(mMachineStateTimes.FindLastIndex(t => t < Timems), mMachineStates.Count - 1));
            if (mMachineStates.Count > 0)
            {
                mMachineStateSetter(mMachineStates[State]);
            }

            mCurrentOffset = (long)((double)Timems);
            mTimer.Reset();

            ProcessEvents(mMachineStateTimes[State],
                Timems);

            mTimer.Start();
        }

        /// <summary>
        /// Internal helper method to convert time in MilliSeconds to time in Ticks
        /// </summary>
        /// <param name="MS">The time in milliseconds</param>
        /// <returns>The same time in ticks</returns>
        private long MStoTick(long MS)
        {
            return (long)((double)MS / 1000d * Stopwatch.Frequency);
        }

        /// <summary>
        /// Internal helper method to convert time in ticks to time in MilliSeconds
        /// </summary>
        /// <param name="Ticks">The time in ticks</param>
        /// <returns>The same time in milliseconds</returns>
        private long TicktoMS(long Ticks)
        {
            return (long)((double)Ticks / Stopwatch.Frequency * 1000);
        }

        /// <summary>
        /// Returns the current point in time in milliseconds
        /// </summary>
        /// <returns>The number of milliseconds since the timeline began until the current perspect</returns>
        public long CurrentTime()
        {
            return TicktoMS(mCurrentOffset + mTimer.ElapsedTicks);
        }

        /// <summary>
        /// Determines if any events have occured in the timeline that have not yet been acted upon
        /// </summary>
        /// <returns>True if events are waiting to be processed</returns>
        public bool HasUnprocessedEvents()
        {
            long CurMoment = CurrentTime();
            return mTimeStamps.Any(l => mPreviousTime <= l && l < CurMoment);
        }
    }
}
