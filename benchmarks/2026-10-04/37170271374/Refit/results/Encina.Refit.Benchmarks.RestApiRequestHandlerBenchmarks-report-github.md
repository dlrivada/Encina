```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Median        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      6.722 μs |     0.0620 μs |     0.0889 μs |      6.722 μs | 0.000 |    0.00 |    1 | 0.4349 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |     18.482 μs |     1.0647 μs |     1.5936 μs |     18.945 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.58 |
| DirectRefitCall_Baseline    | 15,538.723 μs | 1,369.5485 μs | 2,007.4666 μs | 16,674.714 μs | 1.016 |    0.18 |    3 |      - |   9.54 KB |        1.00 |
| DirectRefitCall_Batch10     | 19,973.961 μs |   488.0034 μs |   699.8799 μs | 20,067.696 μs | 1.306 |    0.17 |    4 |      - |  93.56 KB |        9.80 |
| DirectRefitCall_Sequential5 | 80,699.829 μs | 1,958.8930 μs | 2,809.3860 μs | 81,060.385 μs | 5.278 |    0.69 |    5 |      - |  46.11 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
