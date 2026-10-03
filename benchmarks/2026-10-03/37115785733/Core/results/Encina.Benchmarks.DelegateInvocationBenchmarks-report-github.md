```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.19 ns |     0.474 ns |  0.026 ns |     1.26 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.57 ns |     0.273 ns |  0.015 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 51,595.19 ns | 1,716.516 ns | 94.088 ns | 2,392.28 |    4.04 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |     98.44 ns |     8.375 ns |  0.459 ns |     4.56 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.12 ns |     4.057 ns |  0.222 ns |     3.02 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
