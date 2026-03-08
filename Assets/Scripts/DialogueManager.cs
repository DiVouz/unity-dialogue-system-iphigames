using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogueLogic : MonoBehaviour {

    [System.Serializable]
    public class PunctuationPauseMs {
        public string character;
        public float pauseMs;
        public float volumeMultiplier = 1f;
    }

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Image speakerImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI linesText;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject skipButton;

    [Header("Dialogue Content")]
    [SerializeField] private DialogueSequence dialogueSequence;

    [Header("Typing")]
    [SerializeField] private float msDelayBetweenCharacters = 50f;
    [SerializeField] private float speakerImageMovementOffset = 1.25f;
    [SerializeField] private List<PunctuationPauseMs> punctuationPauseMsList;

    private Vector3 speakerImageOriginalPosition;
    private string originalContinueButtonText;
    private string originalSkipButtonText;
    private Dictionary<char, PunctuationPauseMs> punctuationPauseDictionary;

    [Header("Audio")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip[] typingSounds;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float volume = 0.1f;

    private GlyphMappingDatabase glyphMappingDatabase;

    private int dialogueSequenceIndex = 0;
    private IEnumerator typingCoroutine;
    
    private void OnEnable() {
        InputDeviceDetector.OnInputDeviceChanged += OnDeviceChanged;
    }

    private void OnDisable() {
        InputDeviceDetector.OnInputDeviceChanged -= OnDeviceChanged;
    }
    
    private void Awake() {
        // Add listeners to buttons
        continueButton.GetComponent<Button>().onClick.AddListener(OnContinueButtonPressed);
        skipButton.GetComponent<Button>().onClick.AddListener(OnSkipButtonPressed);      

        originalContinueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>().text;
        originalSkipButtonText = skipButton.GetComponentInChildren<TextMeshProUGUI>().text;

        speakerImageOriginalPosition = speakerImage.rectTransform.anchoredPosition;

        punctuationPauseDictionary = new Dictionary<char, PunctuationPauseMs>();
        foreach (PunctuationPauseMs punctuationPause in punctuationPauseMsList) {
            if (!string.IsNullOrEmpty(punctuationPause.character) && punctuationPause.character.Length == 1) {
                char key = punctuationPause.character[0];
                if (!punctuationPauseDictionary.ContainsKey(key)) {
                    punctuationPauseDictionary.Add(key, punctuationPause);
                }
            }
        }

        dialogueBox.SetActive(false);
    }

    private string ParseStringWithGlyphs(string input) {
        if (glyphMappingDatabase == null || glyphMappingDatabase.mappings == null) {
            return input;
        }
        
        foreach (GlyphMapping mapping in glyphMappingDatabase.mappings) {
            input = input.Replace(mapping.token, $"<sprite name=\"{mapping.spriteName}\">");
        }
        
        return input;
    }

    private void OnDeviceChanged(GlyphMappingDatabase newGlyphMappingDatabase) {
        if (newGlyphMappingDatabase == null) {
            return;
        }

        bool shouldStartDialogue = glyphMappingDatabase == null;

        glyphMappingDatabase = newGlyphMappingDatabase;

        if (shouldStartDialogue) {
            dialogueSequenceIndex = 0;
            dialogueBox.SetActive(true);
            typingCoroutine = StartTypeLine();
            StartCoroutine(typingCoroutine);
        }
        
        // DIALOGUE
        dialogueText.spriteAsset = glyphMappingDatabase.spriteAsset;
        dialogueText.text = ParseStringWithGlyphs(dialogueSequence.entries[dialogueSequenceIndex].DialogueText);
        dialogueText.ForceMeshUpdate();

        // CONTINUE BUTTON
        TextMeshProUGUI continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        continueButtonText.spriteAsset = glyphMappingDatabase.spriteAsset;
        continueButtonText.text = ParseStringWithGlyphs(originalContinueButtonText);
        continueButtonText.ForceMeshUpdate();

        // SKIP BUTTON
        TextMeshProUGUI skipButtonText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
        skipButtonText.spriteAsset = glyphMappingDatabase.spriteAsset;
        skipButtonText.text = ParseStringWithGlyphs(originalSkipButtonText);
        skipButtonText.ForceMeshUpdate();
    }

    private void PlayTypingSound() {
        if (typingAudioSource == null || typingSounds == null || typingSounds.Length == 0) {
            return;
        }

        AudioClip clip = typingSounds[Random.Range(0, typingSounds.Length)];
        typingAudioSource.pitch = Random.Range(minPitch, maxPitch);
        typingAudioSource.PlayOneShot(clip);
    }

    private void MoveSpeakerImage() {
        Vector3 randomOffset = new Vector3(0, Random.Range(-speakerImageMovementOffset, speakerImageMovementOffset), 0);
        speakerImage.rectTransform.anchoredPosition = speakerImageOriginalPosition + randomOffset;
    }

    private void ResetSpeakerImagePosition() {
        speakerImage.rectTransform.anchoredPosition = speakerImageOriginalPosition;
    }

    public void OnSkipButtonPressed() {
        if (typingCoroutine == null) {
            return;
        }

        StopCoroutine(typingCoroutine);
        typingCoroutine = null;

        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;

        ResetSpeakerImagePosition();
    }

    public void OnContinueButtonPressed() {
        if (typingCoroutine != null) {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            ResetSpeakerImagePosition();
        }
        
        if (dialogueText.maxVisibleCharacters < dialogueText.textInfo.characterCount) {
            dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
            return;
        }

        if (dialogueSequenceIndex < dialogueSequence.entries.Count - 1) {
            dialogueSequenceIndex++;
            
            typingCoroutine = StartTypeLine();
            StartCoroutine(typingCoroutine);

            return;
        }
        
        dialogueBox.SetActive(false);
    }

    private IEnumerator StartTypeLine() {
        speakerNameText.text = dialogueSequence.entries[dialogueSequenceIndex].SpeakerName;
        speakerImage.sprite = dialogueSequence.entries[dialogueSequenceIndex].SpeakerSprite;

        dialogueText.spriteAsset = glyphMappingDatabase.spriteAsset;
        dialogueText.text = ParseStringWithGlyphs(dialogueSequence.entries[dialogueSequenceIndex].DialogueText);
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        linesText.text = $"Line {dialogueSequenceIndex + 1} / {dialogueSequence.entries.Count}";

        ResetSpeakerImagePosition();

        int totalCharacters = dialogueText.textInfo.characterCount;
        while (dialogueText.maxVisibleCharacters < totalCharacters) {
            dialogueText.maxVisibleCharacters++;

            float delaySeconds = msDelayBetweenCharacters / 1000f;
            typingAudioSource.volume = volume;

            int revealedCharIndex = dialogueText.maxVisibleCharacters - 1;
            if (revealedCharIndex >= 0 && revealedCharIndex < dialogueText.textInfo.characterInfo.Length) {
                char revealedCharacter = dialogueText.textInfo.characterInfo[revealedCharIndex].character;
                if (punctuationPauseDictionary.TryGetValue(revealedCharacter, out PunctuationPauseMs punctuationPause)) {
                    delaySeconds += punctuationPause.pauseMs / 1000f;
                    typingAudioSource.volume *= punctuationPause.volumeMultiplier;
                }
            }

            PlayTypingSound();
            MoveSpeakerImage();

            yield return new WaitForSeconds(delaySeconds);
        }

        ResetSpeakerImagePosition();
    }
}
