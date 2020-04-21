﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using Zafiro.Core;

namespace Zafiro.Wpf.Services.MarkupWindow
{
    public class DialogViewModel : ReactiveObject, IDisposable
    {
        private Option selectedOption;
        private readonly IDisposable closer;

        public DialogViewModel(object content, IEnumerable<Option> options, ICloseable closeable) : this("", content, options, closeable)
        {
        }

        public DialogViewModel(string title, object content, IEnumerable<Option> options, ICloseable closeable)
        {
            Title = title;
            Content = content;
            Options = options.Select(x =>
            {
                return new OptionViewModel(x)
                {
                    Command = ReactiveCommand.Create<OptionViewModel>(o => SelectedOption = x)
                };
            }).ToList();

            closer = this.WhenAnyValue(model => model.SelectedOption)
                .Where(s => s != null)
                .Subscribe(_ => closeable.Close());
        }

        public string Title { get; }
        public object Content { get; set; }

        public Option SelectedOption
        {
            get => selectedOption;
            set => this.RaiseAndSetIfChanged(ref selectedOption, value);
        }

        public List<OptionViewModel> Options { get; set; }

        public void Dispose()
        {
            closer?.Dispose();
        }
    }
}