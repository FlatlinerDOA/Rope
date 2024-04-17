﻿/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
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
*/

namespace Rope.Compare;

using System;

/**
 * Class representing one diff operation.
 */
public readonly record struct Diff(Operation Operation, Rope<char> Text)
{
    public Diff(Operation operation, string text) : this(operation, text.AsMemory())
    {
    }

    public Diff(Operation operation, ReadOnlyMemory<char> text) : this(operation, new Rope<char>(text))
    {
    }

    public Diff WithOperation(Operation op) => this with { Operation = op };
    public Diff WithText(Rope<char> text) => this with { Text = text };

    public Diff Append(Rope<char> text) => this with { Text = this.Text.AddRange(text) };
    public Diff Prepend(Rope<char> text) => this with { Text = text.AddRange(this.Text) };
}
