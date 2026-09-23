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
| CompiledDelegate        |     36.06 ns |     5.334 ns |  0.292 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.59 ns |     2.005 ns |  0.110 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,261.00 ns | 1,566.026 ns | 85.839 ns | 3,157.40 |   10.85 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.43 ns |    11.582 ns |  0.635 ns |     4.70 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.26 ns |    15.025 ns |  0.824 ns |     2.67 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
