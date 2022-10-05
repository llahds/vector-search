using System.Text.RegularExpressions;

namespace VectorSearch.Common.Text
{
    public class Tokenizer
    {
        private readonly HashSet<string> stopwords = new HashSet<string>(File.ReadLines("./Services/Text/stopwords.txt").ToHashSet());

        public Tokenizer()
        {

        }

        public IEnumerable<string> Tokenize(string text)
        {
            foreach (var token in Regex
                .Split(text, @"(\W+)")
                .Select(T => T.ToLower())
                .Where(T => T.Length > 2 && !Regex.IsMatch(T, "[^a-z]") && this.stopwords.Contains(T) == false))
            {
                yield return token;
            }
        }
    }
}
