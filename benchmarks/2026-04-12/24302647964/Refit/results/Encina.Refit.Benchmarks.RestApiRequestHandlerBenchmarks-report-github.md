```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Median        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.008 μs |     0.0877 μs |     0.1229 μs |      3.921 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     13.409 μs |     1.4486 μs |     2.1682 μs |     13.322 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  6,309.945 μs |   631.8993 μs |   906.2512 μs |  5,947.896 μs | 1.018 |    0.20 |    3 |      - |    9.9 KB |        1.00 |
| DirectRefitCall_Batch10     |  8,513.614 μs |   427.6191 μs |   613.2786 μs |  8,335.141 μs | 1.374 |    0.20 |    4 |      - |  97.66 KB |        9.87 |
| DirectRefitCall_Sequential5 | 30,594.928 μs | 1,926.4199 μs | 2,636.9055 μs | 29,674.802 μs | 4.938 |    0.76 |    5 |      - |  47.64 KB |        4.81 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
