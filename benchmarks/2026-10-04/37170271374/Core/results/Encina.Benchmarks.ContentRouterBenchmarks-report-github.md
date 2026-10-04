```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 1,972.2 ns |  7.63 ns |  4.54 ns | 1,972.4 ns |  1.00 |    0.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,471.6 ns |  5.55 ns |  3.67 ns | 1,471.0 ns |  0.75 |    0.00 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,471.0 ns |  3.86 ns |  2.30 ns | 1,470.6 ns |  0.75 |    0.00 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 2,049.4 ns |  6.94 ns |  4.13 ns | 2,048.9 ns |  1.04 |    0.00 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 2,116.9 ns |  8.18 ns |  5.41 ns | 2,116.8 ns |  1.07 |    0.00 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   374.1 ns |  1.45 ns |  0.86 ns |   374.4 ns |  0.19 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   614.4 ns |  3.38 ns |  2.24 ns |   614.3 ns |  0.31 |    0.00 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |            |       |         |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 2,025.2 ns | 19.35 ns | 28.36 ns | 2,045.0 ns |  1.00 |    0.02 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,480.1 ns |  3.41 ns |  5.10 ns | 1,479.9 ns |  0.73 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,475.3 ns |  3.17 ns |  4.74 ns | 1,474.0 ns |  0.73 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 2,112.6 ns | 10.40 ns | 15.25 ns | 2,112.1 ns |  1.04 |    0.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 2,067.4 ns |  5.88 ns |  8.79 ns | 2,065.9 ns |  1.02 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   380.7 ns |  1.39 ns |  2.04 ns |   381.0 ns |  0.19 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   625.8 ns |  6.72 ns |  9.42 ns |   629.6 ns |  0.31 |    0.01 | 0.0782 |    1320 B |        1.34 |
