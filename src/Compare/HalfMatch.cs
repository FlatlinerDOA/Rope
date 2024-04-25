/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
* Copyright 2024 Andrew Chisholm.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
* 
*/

namespace Rope.Compare;

using System;

internal sealed record class HalfMatch<T>(Rope<T> Text1Prefix, Rope<T> Text1Suffix, Rope<T> Text2Prefix, Rope<T> Text2Suffix, Rope<T> Common) where T : IEquatable<T>
{
    //public HalfMatch(string Text1Prefix, string Text1Suffix, string Text2Prefix, string Text2Suffix, string Common) : this(Text1Prefix.ToRope(), Text1Suffix.ToRope(), Text2Prefix.ToRope(), Text2Suffix.ToRope(), Common.ToRope())
    //{
    //}

    public HalfMatch<T> Swap() => new HalfMatch<T>(this.Text2Prefix, this.Text2Suffix, this.Text1Prefix, this.Text1Suffix, this.Common);
}
