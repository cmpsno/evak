using UnityEngine;

namespace Campusano.Dialogue
{
    /// <summary>
    /// TICKET-EP1-01 v2. Builds the Episode 1 DialogueDatabase in code
    /// (greybox; no authored assets yet). Shape matches Section 1 exactly.
    /// </summary>
    public static class Ep1DialogueBank
    {
        public static DialogueDatabase BuildDatabase()
        {
            var db = ScriptableObject.CreateInstance<DialogueDatabase>();
            db.speakers = new[]
            {
                Speaker("nicky", "Nicky", new Color(0.75f, 0.95f, 0.75f)),
                Speaker("c", "C", new Color(1.0f, 0.75f, 0.3f)),
                Speaker("father", "Father", new Color(0.6f, 0.7f, 0.9f)),
                Speaker("mook", "Mook", new Color(1.0f, 0.45f, 0.4f)),
                Speaker("narrator", "", Color.white),
            };
            db.nodes = new[]
            {
                // ---- M1: C gives the job ---------------------------------
                Node("C_First_Talk", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("c", "Look who it is. The kid who didn't rat.", false, 0f),
                        Line("c", "I got a job for you. Guy named Mook owes me two grand. He's late.", false, 0f),
                        Line("nicky", "Two grand? That's a lot of faith for a kid.", false, 0f),
                        Line("c", "Go collect it. Back room, card game. You know where to find him.", false, 0f),
                    },
                    null, ""),

                // ---- M2: Mook confrontation choice ------------------------
                Node("mook_choice", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("mook", "C sent a kid? He must really be desperate.", false, 0f),
                        Line("nicky", "He sent me to collect. Two grand, Mook.", false, 0f),
                        Line("mook", "And if I say no?", false, 0f),
                    },
                    new[]
                    {
                        Choice("intimidate", "Intimidate him.", "mook_intimidate"),
                        Choice("private_convo", "Ask for a private word.", "mook_private"),
                    }, null),

                Node("mook_intimidate", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("nicky", "Then I tell C you said no. And you know how that ends.", false, 0f),
                        Line("mook", "You got a mouth on you, kid.", false, 0f),
                    },
                    null, "knife_pull"),

                Node("mook_private", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("nicky", "Walk with me. Away from your friends.", false, 0f),
                        Line("mook", "...Fine. But this better be good.", false, 0f),
                    },
                    null, "knife_pull"),

                Node("knife_pull", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("narrator", "Mook's hand moves under the table. Steel catches the light.", true, 2f),
                        Line("mook", "Nobody takes my money, kid.", false, 0f),
                    },
                    null, ""),

                // ---- M3: report back to C ---------------------------------
                Node("C_Aftermath", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("c", "It's done, then. Mook won't be late again.", false, 0f),
                        Line("nicky", "It didn't have to go like that.", false, 0f),
                        Line("c", "It always goes like that. Here's your cut.", false, 0f),
                    },
                    null, ""),

                // ---- Denouement: father -----------------------------------
                Node("father_question", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("father", "You were out late. Where were you?", false, 0f),
                        Line("nicky", "Around.", false, 0f),
                        Line("father", "Don't lie to me, son. Was it him? Was it C?", false, 0f),
                    },
                    new[]
                    {
                        Choice("lie", "Lie to him.", "father_lie"),
                        Choice("truth", "Tell the truth.", "father_truth"),
                        Choice("resignation", "Say nothing. Walk away.", "father_resignation"),
                    }, null),

                Node("father_lie", DialoguePresentation.InScene,
                    new[] { Line("nicky", "It was nothing. Just cards with the guys.", false, 0f) },
                    null, ""),
                Node("father_truth", DialoguePresentation.InScene,
                    new[] { Line("nicky", "It was C. I did a job for him.", false, 0f) },
                    null, ""),
                Node("father_resignation", DialoguePresentation.InScene,
                    new[]
                    {
                        Line("narrator", "Nicky says nothing. The door closes behind him.", true, 2f),
                    },
                    null, ""),

                // Repeat line once the job is assigned (no re-give).
                Node("C_Repeat", DialoguePresentation.InScene,
                    new[] { Line("c", "Go handle it, kid.", false, 0f) },
                    null, ""),
            };
            db.Validate();
            return db;
        }

        private static SpeakerData Speaker(string id, string displayName, Color color)
        {
            var s = ScriptableObject.CreateInstance<SpeakerData>();
            s.speakerId = id;
            s.displayName = displayName;
            s.subtitleColor = color;
            return s;
        }

        private static DialogueLine Line(string speakerId, string text, bool autoAdvance, float delay)
        {
            return new DialogueLine
            {
                speakerId = speakerId,
                text = text,
                voiceClip = null,
                autoAdvance = autoAdvance,
                autoAdvanceDelay = delay
            };
        }

        private static DialogueChoice Choice(string choiceId, string choiceText, string resultNodeId)
        {
            return new DialogueChoice { choiceId = choiceId, choiceText = choiceText, resultNodeId = resultNodeId };
        }

        private static DialogueNode Node(string nodeId, DialoguePresentation presentation,
            DialogueLine[] lines, DialogueChoice[] choices, string nextNodeId)
        {
            return new DialogueNode
            {
                nodeId = nodeId,
                presentation = presentation,
                lines = lines ?? new DialogueLine[0],
                choices = choices ?? new DialogueChoice[0],
                nextNodeId = nextNodeId ?? ""
            };
        }
    }
}
