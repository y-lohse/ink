﻿using System.Collections.Generic;

namespace Ink.Parsed
{
    internal class FunctionCall : Expression
    {
        public string name { get { return _proxyDivert.target.firstComponent; } }
        public List<Expression> arguments { get { return _proxyDivert.arguments; } }
        public Runtime.Divert runtimeDivert { get { return _proxyDivert.runtimeDivert; } }
        public bool isChoiceCount { get { return name == "CHOICE_COUNT"; } }
        public bool isTurnsSince { get { return name == "TURNS_SINCE"; } }
        public bool isRandom { get { return name == "RANDOM"; } } 
        public bool isSeedRandom { get { return name == "SEED_RANDOM"; } }
        public bool shouldPopReturnedValue;

        public FunctionCall (string functionName, List<Expression> arguments)
        {
            _proxyDivert = new Parsed.Divert(new Path(functionName), arguments);
            _proxyDivert.isFunctionCall = true;
            AddContent (_proxyDivert);
        }

        public override void GenerateIntoContainer (Runtime.Container container)
        {
            if (isChoiceCount) {

                if (arguments.Count > 0)
                    Error ("The CHOICE_COUNT() function shouldn't take any arguments");

                container.AddContent (Runtime.ControlCommand.ChoiceCount ());

            } else if (isTurnsSince) {

                var divertTarget = arguments [0] as DivertTarget;
                var variableDivertTarget = arguments [0] as VariableReference;

                if (arguments.Count != 1 || (divertTarget == null && variableDivertTarget == null)) {
                    Error ("The TURNS_SINCE() function should take one argument: a divert target to the target knot, stitch, gather or choice you want to check. e.g. TURNS_SINCE(-> myKnot)");
                    return;
                }

                if (divertTarget) {
                    _turnCountDivertTarget = divertTarget;
                    AddContent (_turnCountDivertTarget);

                    _turnCountDivertTarget.GenerateIntoContainer (container);
                } else {
                    _turnCountVariableReference = variableDivertTarget;
                    AddContent (_turnCountVariableReference);

                    _turnCountVariableReference.GenerateIntoContainer (container);

                    if (!story.countAllVisits) {
                        Error ("Attempting to get TURNS_SINCE for a variable target without -c compiler option. You need the compiler switch turned on so that it can track turn counts for everything, not just those you directly reference.");
                    }
                }


                container.AddContent (Runtime.ControlCommand.TurnsSince ());
            } 

            else if (isRandom) {
                if (arguments.Count != 2) 
                    Error ("RANDOM should take 2 parameters: a minimum and a maximum integer");

                // We can type check single values, but not complex expressions
                for (int arg = 0; arg < arguments.Count; arg++) {
                    if (arguments [arg] is Number) {
                        var num = arguments [arg] as Number;
                        if (!(num.value is int)) {
                            string paramName = arg == 0 ? "minimum" : "maximum";
                            Error ("RANDOM's "+paramName+" parameter should be an integer");
                        }
                    }

                    arguments[arg].GenerateIntoContainer (container);
                }

                container.AddContent (Runtime.ControlCommand.Random ());
            } 

            else if (isSeedRandom) {
                if (arguments.Count != 1)
                    Error ("SEED_RANDOM should take 1 parameter - an integer seed");

                var num = arguments [0] as Number;
                if (num && !(num.value is int)) {
                    Error ("SEED_RANDOM's parameter should be an integer seed");
                }

                arguments [0].GenerateIntoContainer (container);

                container.AddContent (Runtime.ControlCommand.SeedRandom ());
            }

              // Normal function call
              else {
                container.AddContent (_proxyDivert.runtimeObject);
            }

            // Function calls that are used alone on a tilda-based line:
            //  ~ func()
            // Should tidy up any returned value from the evaluation stack,
            // since it's unused.
            if (shouldPopReturnedValue)
                container.AddContent (Runtime.ControlCommand.PopEvaluatedValue ());
        }

        public override void ResolveReferences (Story context)
        {
            base.ResolveReferences (context);

            if( _turnCountDivertTarget ) {
                var divert = _turnCountDivertTarget.divert;
                var attemptingTurnCountOfVariableTarget = divert.runtimeDivert.variableDivertName != null;

                if( attemptingTurnCountOfVariableTarget ) {
                    Error("When getting the TURNS_SINCE() of a variable target, remove the '->' - i.e. it should just be TURNS_SINCE("+divert.runtimeDivert.variableDivertName+")");
                    return;
                }

                var targetObject = divert.targetContent;
                if( targetObject == null ) {
                    if( !attemptingTurnCountOfVariableTarget ) {
                        Error("Failed to find target for TURNS_SINCE: '"+divert.target+"'");
                    }
                } else {
                    targetObject.containerForCounting.turnIndexShouldBeCounted = true;
                }
            }

            else if( _turnCountVariableReference ) {
                var runtimeVarRef = _turnCountVariableReference.runtimeVarRef;
                if( runtimeVarRef.pathForCount != null ) {
                    Error("Should be TURNS_SINCE(-> "+_turnCountVariableReference.name+"). Without the '->' it expects a variable target");
                }
            }
        }

        public static bool IsBuiltIn(string name) 
        {
            return name == "CHOICE_COUNT" || name == "TURNS_SINCE" || name == "RANDOM" || name == "SEED_RANDOM";
        }

        public override string ToString ()
        {
            var strArgs = string.Join (", ", arguments);
            return string.Format ("{0}({1})", name, strArgs);
        }
            
        Parsed.Divert _proxyDivert;
        Parsed.DivertTarget _turnCountDivertTarget;
        Parsed.VariableReference _turnCountVariableReference;
    }
}

