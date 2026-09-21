using System.Collections.Generic;

namespace Campusano.Dialogue
{
    /// <summary>
    /// Hand-authored Episode 1 dialogue trees, built in code so the episode is
    /// playable with zero asset authoring. Voice clips are null until the
    /// ElevenLabs pass; text carries the scenes.
    /// </summary>
    public static class Ep1DialogueBank
    {
        public static IEnumerable<DialogueNode> Build()
        {
            return new List<DialogueNode>
            {
                // ---- Ep1-M1-S6: C gives Nicky the Mook job (linear) ----
                new DialogueNode
                {
                    nodeId = "C_First_Talk",
                    lines = new[]
                    {
                        Line("C", "Look who it is. The kid who didn't rat."),
                        Line("C", "I got a job for you. Guy named Mook owes me two grand. He's late."),
                        Line("C", "Go collect it. Back room, card game. You know where to find him."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "",
                    completesObjectiveOnEnd = true,
                },

                // ---- Ep1-M2-S3: confronting Mook (branching) ----
                new DialogueNode
                {
                    nodeId = "Mook_Confrontation",
                    lines = new[]
                    {
                        Line("Nicky", "Mook looks up from the cards. Sweat on his forehead."),
                    },
                    choices = new[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "\"Mook. You got C's money?\"",
                            targetNodeId = "Mook_Intimidate",
                            flagToSet = DialogueChoiceFlag.Ep1_M2_Intimidate,
                        },
                        new DialogueChoice
                        {
                            choiceText = "\"Hey, Mook. Let's take a walk.\"",
                            targetNodeId = "Mook_Walk",
                            flagToSet = DialogueChoiceFlag.Ep1_M2_PrivateConversation,
                        },
                    },
                    nextNodeId = "",
                    completesObjectiveOnEnd = false,
                },
                new DialogueNode
                {
                    nodeId = "Mook_Intimidate",
                    lines = new[]
                    {
                        Line("Mook", "I... I need more time. Tell C — tell him Friday, I swear on my mother."),
                        Line("Nicky", "One of his friends shifts in his chair. A hand moves under the table."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "Mook_Knife",
                    completesObjectiveOnEnd = false,
                },
                new DialogueNode
                {
                    nodeId = "Mook_Walk",
                    lines = new[]
                    {
                        Line("Mook", "A walk? I ain't going nowhere with —"),
                        Line("Nicky", "He stands anyway. They all saw him stand. That's the problem."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "Mook_Knife",
                    completesObjectiveOnEnd = false,
                },
                new DialogueNode
                {
                    nodeId = "Mook_Knife",
                    lines = new[]
                    {
                        Line("", "Mook's hand comes up with a knife. Everything gets very quiet."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "",
                    completesObjectiveOnEnd = true, // the shooting beat follows
                },

                // ---- Ep1-DP-S3: the father (three-way choice) ----
                new DialogueNode
                {
                    nodeId = "Father_Choice",
                    lines = new[]
                    {
                        Line("Father", "I heard about Mook. You were there?"),
                    },
                    choices = new[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "Lie. \"I don't know what you're talking about.\"",
                            targetNodeId = "Father_Lie",
                            flagToSet = DialogueChoiceFlag.Ep1_DP_Lie,
                        },
                        new DialogueChoice
                        {
                            choiceText = "Tell the truth.",
                            targetNodeId = "Father_Truth",
                            flagToSet = DialogueChoiceFlag.Ep1_DP_Truth,
                        },
                        new DialogueChoice
                        {
                            choiceText = "Say nothing. Just look at him.",
                            targetNodeId = "Father_Resign",
                            flagToSet = DialogueChoiceFlag.Ep1_DP_Resignation,
                        },
                    },
                    nextNodeId = "",
                    completesObjectiveOnEnd = false,
                },
                new DialogueNode
                {
                    nodeId = "Father_Lie",
                    lines = new[]
                    {
                        Line("Father", "You're a liar. But you're my son. I love you anyway."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "",
                    completesObjectiveOnEnd = true,
                },
                new DialogueNode
                {
                    nodeId = "Father_Truth",
                    lines = new[]
                    {
                        Line("Father", "The working man is the toughest man in the world. You forgot that."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "",
                    completesObjectiveOnEnd = true,
                },
                new DialogueNode
                {
                    nodeId = "Father_Resign",
                    lines = new[]
                    {
                        Line("Father", "You're born into this shit. You are what you are. But you don't have to be."),
                    },
                    choices = new DialogueChoice[0],
                    nextNodeId = "",
                    completesObjectiveOnEnd = true,
                },
            };
        }

        private static DialogueLine Line(string speaker, string text)
        {
            return new DialogueLine
            {
                speakerName = speaker,
                text = text,
                voiceClip = null,
                autoAdvanceDelay = 0f,
            };
        }
    }
}
