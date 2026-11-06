```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,738.7 ns |  13.22 ns |  8.75 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,376.2 ns |   3.87 ns |  2.56 ns |  0.79 |    0.00 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,369.3 ns |   2.14 ns |  1.12 ns |  0.79 |    0.00 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,827.9 ns |  11.17 ns |  7.39 ns |  1.05 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,850.2 ns |  11.60 ns |  7.67 ns |  1.06 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   297.2 ns |   2.64 ns |  1.75 ns |  0.17 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   492.0 ns |   4.42 ns |  2.92 ns |  0.28 |    0.00 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,745.3 ns | 496.28 ns | 27.20 ns |  1.00 |    0.02 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,374.4 ns |  61.50 ns |  3.37 ns |  0.79 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,369.3 ns |  35.30 ns |  1.93 ns |  0.78 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,806.6 ns |  39.90 ns |  2.19 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,810.2 ns |  66.68 ns |  3.66 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   286.2 ns |  26.34 ns |  1.44 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   488.3 ns |  57.55 ns |  3.15 ns |  0.28 |    0.00 | 0.0782 |    1320 B |        1.34 |
