using UnityEngine;

namespace AnyVR.UserControlSystem.PC
{
    public class PCInteractionDebugger : MonoBehaviour
    {
        private void LogInteraction(string message)
        {
            Debug.Log("[PC Interaction] " + message);
        }

        public void SelectionEntered()
        {
            LogInteraction("Selection Entered");
        }

        public void SelectionExited()
        {
            LogInteraction("Selection Exited");
        }

        public void PrimarySelectionAction()
        {
            LogInteraction("Primary Selection Action Triggered");
        }

        public void PrimarySelectionActionCanceled()
        {
            LogInteraction("Primary Selection Action Canceled");
        }

        public void SecondarySelectionAction()
        {
            LogInteraction("Secondary Selection Action Triggered");
        }

        public void SecondarySelectionActionCanceled()
        {
            LogInteraction("Secondary Selection Action Canceled");
        }
    }
}
