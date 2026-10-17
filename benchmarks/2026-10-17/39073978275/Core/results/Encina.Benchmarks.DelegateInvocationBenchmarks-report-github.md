```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.67 ns |    10.892 ns |   0.597 ns |     1.28 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.41 ns |     4.477 ns |   0.245 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,222.33 ns | 3,760.576 ns | 206.130 ns | 3,034.24 |   22.81 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.14 ns |    22.700 ns |   1.244 ns |     4.60 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.32 ns |    18.782 ns |   1.030 ns |     2.80 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
