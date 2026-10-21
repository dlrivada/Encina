```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.80 ns |    11.394 ns |   0.625 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.88 ns |     1.089 ns |   0.060 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,183.61 ns | 4,750.602 ns | 260.397 ns | 3,084.94 |    9.24 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.92 ns |    11.232 ns |   0.616 ns |     4.38 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.56 ns |     6.900 ns |   0.378 ns |     2.73 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
