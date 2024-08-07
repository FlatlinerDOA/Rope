﻿/*
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
*/

namespace Rope.Compare;

using System;

/// <summary>
/// Record struct representing one diff operation.
/// </summary>
/// <param name="Operation">The operation being performed.</param>
/// <param name="Data">The content of the operation.</param>
public readonly record struct Diff<T>(Operation Operation, Rope<T> Items) where T : IEquatable<T>
{
    public Diff<T> WithOperation(Operation op) => this with { Operation = op };

    public Diff<T> WithItems(Rope<T> items) => this with { Items = items };

    public Diff<T> Append(Rope<T> items) => this with { Items = this.Items.AddRange(items) };

    public Diff<T> Prepend(Rope<T> items) => this with { Items = items.AddRange(this.Items) };

    public bool Equals(Diff<T> other) => this.Operation == other.Operation && this.Items == other.Items;

    public override int GetHashCode() => HashCode.Combine(this.Operation, this.Items.GetHashCode());
}
