using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Judge.Codes
{
    public class CodesSet : IReadOnlyCollection<Code>
    {
        private readonly List<Code> _codes = new List<Code>();
        private readonly string[] _errors;

        public CodesSet(DirectoryInfo codesDir)
        {
            var errors = new List<string>();
            foreach (var code in codesDir.GetFiles())
            {
                var expectedResMatch = Regex.Match(code.Name, @".*_(.*)\..+", RegexOptions.IgnoreCase);
                if (expectedResMatch.Success)
                    _codes.Add(new Code(code, expectedResMatch.Groups[1].Value));
                else
                    errors.Add($"Can't parse the code filename '{code.Name}'");
            }
            _errors = errors.ToArray();
        }

        public int Count => _codes.Count;

        public IEnumerator<Code> GetEnumerator()
        {
            return _codes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _codes.GetEnumerator();
        }

        public bool HasErrors => _errors.Length > 0;
        public string[] GetErrors()
        {
            return _errors;
        }
    }
}
