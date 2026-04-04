using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private Text npcNameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text relationshipText;
    [SerializeField] private Text promptText;

    public void ShowPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.text = "";
            promptText.gameObject.SetActive(false);
        }
    }

    public void ShowDialogue(string npcName, string dialogue, string relation)
    {
        if (npcNameText != null) npcNameText.text = npcName;
        if (dialogueText != null) dialogueText.text = dialogue;
        if (relationshipText != null) relationshipText.text = $"Relationship: {relation}";
    }

    public void ClearDialogue()
    {
        if (npcNameText != null) npcNameText.text = "";
        if (dialogueText != null) dialogueText.text = "";
        if (relationshipText != null) relationshipText.text = "";
    }
}
