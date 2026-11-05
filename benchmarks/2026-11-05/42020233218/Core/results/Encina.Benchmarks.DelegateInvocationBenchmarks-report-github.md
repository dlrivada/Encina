```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.73 ns |    11.383 ns |  0.624 ns |     1.21 |    0.03 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     22.92 ns |     8.332 ns |  0.457 ns |     1.00 |    0.02 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 24,696.01 ns | 1,317.295 ns | 72.205 ns | 1,077.96 |   18.61 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     95.21 ns |    26.530 ns |  1.454 ns |     4.16 |    0.09 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     62.41 ns |     2.756 ns |  0.151 ns |     2.72 |    0.05 | 0.0020 |      - |     176 B |        1.57 |
