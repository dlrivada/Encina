```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     44.08 ns |     2.687 ns |   0.147 ns |   1.33 |    0.04 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     33.19 ns |    19.497 ns |   1.069 ns |   1.00 |    0.04 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 30,567.23 ns | 2,315.401 ns | 126.915 ns | 921.59 |   25.49 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    130.40 ns |     0.843 ns |   0.046 ns |   3.93 |    0.11 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     91.91 ns |     1.213 ns |   0.066 ns |   2.77 |    0.08 | 0.0020 |     176 B |        1.57 |
