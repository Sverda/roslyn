﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    sealed class InheritanceDistanceComparer : IComparer<ITypeSymbol>
    {
        private readonly ITypeSymbol _baseType;
        private readonly SemanticModel _semanticModel;

        public InheritanceDistanceComparer(SemanticModel semanticModel, ITypeSymbol baseType)
        {
            _semanticModel = semanticModel;
            _baseType = baseType;
        }

        public int Compare(ITypeSymbol x, ITypeSymbol y)
        {
            var xDist = GetInheritanceDistance(x);
            var yDist = GetInheritanceDistance(y);
            return xDist.CompareTo(yDist);
        }

        /// <summary>
        /// Calculate the inheritance distance between _baseType and derivedType.
        /// </summary>
        private int GetInheritanceDistanceRecursive(ITypeSymbol? derivedType)
        {
            if (derivedType == null)
                return int.MaxValue;
            if (derivedType.Equals(_baseType))
                return 0;

            var distance = GetInheritanceDistanceRecursive(derivedType.BaseType);

            if (derivedType.Interfaces.Length != 0)
            {
                foreach (var interfaceType in derivedType.Interfaces)
                {
                    distance = Math.Min(GetInheritanceDistanceRecursive(interfaceType), distance);
                }
            }

            return distance == int.MaxValue ? distance : distance + 1;
        }

        /// <summary>
        /// Wrapper funtion of [GetInheritanceDistance], also consider the class with explicit conversion operator
        /// has the highest priority.
        /// </summary>
        private int GetInheritanceDistance(ITypeSymbol type)
        {
            var conversion = _semanticModel.Compilation.ClassifyCommonConversion(_baseType, type);

            // If the node has the explicit conversion operator, then it has the shortest distance,
            // since explicit conversion operator is defined by users and has the highest priority 
            var distance = conversion.IsUserDefined ? 0 : GetInheritanceDistanceRecursive(type);
            return distance;
        }
    }
}
