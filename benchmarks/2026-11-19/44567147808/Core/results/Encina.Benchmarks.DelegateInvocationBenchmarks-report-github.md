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
| CompiledDelegate        |     27.36 ns |     6.208 ns |   0.340 ns |     1.22 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     22.43 ns |     0.639 ns |   0.035 ns |     1.00 |    0.00 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 24,413.37 ns | 1,992.740 ns | 109.229 ns | 1,088.51 |    4.47 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     96.30 ns |    35.536 ns |   1.948 ns |     4.29 |    0.08 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     69.11 ns |    28.907 ns |   1.584 ns |     3.08 |    0.06 | 0.0020 |      - |     176 B |        1.57 |
