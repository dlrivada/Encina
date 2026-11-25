```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     45.30 ns |     6.354 ns |   0.348 ns |     1.61 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.09 ns |     0.364 ns |   0.020 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,455.19 ns | 8,451.829 ns | 463.273 ns | 3,184.74 |   14.42 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    128.03 ns |     5.843 ns |   0.320 ns |     4.56 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.12 ns |     8.568 ns |   0.470 ns |     2.85 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
