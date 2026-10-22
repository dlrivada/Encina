```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean           | Error         | StdDev        | Median         | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|---------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.815 μs |     0.0412 μs |     0.0564 μs |       7.819 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |      20.542 μs |     1.5049 μs |     2.2525 μs |      21.961 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    |  19,236.778 μs | 1,485.4730 μs | 2,177.3872 μs |  18,958.816 μs | 1.012 |    0.16 |    3 |      - |   9.57 KB |        1.00 |
| DirectRefitCall_Batch10     |  32,163.908 μs | 3,000.9444 μs | 4,491.6727 μs |  30,724.895 μs | 1.693 |    0.30 |    4 |      - |  93.78 KB |        9.80 |
| DirectRefitCall_Sequential5 | 107,085.804 μs | 5,819.7939 μs | 8,530.5791 μs | 105,865.901 μs | 5.635 |    0.76 |    5 |      - |   46.2 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
