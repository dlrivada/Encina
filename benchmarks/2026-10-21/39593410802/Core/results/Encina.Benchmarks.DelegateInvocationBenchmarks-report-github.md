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
| CompiledDelegate        |     36.67 ns |     4.016 ns |   0.220 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.97 ns |     2.464 ns |   0.135 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,243.39 ns | 2,532.756 ns | 138.829 ns | 3,115.54 |   13.27 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.07 ns |    21.638 ns |   1.186 ns |     4.46 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     78.99 ns |     8.007 ns |   0.439 ns |     2.73 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
