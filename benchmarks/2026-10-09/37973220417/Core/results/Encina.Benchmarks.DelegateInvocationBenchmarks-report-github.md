```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     46.52 ns |     32.148 ns |   1.762 ns |   1.33 |    0.05 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     35.00 ns |      7.504 ns |   0.411 ns |   1.00 |    0.01 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 33,207.46 ns | 12,283.907 ns | 673.322 ns | 948.99 |   19.24 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    131.08 ns |     13.286 ns |   0.728 ns |   3.75 |    0.04 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |    105.97 ns |     37.382 ns |   2.049 ns |   3.03 |    0.06 | 0.0020 |     176 B |        1.57 |
