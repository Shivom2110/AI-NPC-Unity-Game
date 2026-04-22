using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adaptive NPC that responds to both free-form text and specific game-tutorial keywords.
/// Set personality = "mentor" in the Inspector to unlock the full tutorial knowledge base.
/// Other personalities (merchant, guard) give shorter flavour responses.
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string npcId      = "Old Swordmaster";
    [SerializeField] private string personality = "mentor";   // mentor | merchant | guard

    [Header("Boss Flag")]
    [SerializeField] private bool isBoss = false;

    private NPCMemory _memory;
    private string    _lastReply        = "";
    private string    _secondLastReply  = "";

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Start()
    {
        if (NPCMemoryManager.Instance != null)
            _memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);
    }

    // ── Public accessors ───────────────────────────────────────────────────────

    public bool       IsBoss()    => isBoss;
    public string     GetNPCId()  => npcId;
    public NPCMemory  GetMemory()
    {
        if (_memory == null && NPCMemoryManager.Instance != null)
            _memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);
        return _memory;
    }

    // ── Main response entry point ──────────────────────────────────────────────

    /// <summary>
    /// Generate a response to any raw player input.
    /// Checks tutorial keywords first, then falls back to personality-based responses.
    /// </summary>
    public string GetResponse(string rawInput)
    {
        if (NPCMemoryManager.Instance == null) return "The world isn't ready yet…";

        if (_memory == null)
            _memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);

        string lo       = rawInput.ToLowerInvariant().Trim();
        string response = TryTutorialResponse(lo) ?? GeneratePersonalityResponse(lo);

        // Avoid back-to-back identical lines
        if (response == _lastReply && response == _secondLastReply)
            response = "Is there anything else you'd like to know?";

        // Record interaction
        string action = ClassifyAction(lo);
        int    delta  = RelationshipDelta(action);
        NPCMemoryManager.Instance.RecordInteraction(npcId, rawInput, response, "ok", delta);
        _memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);

        _secondLastReply = _lastReply;
        _lastReply       = response;
        return response;
    }

    /// <summary>Legacy entry point — kept so other scripts don't break.</summary>
    public string InteractWithPlayer(string action) => GetResponse(action);

    // ── Tutorial knowledge base ────────────────────────────────────────────────

    private string TryTutorialResponse(string lo)
    {
        // Greetings
        if (HasAny(lo, "hello", "hi", "hey", "greet", "howdy", "morning", "evening", "yo"))
            return PickFrom(
                "Ah, a warrior approaches. What would you like to know?",
                "Welcome. Ask me anything about the fight ahead.",
                "I've been waiting. What can I teach you?",
                "Good to see you. The dungeon ahead holds great peril — ask wisely.");

        // Goodbye
        if (HasAny(lo, "bye", "goodbye", "farewell", "later", "leave", "nothing", "done"))
            return PickFrom(
                "Stay sharp out there. The boss shows no mercy.",
                "May your blade find its mark. Farewell.",
                "Come back if you need guidance. Good luck.",
                "Remember what I taught you. You'll need it.");

        // Full controls list
        if (HasAny(lo, "controls", "keys", "keyboard", "how to play", "button", "buttons", "keybinds", "keybinding"))
            return
                "Here are all the controls:\n" +
                "  F         — Draw / Sheathe your sword\n" +
                "  W A S D   — Move\n" +
                "  LMB       — Light attack\n" +
                "  RMB       — Heavy attack\n" +
                "  Q         — Parry (press before the hit lands)\n" +
                "  SPACE×2   — Dodge roll (costs stamina)\n" +
                "  E         — Flashy attack (4s cooldown)\n" +
                "  R         — Ultimate (15s cooldown)\n" +
                "  T         — Talk to me\n" +
                "  Esc       — Close this chat";

        // Combat / attacking
        if (HasAny(lo, "how to fight", "fight", "attack", "combat", "hit", "lmb", "light attack", "rmb", "heavy attack", "sword", "strike"))
            return PickFrom(
                "Draw your sword with F first. Then LMB swings a quick light attack — fast but lower damage. " +
                "RMB delivers a slow, powerful heavy blow. Chain attacks together for combo bonuses, but " +
                "vary your moves or the boss will start predicting you.",

                "Light attacks (LMB) are fast and good for building combos. " +
                "Heavy attacks (RMB) hit hard but have longer cooldowns. " +
                "Mix them up — the boss learns your patterns and will counter them.",

                "Your combo multiplier increases when you use different attacks in sequence. " +
                "Spam the same attack and it drops to 0.8×. Keep it varied for up to 1.5× bonus.");

        // Parrying
        if (HasAny(lo, "parry", "block", "q key", " q ", "counter attack", "parry window", "how to parry"))
            return PickFrom(
                "Parrying is your most powerful tool. Press Q just before the boss's attack lands — " +
                "glowing attacks can always be parried. A Perfect parry (very early press) deals 2× counter damage " +
                "and staggers the boss longer. A Good parry still stuns. Miss the window and you'll take full damage.",

                "Watch the bottom-right indicator: when it shows a green 'Q — PARRY', " +
                "that's your signal. Press Q during the boss's wind-up, not after the swing. " +
                "Timing is everything — too late and it won't work.",

                "Chain several successful parries in a row to activate Heat Mode — your attacks deal " +
                "bonus damage while the orange indicator glows. Parrying well rewards aggression.");

        // Dodging
        if (HasAny(lo, "dodge", "roll", "evade", "space", "double tap", "dodge roll", "how to dodge", "stamina roll"))
            return PickFrom(
                "Double-tap SPACE to execute a dodge roll. Some boss attacks can't be parried — " +
                "look for the blue 'SPACE×2 — DODGE' indicator on screen. Rolling costs 20 stamina, " +
                "which regenerates at 10 per second. Don't roll recklessly or you'll be defenseless.",

                "The blue bar below your health is stamina. Each dodge costs 20 stamina. " +
                "If you see 'SPACE×2' in the bottom-right, that attack MUST be dodged — parrying won't help. " +
                "Time your roll through the attack for a perfect dodge.",

                "Roll through attacks, not away from them — you get invincibility frames during the roll. " +
                "If stamina runs out, you're forced to take hits. Keep 20 in reserve at all times.");

        // Flashy / special / E key
        if (HasAny(lo, "flashy", "special", "special attack", " e ", "e key", "e attack", "special move"))
            return PickFrom(
                "E is your Flashy Attack — it deals 100 damage with a 4-second cooldown. " +
                "More importantly, if the boss is charging a heavy attack, landing E during the wind-up " +
                "INTERRUPTS the charge. The boss staggers instead of completing the swing. Very powerful.",

                "The Flashy Attack (E) shines against heavy boss wind-ups. " +
                "Watch for the green indicator during a heavy telegraph and strike with E — " +
                "the boss will be interrupted and left open for follow-up attacks.");

        // Ultimate / R key
        if (HasAny(lo, "ultimate", " r ", "r key", "ult ", "special r", "big attack", "strongest"))
            return PickFrom(
                "R is your Ultimate — 300 damage and a 15-second cooldown. " +
                "Like the Flashy Attack, hitting R during a heavy boss telegraph interrupts the charge. " +
                "Save it for Phase 3 or 4 when the boss gets dangerous, or to interrupt a guaranteed-hit attack.",

                "The Ultimate (R) is your hardest-hitting move. Combine it with the interrupt mechanic: " +
                "if the boss starts a heavy wind-up, R will cancel it and deal massive damage. " +
                "Don't waste it on light attacks — time it for interrupts or phase transitions.");

        // Interrupt mechanic
        if (HasAny(lo, "interrupt", "cancel", "stagger", "stop boss", "stop attack", "counter heavy", "interrupt boss"))
            return
                "The interrupt mechanic is key against heavy attacks. When you see the boss " +
                "winding up a heavy or special move (high multiplier attacks), hit E (Flashy) or R (Ultimate) " +
                "before the hit lands. The charge cancels, the boss plays a hurt animation, and you have a " +
                "1.8-second window to land free attacks. Only works on heavy boss moves — light strikes must be parried or dodged.";

        // Boss general
        if (HasAny(lo, "boss", "enemy", "monster", "creature", "opponent", "who is", "what is"))
            return PickFrom(
                "The boss you face is no ordinary foe. It adapts to your fighting style — " +
                "use the same combo twice and it will start predicting you. It has four phases, each " +
                "more dangerous than the last. Phase transitions trigger new attacks and faster assault intervals.",

                "The boss grows smarter each fight. It tracks your most-used combos and counters them. " +
                "Keep your attacks varied and it can't build a pattern model against you. " +
                "Land enough damage quickly and you'll keep it guessing.");

        // Boss phases
        if (HasAny(lo, "phase", "phases", "stronger", "health drop", "enrage", "boss stage"))
            return
                "The boss has four combat phases:\n" +
                "  Phase 1  — above 70% HP. Mostly parryable attacks, slow pace.\n" +
                "  Phase 2  — 40–70% HP. Unparryable spins and grabs appear. Mix dodges in.\n" +
                "  Phase 3  — 15–40% HP. Faster attacks, shorter intervals, more counters.\n" +
                "  Phase 4  — below 15% HP. Unstoppable Rush added. Near-constant pressure.\n" +
                "Watch for the phase banner — each transition triggers a roar. Back off and reset when you see it.";

        // Heat mode
        if (HasAny(lo, "heat mode", "heat", "bonus damage", "orange indicator", "successive parry"))
            return
                "Heat Mode activates after several consecutive successful parries. " +
                "While active (shown by the orange HEAT MODE pill), your attacks deal increased damage. " +
                "It's your reward for playing aggressively and precisely. Taking a hit without parrying resets it. " +
                "The boss fights back harder in Phase 4, so activate Heat Mode early.";

        // Stamina
        if (HasAny(lo, "stamina", "energy", "blue bar", "run out", "out of stamina", "tired"))
            return
                "The blue bar below your health is your stamina. Each dodge roll costs 20 stamina. " +
                "Stamina regenerates at 10 per second, so it refills in about 2 seconds after a roll. " +
                "If the bar empties you cannot dodge — plan ahead and never roll unnecessarily. " +
                "The SPACE×2 and Out of Stamina sounds will cue you.";

        // Window indicator
        if (HasAny(lo, "indicator", "window", "screen indicator", "bottom right", "prompt", "signal", "cue"))
            return
                "Watch the bottom-right corner of the screen during combat. It shows the current window:\n" +
                "  Green  'Q — PARRY'      → Press Q to parry this attack\n" +
                "  Blue   'SPACE×2 — DODGE' → Double-tap SPACE to dodge\n" +
                "  Orange 'NOW!'           → The hitbox is active — react now\n" +
                "  Gold   'COUNTER!'       → You parried or dodged — attack immediately for bonus damage\n" +
                "React to the indicator, not the animation — the screen tells you exactly what to do.";

        // General tips
        if (HasAny(lo, "tip", "tips", "advice", "strategy", "how do i win", "help me", "suggest", "recommendation"))
            return PickFrom(
                "My top tips:\n" +
                "1. Vary your attacks — never use the same move three times in a row.\n" +
                "2. Watch the bottom-right indicator and react to it.\n" +
                "3. Use E or R to interrupt heavy boss wind-ups.\n" +
                "4. Keep 20 stamina reserved for emergency dodges.\n" +
                "5. Chain parries to enter Heat Mode for bonus damage.\n" +
                "6. Back off when the boss phases up — it roars and resets its patterns.",

                "The boss reads your patterns. If you always dodge left, it adjusts. " +
                "Alternate your approach and never spam the same combo. " +
                "Perfect parries are worth more than any attack — master the timing above everything else.",

                "Save your Ultimate for Phase 3 and 4. A well-timed R can interrupt the most " +
                "dangerous attacks and swing the fight in your favor. Desperation is when the boss is strongest — " +
                "stay calm and read the indicators.");

        // Relationship / memory
        if (HasAny(lo, "remember", "last time", "before", "seen me", "know me", "past"))
        {
            if (_memory != null && _memory.interactions != null && _memory.interactions.Count > 1)
                return PickFrom(
                    "I remember you. You've come a long way since we first spoke.",
                    "Yes, we've talked before. Your understanding has grown.",
                    "Each visit you ask sharper questions. Keep pushing yourself.");
            return "This appears to be our first real conversation. Ask me what you need to know.";
        }

        return null;   // no tutorial keyword matched — fall through to personality response
    }

    // ── Personality-based fallback ─────────────────────────────────────────────

    private string GeneratePersonalityResponse(string lo)
    {
        string action   = ClassifyAction(lo);
        int    score    = _memory != null ? _memory.relationshipScore : 0;
        string relation = NPCMemoryManager.GetRelationshipLevel(score);
        string persona  = personality.ToLowerInvariant();

        List<string> pool = BuildPool(persona, action, relation);
        string callback   = BuildMemoryCallback(action, relation);
        string pick       = PickNonRepeating(pool);

        return string.IsNullOrEmpty(callback) ? pick : callback + " " + pick;
    }

    private string ClassifyAction(string lo)
    {
        if (HasAny(lo, "hello","hi","hey","greet","howdy","morning","evening","yo")) return "greet";
        if (HasAny(lo, "buy","sell","trade","shop","deal","goods","wares","price"))  return "trade";
        if (HasAny(lo, "help","assist","quest","task","need","favour","job"))        return "help";
        if (HasAny(lo, "threat","kill","die","attack","hurt","warn","fight you"))    return "threaten";
        if (HasAny(lo, "bye","goodbye","farewell","later","leave","done","nothing")) return "bye";
        return "greet";
    }

    private int RelationshipDelta(string action)
    {
        switch (action)
        {
            case "greet":    return  1;
            case "help":     return  5;
            case "trade":    return  2;
            case "threaten": return -10;
            default:         return  0;
        }
    }

    private string BuildMemoryCallback(string action, string relation)
    {
        if (_memory == null || _memory.interactions == null || _memory.interactions.Count == 0)
            return "";
        var last = _memory.interactions[_memory.interactions.Count - 1];
        if (action == "greet" && relation == "Friendly")                       return "Good to see you again.";
        if (relation == "Hostile" || relation == "Enemy")                      return "I remember how you behaved.";
        if (last.playerAction == "threaten")                    return "I haven't forgotten your tone.";
        return "";
    }

    private List<string> BuildPool(string persona, string action, string relation)
    {
        var lines = new List<string>();

        bool isFriendly = relation == "Friendly" || relation == "Allied";

        if (persona.Contains("mentor") || persona.Contains("wise") || persona.Contains("sage"))
        {
            if (action == "greet" && isFriendly)
            {
                lines.Add("Welcome back. Your questions grow sharper each visit.");
                lines.Add("Ah, you return. The dungeon has not broken you yet.");
                lines.Add("Good to see you alive. What wisdom do you seek today?");
            }
            else if (action == "greet")
            {
                lines.Add("Greetings, warrior. Ask me what you need.");
                lines.Add("I have knowledge of the fight ahead. Speak freely.");
                lines.Add("Another seeks guidance before the trial. What is your question?");
            }
            else if (action == "help")
            {
                lines.Add("You have shown wisdom by asking. Use the quick replies or ask anything.");
                lines.Add("Curiosity keeps warriors alive. I will answer what I can.");
                lines.Add("The willing student learns fast. What do you need to know?");
            }
            else if (action == "trade")
            {
                lines.Add("I trade in knowledge, not coin. Ask me what you need.");
                lines.Add("My wares are words and warnings. What do you seek?");
                lines.Add("Information is the only currency that matters here.");
            }
            else if (action == "threaten")
            {
                lines.Add("Threaten me if you wish. The boss will be far less patient.");
                lines.Add("Save that anger for the arena. You will need it.");
                lines.Add("I have faced worse than your threats. Ask or leave.");
            }
            else if (action == "bye")
            {
                lines.Add("Go well. Remember — vary your attacks and watch the indicator.");
                lines.Add("May your parries be perfect. Farewell.");
                lines.Add("The knowledge is yours now. Use it wisely.");
            }
            else
            {
                lines.Add("Ask me about controls, parrying, the boss, or combat tips.");
                lines.Add("Use the quick replies above or type your question.");
                lines.Add("I know all the secrets of this dungeon. Ask anything.");
            }
        }
        else if (persona.Contains("merchant"))
        {
            if (action == "trade" && isFriendly)
            {
                lines.Add("For a friend, I always have something special.");
                lines.Add("Back again? I may have just what you need.");
                lines.Add("Take your time. You've earned a good deal.");
            }
            else if (action == "trade")
            {
                lines.Add("Take a look. I may have what you need.");
                lines.Add("Browse freely, but don't waste my time.");
                lines.Add("Let's see if anything here interests you.");
            }
            else if (action == "bye")
            {
                lines.Add("Safe travels. Come back if you need supplies.");
                lines.Add("Until next time, traveler.");
                lines.Add("Don't die out there — you still owe me.");
            }
            else
            {
                lines.Add("Welcome. What can I do for you?");
                lines.Add("Looking for something specific?");
                lines.Add("Speak up — what do you need?");
            }
        }
        else // guard / generic
        {
            if (action == "threaten")
            {
                lines.Add("One more word and this conversation ends differently.");
                lines.Add("Watch yourself.");
                lines.Add("I don't scare easily.");
            }
            else if (isFriendly)
            {
                lines.Add("You've proven yourself. What do you need?");
                lines.Add("A trusted face. Speak freely.");
                lines.Add("Stay alert out there. What can I do for you?");
            }
            else
            {
                lines.Add("State your business.");
                lines.Add("Keep it brief.");
                lines.Add("What do you want?");
            }
        }

        return lines;
    }

    // ── Utility ────────────────────────────────────────────────────────────────

    private string PickFrom(params string[] options)
        => options[Random.Range(0, options.Length)];

    private string PickNonRepeating(List<string> pool)
    {
        if (pool == null || pool.Count == 0) return "I have nothing more to add.";
        var filtered = pool.FindAll(s => s != _lastReply && s != _secondLastReply);
        if (filtered.Count == 0) filtered = pool;
        return filtered[Random.Range(0, filtered.Count)];
    }

    private static bool HasAny(string src, params string[] keywords)
    {
        foreach (var k in keywords)
            if (src.Contains(k)) return true;
        return false;
    }
}
