```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     11.97 μs |      0.247 μs |     0.014 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     28.91 μs |     16.043 μs |     0.879 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,702.38 μs |  7,282.228 μs |   399.164 μs | 1.000 |    0.03 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 20,591.50 μs | 46,988.283 μs | 2,575.586 μs | 1.401 |    0.16 |    4 |      - |  93.43 KB |        9.91 |
| DirectRefitCall_Sequential5 | 72,790.79 μs |  6,368.179 μs |   349.061 μs | 4.953 |    0.12 |    5 |      - |  45.77 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
