```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.221 μs |      3.005 μs |     0.1647 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     20.669 μs |      9.976 μs |     0.5468 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    | 14,625.667 μs |  5,026.027 μs |   275.4935 μs | 1.000 |    0.02 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     | 21,674.317 μs | 21,117.521 μs | 1,157.5226 μs | 1.482 |    0.07 |    4 |      - |  93.52 KB |        9.86 |
| DirectRefitCall_Sequential5 | 79,651.837 μs | 25,405.169 μs | 1,392.5430 μs | 5.447 |    0.12 |    5 |      - |  45.78 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
