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
| CompiledDelegate        |     45.36 ns |     0.807 ns |  0.044 ns |   1.34 |    0.01 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     33.77 ns |     2.770 ns |  0.152 ns |   1.00 |    0.01 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 31,794.38 ns | 1,328.537 ns | 72.822 ns | 941.39 |    4.11 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    136.57 ns |     6.462 ns |  0.354 ns |   4.04 |    0.02 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     98.54 ns |     1.354 ns |  0.074 ns |   2.92 |    0.01 | 0.0020 |     176 B |        1.57 |
