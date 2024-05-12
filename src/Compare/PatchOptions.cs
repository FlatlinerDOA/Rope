/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
*
* Copyright 2024 Andrew Chisholm (FlatlinerDOA).
* https://github.com/FlatlinerDOA/Rope
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

/// <summary>
/// Defines options for creating and applying patches, extends <see cref="MatchOptions"/>.
/// </summary>
/// <param name="DeleteThreshold">When deleting a large block of text (over ~64 characters), how close
/// do the contents have to be to match the expected contents. 
/// (0.0 = perfection, 1.0 = very loose).
/// Note that Match_Threshold controls
/// how closely the end points of a delete need to match.</param>
/// <param name="Margin">Chunk size for context length.</param
/// <param name="MatchThreshold">
/// At what point is no match declared (0.0 = perfection, 1.0 = very loose).
/// </param>
/// <param name="MatchDistance">
/// How far to search for a match (0 = exact location, 1000+ = broad match).
/// A match this many characters away from the expected location will add
/// 1.0 to the score (0.0 is a perfect match).
/// </param>
public record class PatchOptions(float DeleteThreshold, short Margin, short MaxLength, float MatchThreshold, int MatchDistance) : MatchOptions(MatchThreshold, MatchDistance)
{
    public static new readonly PatchOptions Default = new(0.5f, 4, 32, 0.5f, 1000);
}
