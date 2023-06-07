using System;
using AutoFixture;
using AutoFixture.Dsl;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using JetBrains.Annotations;
using Reloaded.Memory.Buffers.Structs.Internal;

namespace Reloaded.Memory.Buffers.Tests.Attributes;

/// <summary>
///     Custom <see cref="AutoDataAttribute" /> with support for
///     creating a dummies for locator headers.
/// </summary>
[PublicAPI]
public class AutoLocatorHeader : AutoDataAttribute
{
    public AutoLocatorHeader(bool randomizeHeader) : base(() =>
    {
        var ret = new Fixture();
        ret.Customize<LocatorHeader>(composer =>
        {
            IPostprocessComposer<LocatorHeader>? result = composer.FromFactory(() => new LocatorHeader());
            if (!randomizeHeader)
                result = result.OmitAutoProperties();

            return result;
        });

        ret.Customizations.Add(new NullLocatorHeaderPointerBuilder());
        return ret;
    }) { }

    private class NullLocatorHeaderPointerBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is not Type type || type != typeof(LocatorHeader*))
                return new NoSpecimen();

            return null!;
        }
    }
}
