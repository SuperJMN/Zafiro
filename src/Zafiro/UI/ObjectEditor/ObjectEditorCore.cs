using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reflection;
using ReactiveUI;
using Zafiro.Mixins;
using Zafiro.UI.ObjectEditor.TemplateMatchers;

namespace Zafiro.UI.ObjectEditor;

public class ObjectEditorCore<TEditor, TTemplate> : ReactiveObject where TTemplate : class
{
    private readonly ITemplateMatcher<TTemplate> editorTemplateMatcher;
    private readonly IObjectEditor<TEditor, TTemplate> objectEditor;
    private readonly Func<TTemplate, PropertyInfo, IList<object>, PropertyItem<TEditor>> propertyItemFactory;

    private CompositeDisposable disposables = new();
    private IList<PropertyItem<TEditor>> propertyItems;

    public ObjectEditorCore(IObjectEditor<TEditor, TTemplate> objectEditor,
        Func<TTemplate, PropertyInfo, IList<object>, PropertyItem<TEditor>> propertyItemFactory,
        Func<TTemplate> getTemplate)
    {
        this.objectEditor = objectEditor;
        this.propertyItemFactory = propertyItemFactory;
        editorTemplateMatcher = new NameAndTypeTemplateMatcher<TTemplate>()
            .Chain(new TypeTemplateMatcher<TTemplate>()
                .Chain(new EnumTemplateMatcher<TTemplate>()
                    .Chain(new ReturnTemplateMatcher<TTemplate>(getTemplate))));
    }

    public IList<PropertyItem<TEditor>> PropertyItems
    {
        get => propertyItems;
        set
        {
            DisposeAll(propertyItems);
            this.RaiseAndSetIfChanged(ref propertyItems, value);
        }
    }

    public void OnSelectedItemsChanged(object newValue)
    {
        disposables.Dispose();
        disposables = new CompositeDisposable();

        IList<object> targets;
        if (newValue is IList<object> list)
        {
            targets = list;
            if (list is INotifyCollectionChanged col)
            {
                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                        h => col.CollectionChanged += h, h => col.CollectionChanged -= h)
                    .Subscribe(x => OnCollectionChanged())
                    .DisposeWith(disposables);
            }
        }
        else
        {
            targets = new List<object> { newValue };
        }

        PropertyItems = UpdatePropertyItems(targets);

        foreach (var propertyItem in PropertyItems) disposables.Add(propertyItem);
    }

    private List<PropertyItem<TEditor>> UpdatePropertyItems(IList<object> targets)
    {
        if (targets == null || targets.Any(o => o == null))
        {
            return new List<PropertyItem<TEditor>>();
        }

        var allProperties = targets
            .Select(o =>
                o.GetType().GetRuntimeProperties()
                    .Where(p => p.GetMethod != null && p.SetMethod != null)
                    .Where(p => !(p.GetMethod.IsStatic || p.SetMethod.IsStatic))
                    .Where(p => p.SetMethod.IsPublic && p.GetMethod.IsPublic)
                    .Where(x => x.GetCustomAttribute<HiddenAttribute>() == null))
            .ToList();

        if (allProperties.Count == 0)
        {
            return new List<PropertyItem<TEditor>>();
        }

        var equalityComparer =
            new LambdaComparer<PropertyInfo>((a, b) => a.PropertyType == b.PropertyType && a.Name == b.Name);

        var commonProperties = allProperties.Count == 1
            ? allProperties.First()
            : allProperties.GetCommon(equalityComparer);

        return commonProperties
            .Select(property => new { Editor = SelectPropertyEditor(property, objectEditor.EditorsCore), Property = property })
            .Where(tuple => tuple.Editor != null)
            .Select(tuple => propertyItemFactory(tuple.Editor, tuple.Property, targets))
            .OrderBy(item => item.PropertyName)
            .ToList();
    }

    private TTemplate SelectPropertyEditor(PropertyInfo propertyInfo, EditorCollection<TTemplate> editorEditors)
    {
        var selectPropertyEditor = editorTemplateMatcher.Select(editorEditors, propertyInfo);
        return selectPropertyEditor;
    }

    private void OnCollectionChanged()
    {
        UpdatePropertyItems((IList<object>)objectEditor.SelectedItems);
    }

    private static void DisposeAll(IList<PropertyItem<TEditor>> disposables)
    {
        if (disposables == null)
        {
            return;
        }

        foreach (var i in disposables) i.Dispose();
    }
}