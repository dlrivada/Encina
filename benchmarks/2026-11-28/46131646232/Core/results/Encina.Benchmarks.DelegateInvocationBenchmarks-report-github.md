```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     46.20 ns |     1.958 ns |  0.107 ns |   1.08 |    0.00 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     42.72 ns |     1.833 ns |  0.100 ns |   1.00 |    0.00 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 33,632.23 ns | 1,604.471 ns | 87.946 ns | 787.21 |    2.40 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    135.14 ns |    12.460 ns |  0.683 ns |   3.16 |    0.02 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     96.28 ns |     4.108 ns |  0.225 ns |   2.25 |    0.01 | 0.0020 |     176 B |        1.57 |
