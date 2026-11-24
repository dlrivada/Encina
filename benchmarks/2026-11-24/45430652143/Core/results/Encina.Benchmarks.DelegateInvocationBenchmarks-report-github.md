```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.10 ns |     4.315 ns |   0.237 ns |     1.15 |    0.02 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     24.46 ns |     7.528 ns |   0.413 ns |     1.00 |    0.02 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 24,692.25 ns | 2,478.096 ns | 135.833 ns | 1,009.80 |   15.50 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     96.76 ns |     6.992 ns |   0.383 ns |     3.96 |    0.06 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     67.85 ns |    12.519 ns |   0.686 ns |     2.77 |    0.05 | 0.0020 |      - |     176 B |        1.57 |
