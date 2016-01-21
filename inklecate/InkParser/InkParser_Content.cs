﻿using Ink.Parsed;
using System.Text;
using System.Collections.Generic;

namespace Ink
{
    internal partial class InkParser
    {
        void TrimEndWhitespace(List<Parsed.Object> mixedTextAndLogicResults, bool terminateWithSpace)
        {
            // Trim whitespace from end
            if (mixedTextAndLogicResults.Count > 0) {
                var lastObj = mixedTextAndLogicResults[mixedTextAndLogicResults.Count-1];
                if (lastObj is Text) {
                    var text = (Text)lastObj;
                    text.text = text.text.TrimEnd (' ', '\t');

                    if (terminateWithSpace)
                        text.text += " ";
                }
            }
        }

        protected List<Parsed.Object> LineOfMixedTextAndLogic()
        {
            var result = Parse(MixedTextAndLogic);
            if (result == null || result.Count == 0)
                return null;

            // Trim whitepace from start
            var firstText = result[0] as Text;
            if (firstText) {
                firstText.text = firstText.text.TrimStart(' ', '\t');
                if (firstText.text.Length == 0) {
                    result.RemoveAt (0);
                }

                if (firstText.text.StartsWith ("return")) {
                    Warning ("Do you need a '~' before 'return'? If not, perhaps use a glue: <> (since it's lowercase) or rewrite somehow?");
                }
            }
            if (result.Count == 0)
                return null;

            var lastObj = result [result.Count - 1];
            if (!(lastObj is Divert)) {
                TrimEndWhitespace (result, terminateWithSpace:false);
            }

            // Add newline since it's the end of the line
            result.Add (new Text ("\n"));

            Expect(EndOfLine, "end of line", recoveryRule: SkipToNextLine);

            return result;
        }

        protected List<Parsed.Object> MixedTextAndLogic()
        {
            // Check for disallowed "~" within this context
            var disallowedTilda = ParseObject(Spaced(String("~")));
            if (disallowedTilda != null)
                Error ("You shouldn't use a '~' here - tildas are for logic that's on its own line. To do inline logic, use { curly braces } instead");

            // Either, or both interleaved
            var results = Interleave<Parsed.Object>(Optional (ContentText), Optional (InlineLogicOrGlue));

            // Terminating divert?
            // (When parsing content for the text of a choice, diverts aren't allowed.
            //  The divert on the end of the body of a choice is handled specially.)
            if (!_parsingChoice) {

                var divertsOrOnwards = OneOf(MultiStepTunnelDivert, TunnelOnwards);
                if (divertsOrOnwards != null) {

                    // May not have had any results at all if there's *only* a divert!
                    if (results == null)
                        results = new List<Parsed.Object> ();

                    TrimEndWhitespace (results, terminateWithSpace:true);

                    var diverts = divertsOrOnwards as List<Divert>;
                    if (diverts != null)
                        results.AddRange (diverts);
                    else
                        results.Add (divertsOrOnwards as TunnelOnwards);
                }

            }
                
            if (results == null)
                return null;

            return results;
        }

        protected Parsed.Text ContentText()
        {
            return ContentTextAllowingEcapeChar ();
        }

        protected Parsed.Text ContentTextAllowingEcapeChar()
        {
            StringBuilder sb = null;

            do {
                var str = Parse(ContentTextNoEscape);
                bool gotEscapeChar = ParseString(@"\") != null;

                if( gotEscapeChar || str != null ) {
                    if( sb == null ) {
                        sb = new StringBuilder();
                    }

                    if( str != null ) {
                        sb.Append(str);
                    }

                    if( gotEscapeChar ) {
                        char c = ParseSingleCharacter();
                        sb.Append(c);
                    }

                } else {
                    break;
                }

            } while(true);

            if (sb != null ) {
                return new Parsed.Text (sb.ToString ());

            } else {
                return null;
            }
        }

        // Content text is an unusual parse rule compared with most since it's
        // less about saying "this is is the small selection of stuff that we parse"
        // and more "we parse ANYTHING except this small selection of stuff".
        protected string ContentTextNoEscape()
        {
            // Eat through text, pausing at the following characters, and
            // attempt to parse the nonTextRule.
            // "-": possible start of divert or start of gather
            // "<": possible start of glue
            if (_nonTextPauseCharacters == null) {
                _nonTextPauseCharacters = new CharacterSet ("-<");
            }

            // If we hit any of these characters, we stop *immediately* without bothering to even check the nonTextRule
            // "{" for start of logic
            // "|" for mid logic branch
            if (_nonTextEndCharacters == null) {
                _nonTextEndCharacters = new CharacterSet ("{}|\n\r\\");
                _notTextEndCharactersChoice = new CharacterSet (_nonTextEndCharacters);
                _notTextEndCharactersChoice.AddCharacters ("[]");
                _notTextEndCharactersString = new CharacterSet (_nonTextEndCharacters);
                _notTextEndCharactersString.AddCharacters ("\"");
            }

            // When the ParseUntil pauses, check these rules in case they evaluate successfully
            ParseRule nonTextRule = () => OneOf (ParseDivertArrow, EndOfLine, Glue);

            CharacterSet endChars = null;
            if (parsingStringExpression) {
                endChars = _notTextEndCharactersString;
            } 
            else if (_parsingChoice) {
                endChars = _notTextEndCharactersChoice;
            } 
            else {
                endChars = _nonTextEndCharacters;
            }

            string pureTextContent = ParseUntil (nonTextRule, _nonTextPauseCharacters, endChars);
            if (pureTextContent != null ) {
                return pureTextContent;

            } else {
                return null;
            }

        }

        CharacterSet _nonTextPauseCharacters;
        CharacterSet _nonTextEndCharacters;
        CharacterSet _notTextEndCharactersChoice;
        CharacterSet _notTextEndCharactersString;



    }
}

