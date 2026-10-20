```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.79 ns |      3.922 ns |   0.215 ns |     1.38 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.12 ns |      2.605 ns |   0.143 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 63,822.43 ns | 14,328.960 ns | 785.419 ns | 2,269.49 |   26.17 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.51 ns |     33.069 ns |   1.813 ns |     4.75 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     94.58 ns |     14.302 ns |   0.784 ns |     3.36 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
