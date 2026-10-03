```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|-------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |           NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.52 μs |     1.163 μs |   0.064 μs | 0.000 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |      24.97 μs |     4.348 μs |   0.238 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    |  21,265.59 μs | 3,050.196 μs | 167.192 μs | 1.000 |    0.01 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  27,037.48 μs | 2,518.012 μs | 138.021 μs | 1.271 |    0.01 |    4 |      - |  93.35 KB |        9.86 |
| DirectRefitCall_Sequential5 | 112,044.09 μs | 1,428.534 μs |  78.303 μs | 5.269 |    0.04 |    5 |      - |   49.2 KB |        5.20 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
