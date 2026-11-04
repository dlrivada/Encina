```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.07 μs |       1.981 μs |      0.109 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.85 |
| EncinaRefitCall_Batch10     |      29.18 μs |      53.891 μs |      2.954 μs | 0.000 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.77 |
| DirectRefitCall_Batch10     | 174,744.13 μs |  16,932.130 μs |    928.107 μs | 0.931 |    0.05 |    3 |      - |  94.29 KB |        9.12 |
| DirectRefitCall_Baseline    | 188,261.65 μs | 230,550.862 μs | 12,637.271 μs | 1.003 |    0.08 |    3 |      - |  10.34 KB |        1.00 |
| DirectRefitCall_Sequential5 | 873,362.79 μs | 102,104.227 μs |  5,596.677 μs | 4.653 |    0.26 |    4 |      - |  48.28 KB |        4.67 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
