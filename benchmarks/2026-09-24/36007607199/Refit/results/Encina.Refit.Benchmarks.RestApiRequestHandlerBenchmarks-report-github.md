```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error             | StdDev          | Median         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|------------------:|----------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |                NA |              NA |             NA |      ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       8.599 μs |         0.5363 μs |       0.0294 μs |       8.605 μs |  0.000 |    0.00 |    1 | 0.2899 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |      23.022 μs |        14.1989 μs |       0.7783 μs |      23.387 μs |  0.001 |    0.00 |    2 | 0.6104 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    |  22,750.983 μs |    11,290.9041 μs |     618.8926 μs |  22,840.009 μs |  1.000 |    0.03 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Sequential5 | 119,315.629 μs |    72,635.3887 μs |   3,981.3908 μs | 117,412.702 μs |  5.247 |    0.20 |    4 |      - |  48.09 KB |        4.86 |
| DirectRefitCall_Batch10     | 456,956.352 μs | 5,285,866.4191 μs | 289,736.1760 μs | 293,632.841 μs | 20.095 |   11.05 |    5 |      - |  98.36 KB |        9.95 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
