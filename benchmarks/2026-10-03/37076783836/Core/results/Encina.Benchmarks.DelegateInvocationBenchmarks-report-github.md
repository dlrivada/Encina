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
| CompiledDelegate        |     36.86 ns |     3.797 ns |   0.208 ns |     1.21 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.41 ns |    10.907 ns |   0.598 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,078.30 ns | 4,154.124 ns | 227.702 ns | 3,061.70 |   53.12 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.61 ns |    15.879 ns |   0.870 ns |     4.49 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     77.83 ns |    20.057 ns |   1.099 ns |     2.56 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
