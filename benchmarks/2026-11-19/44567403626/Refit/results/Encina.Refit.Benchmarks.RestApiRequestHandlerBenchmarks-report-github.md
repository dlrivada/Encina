```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.32 μs |       1.176 μs |      0.064 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.91 |
| EncinaRefitCall_Batch10     |      24.52 μs |      19.137 μs |      1.049 μs | 0.000 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.91 |
| DirectRefitCall_Baseline    |  51,486.35 μs |   6,442.041 μs |    353.110 μs | 1.000 |    0.01 |    3 |      - |   9.66 KB |        1.00 |
| DirectRefitCall_Batch10     |  60,395.82 μs |  23,734.395 μs |  1,300.962 μs | 1.173 |    0.02 |    3 |      - |  93.84 KB |        9.72 |
| DirectRefitCall_Sequential5 | 282,822.83 μs | 402,294.869 μs | 22,051.139 μs | 5.493 |    0.37 |    4 |      - |  46.83 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
