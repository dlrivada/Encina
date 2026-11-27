```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.53 ns |     2.130 ns |  0.117 ns |     1.27 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.79 ns |     0.889 ns |  0.049 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,586.98 ns | 1,346.891 ns | 73.828 ns | 3,146.90 |    5.13 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.62 ns |    41.089 ns |  2.252 ns |     4.64 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.92 ns |    22.878 ns |  1.254 ns |     2.85 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
