```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     42.71 ns |   1.990 ns |  0.109 ns |   1.29 |    0.01 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     33.06 ns |   3.529 ns |  0.193 ns |   1.00 |    0.01 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 32,303.57 ns | 791.837 ns | 43.403 ns | 977.25 |    5.08 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    133.28 ns |  10.591 ns |  0.581 ns |   4.03 |    0.03 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     96.77 ns |   4.622 ns |  0.253 ns |   2.93 |    0.02 | 0.0020 |     176 B |        1.57 |
