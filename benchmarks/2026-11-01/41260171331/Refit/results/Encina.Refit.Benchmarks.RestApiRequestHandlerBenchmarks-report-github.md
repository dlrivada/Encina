```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean         | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |           NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.07 μs |     0.061 μs |     0.087 μs | 0.002 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     27.59 μs |     0.128 μs |     0.180 μs | 0.003 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    |  8,172.53 μs |   948.721 μs | 1,420.001 μs | 1.027 |    0.24 |    3 |      - |   9.41 KB |        1.00 |
| DirectRefitCall_Batch10     | 14,057.32 μs | 1,063.173 μs | 1,558.386 μs | 1.767 |    0.34 |    4 |      - |  93.38 KB |        9.92 |
| DirectRefitCall_Sequential5 | 40,277.99 μs | 3,696.264 μs | 5,532.395 μs | 5.063 |    1.06 |    5 |      - |   45.9 KB |        4.88 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
