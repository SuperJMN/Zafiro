using System;
using CSharpFunctionalExtensions;
using Zafiro.ProgressReporting;

namespace Zafiro.Works;

public interface IWork
{
    IObservable<Progress> Progress { get; }
    IObservable<Result> Execute();
}