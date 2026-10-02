```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| DirectRefitCall_Batch10     |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       8.952 μs |      2.827 μs |     0.1550 μs | 0.000 |    0.00 |    1 | 0.0763 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |      25.777 μs |     30.426 μs |     1.6677 μs | 0.001 |    0.00 |    2 | 0.1831 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    |  23,370.775 μs |  4,006.845 μs |   219.6287 μs | 1.000 |    0.01 |    3 |      - |   9.49 KB |        1.00 |
| DirectRefitCall_Sequential5 | 126,672.069 μs | 18,666.407 μs | 1,023.1688 μs | 5.420 |    0.06 |    4 |      - |  46.14 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RestApiRequestHandlerBenchmarks.DirectRefitCall_Batch10: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
