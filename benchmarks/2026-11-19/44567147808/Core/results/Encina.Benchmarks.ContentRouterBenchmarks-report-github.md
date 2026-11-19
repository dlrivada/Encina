```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,774.3 ns |  24.34 ns | 16.10 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,385.9 ns |   5.39 ns |  3.56 ns |  0.78 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,378.5 ns |  20.10 ns | 13.29 ns |  0.78 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,852.0 ns |  14.12 ns |  9.34 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,877.3 ns |  18.73 ns | 12.39 ns |  1.06 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   304.6 ns |   4.78 ns |  3.16 ns |  0.17 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   514.5 ns |  12.18 ns |  8.05 ns |  0.29 |    0.01 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,798.6 ns | 812.58 ns | 44.54 ns |  1.00 |    0.03 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,387.6 ns | 563.69 ns | 30.90 ns |  0.77 |    0.02 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,365.8 ns |  39.81 ns |  2.18 ns |  0.76 |    0.02 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,811.6 ns |  67.96 ns |  3.72 ns |  1.01 |    0.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,872.6 ns | 325.25 ns | 17.83 ns |  1.04 |    0.02 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   290.9 ns |  34.48 ns |  1.89 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   510.9 ns | 197.39 ns | 10.82 ns |  0.28 |    0.01 | 0.0782 |    1320 B |        1.34 |
