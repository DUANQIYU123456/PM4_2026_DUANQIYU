using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>
    /// Subscribes to an <see cref="ElevenLabsTranscriber"/>, tokenizes the transcript, and matches
    /// tokens against the live <see cref="CommandRegistry"/> by <see cref="ICommand.Key"/>. Every
    /// registered Command whose key appears as a word in the transcript is added to
    /// <see cref="LastMatches"/>. Does NOT execute the matches — VoiceCommandRouter consumes the
    /// output and dispatches <c>Execute()</c>.
    /// </summary>
    public class TranscriptCommandParser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("Transcriber whose OnTranscriptionComplete will feed parsing. Optional — Parse(string) is public so it can also be wired from any UnityEvent<string>.")]
        [SerializeField] private ElevenLabsTranscriber transcriber;

        [Header("Registry")]
        [Tooltip("CommandRegistry SO that holds the vocabulary to match transcript tokens against. Commands must be registered here (i.e. have this same SO assigned to their own Registry slot) to be discoverable by this parser.")]
        [SerializeField] private CommandRegistry registry;

        [Header("Execution")]
        [Tooltip("If true, every matched command's Execute() is called immediately after parsing, in the order their key words appeared in the transcript. Turn off to inspect matches without firing them.")]
        [SerializeField] private bool autoExecute = true;

        //==================== STATE =====================
        [Header("Debug")]
        [Tooltip("The most recent transcript that was parsed.")]
        [ReadOnly, SerializeField] private string lastTranscript;

        [Tooltip("How the transcript was tokenized (lowercase, punctuation stripped).")]
        [ReadOnly, SerializeField] private string[] lastTokens;

        [Tooltip("Commands matched on the last Parse call. Cleared on each new parse.")]
        [ReadOnly, SerializeField] private List<Command> lastMatches = new();

        /// <summary>Commands matched on the last Parse call. Backed by the inspector debug list.</summary>
        public IReadOnlyList<Command> LastMatches => lastMatches;

        /// <summary>The most recent transcript fed into Parse.</summary>
        public string LastTranscript => lastTranscript;

        //==================== OUTPUTS =====================
        public event Action<IReadOnlyList<Command>> OnParsed;

        [Header("Events")]
        [Tooltip("Fired after Parse when at least one command matched.")]
        [SerializeField] private UnityEvent matchedEvent;

        [Tooltip("Fired after Parse when no command matched. Useful for 'I didn't understand' UI feedback.")]
        [SerializeField] private UnityEvent noMatchEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (transcriber) transcriber.OnTranscriptionComplete += Parse;
        }

        private void OnDisable()
        {
            if (transcriber) transcriber.OnTranscriptionComplete -= Parse;
        }

        //==================== PUBLIC API =====================
        /// <summary>
        /// Tokenize <paramref name="transcript"/> and match each unique token against the live
        /// <see cref="CommandRegistry"/>. Every Command registered under a matched key is added to
        /// <see cref="LastMatches"/>. Idempotent — clears prior matches first.
        /// </summary>
        public void Parse(string transcript)
        {
            lastTranscript = transcript ?? "";
            lastMatches.Clear();
            lastTokens = Tokenize(lastTranscript);

            if (lastTokens.Length > 0 && registry)
            {
                // Unique tokens only — otherwise "fight fight" would add every matching command twice.
                var seen = new HashSet<string>();
                foreach (var token in lastTokens)
                {
                    if (!seen.Add(token)) continue;
                    var hits = registry.Get(token);
                    for (int i = 0; i < hits.Count; i++)
                        if (hits[i] is Command c) lastMatches.Add(c);
                }
            }

            OnParsed?.Invoke(lastMatches);
            if (lastMatches.Count > 0) matchedEvent?.Invoke();
            else noMatchEvent?.Invoke();

            if (autoExecute) ExecuteMatches();
        }

        /// <summary>
        /// Calls <c>Execute()</c> on every command in <see cref="LastMatches"/>. Safe to call repeatedly —
        /// <see cref="Command.Execute"/> is a no-op when the command is already active. Exposed publicly so
        /// you can disable <c>autoExecute</c> and fire matches manually (e.g. behind a confirmation step).
        /// </summary>
        public void ExecuteMatches()
        {
            for (int i = 0; i < lastMatches.Count; i++)
            {
                var c = lastMatches[i];
                if (c) c.Execute();
            }
        }

        //==================== PRIVATE =====================
        // Lowercase, split on non-alphanumerics. Apostrophes dropped so "don't" -> "dont".
        private static string[] Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (c == '\'') continue;
                else if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
            }

            var trimmed = sb.ToString().Trim();
            return trimmed.Length == 0 ? Array.Empty<string>() : trimmed.Split(' ');
        }
    }
}
