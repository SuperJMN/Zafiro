# ByteSource Usage Guide

## Overview

`ByteSource` provides a functional, composable API for working with byte streams. It's built on top of `IObservable<byte[]>` and integrates with `CSharpFunctionalExtensions.Result` for total error handling.

## Core Principle

**All high-level read operations return `Result<T>` instead of throwing exceptions.**

This means:
- I/O errors (network failures, file read errors, etc.) are captured as `Result.Failure`
- No `try/catch` needed in consuming code
- Easy composition with functional operators like `Bind`, `Map`, `Successes`, `Failures`

---

## Reading Data

### Read entire source into memory

```csharp
// Load all bytes
var bytesResult = await byteSource.ReadAll(cancellationToken);
if (bytesResult.IsFailure)
{
    // Handle error without exceptions
    logger.Error(bytesResult.Error);
    return;
}
var data = bytesResult.Value;

// Load as text (UTF-8 by default)
var textResult = await byteSource.ReadAllText(cancellationToken);
textResult
    .Tap(text => Console.WriteLine(text))
    .TapError(error => notificationService.Notify(error));

// With custom encoding
var textResult = await byteSource.ReadAllText(Encoding.ASCII, cancellationToken);
```

### Streaming with functional error handling

When you need to process chunks as they arrive without loading everything into memory:

```csharp
// Process successes, log failures
byteSource.ToResultSequence()
    .Successes()
    .Subscribe(chunk => ProcessChunk(chunk));

byteSource.ToResultSequence()
    .Failures()
    .Subscribe(error => logger.Error(error));

// Or both in one subscription
byteSource.ToResultSequence()
    .Subscribe(result =>
    {
        if (result.IsSuccess)
            ProcessChunk(result.Value);
        else
            HandleError(result.Error);
    });
```

**Key difference from raw `IObservable<byte[]>`:**
- `ToResultSequence()` **never** calls `OnError` on the subscriber
- Errors are surfaced as `Result.Failure<byte[]>` items in the stream
- The sequence completes normally even if an error occurred

---

## Writing Data

All write operations already return `Result`:

```csharp
// Write to stream (final result)
var result = await byteSource.WriteTo(outputStream, cancellationToken);

// Write to file path
var result = await byteSource.WriteTo("/path/to/file.bin", cancellationToken);

// Chunked write with per-chunk progress
byteSource.WriteToChunked(outputStream, cancellationToken)
    .Successes()
    .Subscribe(_ => progressReporter.Increment());

byteSource.WriteToChunked(outputStream, cancellationToken)
    .Failures()
    .Subscribe(error => logger.Error(error));
```

### Safe vs. zero-copy writes

```csharp
// Default: zero-copy (fast, but requires producer to not reuse buffers)
await byteSource.WriteTo(stream);

// Safe: defensive copy per chunk (use if producer reuses buffers)
await byteSource.WriteToSafe(stream);
```

---

## Creating ByteSources

### From memory

```csharp
var source = ByteSource.FromBytes(byteArray, bufferSize: 1024 * 1024);
var source = ByteSource.FromString("Hello, World!", Encoding.UTF8);
```

### From streams

```csharp
// From existing stream
var source = ByteSource.FromStream(stream, leaveOpen: false);

// From stream factory (recommended for resource management)
var source = ByteSource.FromStreamFactory(() => File.OpenRead(path));

// From async stream factory
var source = ByteSource.FromAsyncStreamFactory(async () =>
{
    var response = await httpClient.GetAsync(url);
    return await response.Content.ReadAsStreamAsync();
});
```

### From URIs

```csharp
// With provided HttpClient (recommended)
var result = await ByteSourceUriFactoryMethods.FromUri(uri, httpClient, ct);
if (result.IsSuccess)
{
    var source = result.Value;
    // ...
}

// Or as extension
var result = await uri.ToByteSource(httpClient, ct);
```

---

## Functional Composition Examples

### Download and save with full error handling

```csharp
var result = await ByteSourceUriFactoryMethods.FromUri(url, httpClient, ct)
    .Bind(source => source.WriteTo(outputPath, ct));

result
    .Tap(() => logger.Information("Download complete"))
    .TapError(error => notificationService.ShowError(error));
```

### Process multiple files

```csharp
var results = await filePaths
    .Select(path => ByteSource.FromStreamFactory(() => File.OpenRead(path)))
    .Select(source => source.ReadAllText())
    .CombineSequentially();

// All successes or first failure
var combined = results.Combine();
```

### Stream processing with progress

```csharp
var progress = new Progress<int>();
var chunks = 0;

await byteSource.ToResultSequence()
    .Successes()
    .Do(chunk =>
    {
        chunks++;
        progress.Report(chunks);
    })
    .Select(chunk => ProcessChunk(chunk))
    .ToTask(ct);
```

---

## Migration Guide

### Old pattern (exceptions)

```csharp
try
{
    var data = await byteSource.Bytes
        .SelectMany(chunk => chunk)
        .ToArray()
        .ToTask();
    // Process data
}
catch (Exception ex)
{
    // Handle error
}
```

### New pattern (Result)

```csharp
var result = await byteSource.ReadAll();
result
    .Tap(data => /* Process data */)
    .TapError(error => /* Handle error */);
```

---

## Advanced: Low-Level Observable Access

If you need direct access to the underlying observable (for custom Rx pipelines), you can still use:

```csharp
byteSource.Bytes.Subscribe(onNext, onError, onCompleted);
```

**But be aware:**
- Errors will come through `onError` callback
- You're responsible for handling exceptions
- For most use cases, prefer `ReadAll`, `ReadAllText`, or `ToResultSequence`

---

## Best Practices

1. **Use `ReadAll` / `ReadAllText` for simple "load everything" scenarios**
2. **Use `ToResultSequence` for streaming with error handling**
3. **Use `WriteTo*` methods instead of manual stream writing**
4. **Compose with `Result` combinators (`Bind`, `Map`, `Tap`) for clean pipelines**
5. **Handle errors functionally with `Successes` / `Failures` operators**
6. **Only use raw `.Bytes` subscription for advanced Rx scenarios**
