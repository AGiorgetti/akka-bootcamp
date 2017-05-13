using Akka.Actor;
using System;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Start tailing the file at user-specified path
        /// </summary>
        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterAction)
            {
                FilePath = filePath;
                ReporterAction = reporterAction;
            }

            public string FilePath { get; }
            public IActorRef ReporterAction { get; }
        }

        /// <summary>
        /// Stop tailing the file at user-specified path.
        /// </summary>
        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; }
        }

        #endregion
        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;
                // here e are creating or first parent/child relationship!
                // the TailActor instance created here ia a child
                // of the instance of TailCoordinatorActor
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterAction, msg.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maxNumberOfretries
                TimeSpan.FromSeconds(30), // withinTimeRange
                x => // localOnlyDecider
                {
                    // Maybe we consider ArithmeticExceotion to not be application critical
                    // so we just ignore and keep going.
                    if (x is ArithmeticException) return Directive.Resume;

                    // Error that we cannot recover from, stop the failing actor
                    else if (x is NotSupportedException) return Directive.Stop;

                    // In all other cases, just restart the failing actor
                    else return Directive.Restart;
                });
        }
    }
}
