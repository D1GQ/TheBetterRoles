using Hazel;
using System.Collections;
using System.Reflection;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Interfaces;

namespace TheBetterRoles.Items.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal class SyncVarAttribute : Attribute
{
    private MemberInfo? _targetMember;
    private object? _lastValue;
    private ushort _identifier;

    internal ushort Identifier => _identifier;

    internal void Setup(MemberInfo memberInfo)
    {
        _targetMember = memberInfo;

        _identifier = _targetMember switch
        {
            FieldInfo fieldInfo => Utils.GetHashUInt16(fieldInfo.Name + fieldInfo.FieldType.FullName),
            PropertyInfo propertyInfo => Utils.GetHashUInt16(propertyInfo.Name + propertyInfo.PropertyType.FullName),
            _ => 0
        };
    }

    private object? GetCurrentValue(object instance)
    {
        if (_targetMember == null) return null;

        return _targetMember switch
        {
            FieldInfo fieldInfo => fieldInfo.GetValue(instance),
            PropertyInfo propertyInfo => propertyInfo.GetValue(instance),
            _ => null
        };
    }

    private void SetValue(object instance, object? value)
    {
        if (_targetMember == null) return;

        switch (_targetMember)
        {
            case FieldInfo fieldInfo:
                fieldInfo.SetValue(instance, value);
                break;
            case PropertyInfo propertyInfo:
                propertyInfo.SetValue(instance, value);
                break;
        }
        _lastValue = value;
    }

    internal bool IsDirty(object instance)
    {
        if (_targetMember == null) return false;

        var currentValue = GetCurrentValue(instance);
        return !Equals(currentValue, _lastValue);
    }

    private void Serialize(MessageWriter writer, object instance)
    {
        if (_targetMember == null || _identifier == 0) return;

        var currentValue = GetCurrentValue(instance);
        if (currentValue is IEnumerable enumerable)
            currentValue = enumerable;
        writer.Write(_identifier);
        writer.WriteFast(currentValue);
        _lastValue = currentValue;
    }

    private bool Deserialize(MessageReader reader, object instance, ushort expectedId)
    {
        if (_targetMember == null || _identifier != expectedId) return false;

        object? value = _targetMember switch
        {
            FieldInfo fieldInfo => reader.ReadFast(fieldInfo.FieldType),
            PropertyInfo propertyInfo => reader.ReadFast(propertyInfo.PropertyType),
            _ => null
        };

        if (value != null)
        {
            SetValue(instance, value);
            return true;
        }
        return false;
    }

    internal static bool AnyDirty(INetworkClass instance) => instance.SyncVars.Any(sv => sv.IsDirty(instance));

    internal static void SerializeAll(MessageWriter writer, INetworkClass instance)
    {
        var dirtyAttributes = instance.SyncVars.Where(attr => attr.IsDirty(instance)).ToList();

        writer.Write(dirtyAttributes.Count);
        foreach (var attr in dirtyAttributes)
        {
            attr.Serialize(writer, instance);
        }
    }

    internal static void DeserializeAll(MessageReader reader, INetworkClass instance)
    {
        int count = reader.ReadInt32();
        var attrDictionary = instance.SyncVars.ToDictionary(attr => attr.Identifier);

        for (int i = 0; i < count; i++)
        {
            ushort id = reader.ReadUInt16();
            if (attrDictionary.TryGetValue(id, out var attr))
            {
                attr.Deserialize(reader, instance, id);
            }
        }
    }
}