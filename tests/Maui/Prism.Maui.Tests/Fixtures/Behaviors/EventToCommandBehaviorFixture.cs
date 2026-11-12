using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Prism.Behaviors;
using Prism.Maui.Tests.Mocks;
using Prism.Maui.Tests.Mocks.Behaviors;

namespace Prism.Maui.Tests.Fixtures.Behaviors;

public class EventToCommandBehaviorFixture
{
    /// <summary>Mirrors <see cref="SelectionChangedEventArgs"/> shape for tests (MAUI ctor is not public).</summary>
    private sealed class FakeSelectionChangedEventArgs : EventArgs
    {
        public FakeSelectionChangedEventArgs(IReadOnlyList<object> previousSelection, IReadOnlyList<object> currentSelection)
        {
            PreviousSelection = previousSelection;
            CurrentSelection = currentSelection;
        }

        public IReadOnlyList<object> PreviousSelection { get; }
        public IReadOnlyList<object> CurrentSelection { get; }
    }

    /// <summary>Minimal EventArgs for nested property-path tests (no Item on selection args).</summary>
    private sealed class TestNestedArgs : EventArgs
    {
        public object Item => null;
    }

    private class SelectionChangedEventArgsConverter : IValueConverter
    {
        private readonly bool _returnParameter;

        public bool HasConverted { get; private set; }

        public SelectionChangedEventArgsConverter(bool returnParameter)
        {
            _returnParameter = returnParameter;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HasConverted = true;
            if (_returnParameter)
                return parameter;
            var prop = value?.GetType().GetRuntimeProperty(nameof(FakeSelectionChangedEventArgs.CurrentSelection));
            return prop?.GetValue(value) is IReadOnlyList<object> list ? list.FirstOrDefault() : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public EventToCommandBehaviorFixture()
    {
        DispatcherProvider.SetCurrent(TestDispatcher.Provider);
        _ = MauiApp.CreateBuilder()
            .UseMauiApp<Application>()
            .Build();
    }

    [Fact]
    public void Command_OrderOfExecution()
    {
        const string commandParameter = "ItemProperty";
        var executedCommand = false;
        var converter = new SelectionChangedEventArgsConverter(false);
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            EventArgsConverter = converter,
            CommandParameter = commandParameter,
            Command = new DelegateCommand<string>(o =>
            {
                executedCommand = true;
                Assert.NotNull(o);
                Assert.Equal(commandParameter, o);
                Assert.False(converter.HasConverted);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, new FakeSelectionChangedEventArgs(Array.Empty<object>(), new[] { commandParameter }));
        Assert.True(executedCommand);
    }

    [Fact]
    public void Command_Converter()
    {
        const string item = "ItemProperty";
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            EventArgsConverter = new SelectionChangedEventArgsConverter(false),
            Command = new DelegateCommand<string>(o =>
            {
                executedCommand = true;
                Assert.NotNull(o);
                Assert.Equal(item, o);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, new FakeSelectionChangedEventArgs(Array.Empty<object>(), new[] { item }));
        Assert.True(executedCommand);
    }

    [Fact]
    public void Command_ConverterWithConverterParameter()
    {
        const string item = "ItemProperty";
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            EventArgsConverter = new SelectionChangedEventArgsConverter(true),
            EventArgsConverterParameter = item,
            Command = new DelegateCommand<string>(o =>
            {
                executedCommand = true;
                Assert.NotNull(o);
                Assert.Equal(item, o);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, new FakeSelectionChangedEventArgs(Array.Empty<object>(), Array.Empty<object>()));
        Assert.True(executedCommand);
    }

    [Fact]
    public void Command_ExecuteWithParameter()
    {
        const string item = "ItemProperty";
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            CommandParameter = item,
            Command = new DelegateCommand<string>(o =>
            {
                executedCommand = true;
                Assert.NotNull(o);
                Assert.Equal(item, o);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, new FakeSelectionChangedEventArgs(Array.Empty<object>(), Array.Empty<object>()));
        Assert.True(executedCommand);
    }

    [Fact]
    public void Command_EventArgsParameterPath()
    {
        const string item = "ItemProperty";
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            EventArgsParameterPath = "CurrentSelection",
            Command = new DelegateCommand<IReadOnlyList<object>>(o =>
            {
                executedCommand = true;
                Assert.NotNull(o);
                Assert.Single(o);
                Assert.Equal(item, o[0]);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, new FakeSelectionChangedEventArgs(Array.Empty<object>(), new[] { item }));
        Assert.True(executedCommand);
    }

    //[Fact]
    //public void Command_EventArgsParameterPath_Nested()
    //{
    //    dynamic item = new
    //    {
    //        AProperty = "Value"
    //    };
    //    var executedCommand = false;
    //    var behavior = new EventToCommandBehaviorMock
    //    {
    //        EventName = "SelectionChanged",
    //        EventArgsParameterPath = "Item.AProperty",
    //        Command = new DelegateCommand<object>(o =>
    //        {
    //            executedCommand = true;
    //            Assert.NotNull(o);
    //            Assert.Equal("Value", o);
    //        })
    //    };
    //    var collectionView = new CollectionView();
    //    collectionView.Behaviors.Add(behavior);
    //    behavior.RaiseEvent(collectionView, new TestNestedArgs());
    //    Assert.True(executedCommand);
    //}


    [Fact]
    public void Command_EventArgsParameterPath_Nested_When_ChildIsNull()
    {
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            EventArgsParameterPath = "Item.AProperty",
            Command = new DelegateCommand<object>(o =>
            {
                executedCommand = true;
                Assert.Null(o);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, new TestNestedArgs());
        Assert.True(executedCommand);
    }

    [Fact]
    public void Command_CanExecute()
    {
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            Command = new DelegateCommand(() => Assert.True(false), () => false)
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, null);
    }

    [Fact]
    public void Command_CanExecuteWithParameterShouldExecute()
    {
        var shouldExeute = bool.TrueString;
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            CommandParameter = shouldExeute,
            Command = new DelegateCommand<string>(o =>
            {
                executedCommand = true;
                Assert.True(true);
            }, o => o.Equals(bool.TrueString))
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, null);
        Assert.True(executedCommand);
    }

    [Fact]
    public void Command_CanExecuteWithParameterShouldNotExeute()
    {
        var shouldExeute = bool.FalseString;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            CommandParameter = shouldExeute,
            Command = new DelegateCommand<string>(o => Assert.True(false), o => o.Equals(bool.TrueString))
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, null);
    }

    [Fact]
    public void Command_Execute()
    {
        var executedCommand = false;
        var behavior = new EventToCommandBehaviorMock
        {
            EventName = "SelectionChanged",
            Command = new DelegateCommand(() =>
            {
                executedCommand = true;
                Assert.True(true);
            })
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
        behavior.RaiseEvent(collectionView, null);
        Assert.True(executedCommand);
    }

    [Fact]
    public void EventName_InvalidEventShouldThrow()
    {
        var behavior = new EventToCommandBehavior
        {
            EventName = "OnSelectionChanged"
        };
        var collectionView = new CollectionView();
        Assert.Throws<ArgumentException>(() => collectionView.Behaviors.Add(behavior));
    }

    [Fact]
    public void EventName_Valid()
    {
        var behavior = new EventToCommandBehavior
        {
            EventName = "SelectionChanged"
        };
        var collectionView = new CollectionView();
        collectionView.Behaviors.Add(behavior);
    }
}
