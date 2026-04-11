```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       4.043 μs |     0.0513 μs |     0.0752 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |      12.454 μs |     0.8878 μs |     1.2446 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  22,439.237 μs |   349.4919 μs |   501.2308 μs | 1.000 |    0.03 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     |  25,433.155 μs |   319.8411 μs |   437.8021 μs | 1.134 |    0.03 |    4 |      - |  98.18 KB |        9.92 |
| DirectRefitCall_Sequential5 | 118,178.958 μs | 2,983.9408 μs | 4,183.0686 μs | 5.269 |    0.22 |    5 |      - |   48.1 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
