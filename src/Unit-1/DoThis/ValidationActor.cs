using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor that validates use input and signals result to others.
    /// </summary>
    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public ValidationActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply and input.
                _consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsValid(msg);
                if (valid)
                {
                    // send success to console writer
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid."));
                }
                else
                {
                    // signal the input was bad
                    _consoleWriterActor.Tell(new Messages.ValidationError("invalid: input has odd number of characters."));
                }
            }

            // tell the sender to continue doing its thing
            // (whatever that may be, this actor doesn't care)
            Sender.Tell(new Messages.ContinueProcessing());
        }

        /// <summary>
        /// Determines if the message is valid.
        /// Checks if the number of chars in message received is even.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool IsValid(string msg)
        {
            return msg.Length % 2 == 0;
        }
    }
}
