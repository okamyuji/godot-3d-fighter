using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests;

/// <summary>C-14。Coreの公開APIに<c>float</c>と<c>double</c>が現れないことを確かめる。</summary>
public sealed class PublicApiTests
{
    [Fact]
    public void PublicApiDoesNotExposeFloatOrDouble()
    {
        var coreAssembly = typeof(Fix16).Assembly;
        var violations = new List<string>();

        foreach (var type in coreAssembly.GetExportedTypes())
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                CheckType(field.FieldType, $"{type.FullName}.{field.Name} (field)", violations);
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                CheckType(property.PropertyType, $"{type.FullName}.{property.Name} (property)", violations);
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                CheckType(method.ReturnType, $"{type.FullName}.{method.Name} (return)", violations);
                foreach (var parameter in method.GetParameters())
                {
                    CheckType(parameter.ParameterType, $"{type.FullName}.{method.Name}({parameter.Name}) (parameter)", violations);
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join('\n', violations));
    }

    private static void CheckType(Type type, string location, List<string> violations)
    {
        var effective = type.IsByRef || type.IsPointer ? type.GetElementType() ?? type : type;
        var element = effective.HasElementType ? effective.GetElementType() ?? effective : effective;

        if (element == typeof(float) || element == typeof(double))
        {
            violations.Add($"{location}: {element.Name}");
        }

        if (element.IsGenericType)
        {
            foreach (var argument in element.GetGenericArguments())
            {
                CheckType(argument, location, violations);
            }
        }
    }
}
