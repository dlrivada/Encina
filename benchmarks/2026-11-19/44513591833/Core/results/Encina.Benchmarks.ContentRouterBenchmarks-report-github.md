```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,398.3 ns |    44.94 ns | 29.72 ns |  1.00 |    0.03 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,026.8 ns |    32.86 ns | 21.74 ns |  0.73 |    0.02 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     |   994.3 ns |    13.30 ns |  8.79 ns |  0.71 |    0.02 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,447.3 ns |    41.41 ns | 21.66 ns |  1.04 |    0.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,416.2 ns |    40.23 ns | 26.61 ns |  1.01 |    0.03 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   207.2 ns |     8.43 ns |  5.58 ns |  0.15 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   359.7 ns |    10.45 ns |  5.47 ns |  0.26 |    0.01 | 0.0787 |    1320 B |        1.34 |
|                              |            |                |             |            |             |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,389.4 ns |   204.46 ns | 11.21 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,030.8 ns | 1,101.52 ns | 60.38 ns |  0.74 |    0.04 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,041.6 ns |   132.23 ns |  7.25 ns |  0.75 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,430.5 ns |   284.16 ns | 15.58 ns |  1.03 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,458.2 ns |   413.23 ns | 22.65 ns |  1.05 |    0.02 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   206.0 ns |    49.13 ns |  2.69 ns |  0.15 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   378.4 ns |    65.29 ns |  3.58 ns |  0.27 |    0.00 | 0.0787 |    1320 B |        1.34 |
