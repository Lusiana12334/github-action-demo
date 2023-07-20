using System.ComponentModel;

namespace PEXC.Case.Infrastructure.Converter;

public static class TypeConverterExtension
{
    public static void AddTypeDescriptors()
    {
        // net 6 does not have support for converting DateOnly struct, we have to do it by our own if we want to have DateOnly parameters in configuration options
        TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));
    }
}
