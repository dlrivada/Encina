```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     45.65 ns |     4.189 ns |   0.230 ns |   1.37 |    0.01 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     33.34 ns |     1.601 ns |   0.088 ns |   1.00 |    0.00 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 32,318.05 ns | 2,409.457 ns | 132.070 ns | 969.28 |    4.08 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    140.04 ns |     2.170 ns |   0.119 ns |   4.20 |    0.01 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     99.04 ns |     7.427 ns |   0.407 ns |   2.97 |    0.01 | 0.0020 |     176 B |        1.57 |
