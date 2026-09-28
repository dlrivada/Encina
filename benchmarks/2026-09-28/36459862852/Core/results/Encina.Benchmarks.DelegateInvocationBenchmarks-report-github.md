```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.50 ns |      1.623 ns |     0.089 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.45 ns |      5.263 ns |     0.288 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,277.62 ns | 18,961.093 ns | 1,039.321 ns | 3,167.74 |   40.63 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.90 ns |     28.986 ns |     1.589 ns |     4.68 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     78.16 ns |      4.765 ns |     0.261 ns |     2.65 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
