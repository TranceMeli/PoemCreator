using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poems
{
    public class List
    {
        public string sentenceName;
        public List<string> sentences;
        public int userChoice;
        public bool gameLoop = true;
        public string input;

        public List()
        {
            sentences = new List<string>();
            sentences.Add("The moonlight danced upon the tranquil sea");
            sentences.Add("In the garden of dreams, I found my solace");
            sentences.Add("Whispers of the wind carried secrets untold");
            sentences.Add("Your laughter was the melody of my heart");
            sentences.Add("Beneath the ancient oak, time stood still");
            sentences.Add("Stars above, like diamonds in the night");
            sentences.Add("In the silence, I heard the echoes of love");
            sentences.Add("The dawn broke with a promise of new beginnings");
            sentences.Add("Through the storm, we found our strength");
            sentences.Add("Memories linger like shadows in the twilight");
            sentences.Add("Your love is the light that guides me through the darkest nights");
            sentences.Add("In your eyes, I found my forever home.");
            sentences.Add("Every heartbeat whispers your name");
            sentences.Add("With you, every moment is a beautiful eternity");
            sentences.Add("Your smile is the sunrise that brightens my soul");
            sentences.Add("The river sang a lullaby beneath the silver moon");
            sentences.Add("Petals fell like confetti on the path of dreams");
            sentences.Add("A gentle breeze carried memories of yesterday");
            sentences.Add("The horizon blushed with the colors of hope");
            sentences.Add("Raindrops kissed the earth with tender grace");
            sentences.Add("Your voice lingered like a melody in the night");
            sentences.Add("The forest whispered tales of forgotten times");
            sentences.Add("Sunlight streamed through the windows of my heart");
            sentences.Add("Moments with you are fragments of eternity");
            sentences.Add("The night sky cradled my restless thoughts");
            sentences.Add("In every shadow, I found a spark of light");
            sentences.Add("Dreams took flight on wings of golden fire");
            sentences.Add("Your touch painted warmth across my soul");
            sentences.Add("Leaves danced gracefully to the rhythm of the wind");
            sentences.Add("The ocean’s roar mirrored my hidden longing");
            sentences.Add("Stars winked secrets only the heart could hear");
            sentences.Add("Time slipped quietly through the hourglass of us");
            sentences.Add("Your laughter scattered like sunlight through the trees");
            sentences.Add("In the quiet, love spoke without a sound");
            sentences.Add("The dawn whispered promises of a brand-new day");

        }
        public void Shuffle()
        {
            do
            {
                Random random = new Random();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[--------Welcome to the poem generator--------]");
                Console.ResetColor();
                Console.WriteLine("How many sentences, enter a valid number, 'Q' to quit");

                input = Console.ReadLine();
                Console.WriteLine();

                if (string.Equals(input, "Q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Goodbye");
                    break;
                }

                bool valid = int.TryParse(input, out userChoice);



                if (valid && userChoice >= 1 && userChoice <= 20)
                {

                    var selectedSentences = sentences.OrderBy(x => random.Next()).Distinct().Take(userChoice).ToList();

                    foreach (var p in selectedSentences)
                    {
                        Console.WriteLine(p);
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Invalid input, enter a number");
                }
            } while (gameLoop);
        }
    }
}
