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
| CompiledDelegate        |     39.19 ns |     3.738 ns |   0.205 ns |     1.23 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.87 ns |     6.867 ns |   0.376 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 94,430.25 ns | 9,899.913 ns | 542.648 ns | 2,963.68 |   33.89 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.91 ns |     7.007 ns |   0.384 ns |     4.45 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.37 ns |     5.117 ns |   0.280 ns |     2.62 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
