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
/// Enumeration of the possible diff operations between two lists.
/// </summary>
public enum Operation
{
    /// <summary>
    /// Diff operation representing a removed item.
    /// </summary>
    Delete = -1,
    
    /// <summary>
    /// Diff operation representing an unchanged item.
    /// </summary>
    Equal = 0,

    /// <summary>
    /// Diff operation representing an inserted item.
    /// </summary>
    Insert = 1
}
