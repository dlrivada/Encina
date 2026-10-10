```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.26 ns |     4.002 ns |  0.219 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.98 ns |     2.286 ns |  0.125 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,895.74 ns | 1,679.372 ns | 92.052 ns | 3,205.74 |   12.34 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.58 ns |     4.411 ns |  0.242 ns |     4.64 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     75.72 ns |     5.344 ns |  0.293 ns |     2.61 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
