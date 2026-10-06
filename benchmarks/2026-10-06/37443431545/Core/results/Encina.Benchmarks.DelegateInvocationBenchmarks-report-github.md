```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.82 ns |    13.514 ns |   0.741 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.74 ns |     5.537 ns |   0.303 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,991.98 ns | 6,784.340 ns | 371.873 ns | 2,898.84 |   26.18 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    143.01 ns |    33.702 ns |   1.847 ns |     4.51 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.04 ns |     5.840 ns |   0.320 ns |     2.55 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
