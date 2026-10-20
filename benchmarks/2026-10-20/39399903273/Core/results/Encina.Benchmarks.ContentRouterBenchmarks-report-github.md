```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,360.1 ns |    19.18 ns | 11.41 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,027.8 ns |    13.28 ns |  8.78 ns |  0.76 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,035.6 ns |    20.67 ns | 12.30 ns |  0.76 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,488.5 ns |    92.36 ns | 61.09 ns |  1.09 |    0.04 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,430.2 ns |    23.11 ns | 12.09 ns |  1.05 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   223.6 ns |     2.44 ns |  1.28 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   395.1 ns |    17.08 ns | 11.30 ns |  0.29 |    0.01 | 0.0787 |    1320 B |        1.34 |
|                              |            |                |             |            |             |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,353.8 ns |   335.96 ns | 18.42 ns |  1.00 |    0.02 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,042.5 ns | 1,237.27 ns | 67.82 ns |  0.77 |    0.04 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,039.9 ns |   700.75 ns | 38.41 ns |  0.77 |    0.03 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,407.6 ns |   267.09 ns | 14.64 ns |  1.04 |    0.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,428.0 ns |   144.28 ns |  7.91 ns |  1.05 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   216.6 ns |    22.92 ns |  1.26 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   378.2 ns |    46.83 ns |  2.57 ns |  0.28 |    0.00 | 0.0787 |    1320 B |        1.34 |
