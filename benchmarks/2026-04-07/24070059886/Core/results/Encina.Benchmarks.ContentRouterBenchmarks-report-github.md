```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,972.1 ns |  8.23 ns | 5.44 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,397.4 ns |  4.51 ns | 2.68 ns |  0.71 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,425.7 ns |  6.21 ns | 4.11 ns |  0.72 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,073.1 ns |  7.11 ns | 4.23 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,061.0 ns | 10.58 ns | 6.29 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        373.4 ns |  1.81 ns | 1.20 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        629.5 ns |  3.22 ns | 1.92 ns |  0.32 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| SimpleRoute_SingleCondition  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,046,857.0 ns |       NA | 0.00 ns |  1.00 |      - |    1192 B |        1.00 |
| ComplexRoute_FirstMatch      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,654,226.0 ns |       NA | 0.00 ns |  0.93 |      - |     792 B |        0.66 |
| ComplexRoute_DefaultFallback | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,679,763.0 ns |       NA | 0.00 ns |  0.93 |      - |     792 B |        0.66 |
| ManyRoutes_FirstMatch        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,713,172.0 ns |       NA | 0.00 ns |  1.02 |      - |    1232 B |        1.03 |
| ManyRoutes_LateMatch         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,933,484.0 ns |       NA | 0.00 ns |  1.03 |      - |    1232 B |        1.03 |
| BuildDefinition_Simple       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    576,978.0 ns |       NA | 0.00 ns |  0.02 |      - |     584 B |        0.49 |
| BuildDefinition_Complex      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,056,103.0 ns |       NA | 0.00 ns |  0.03 |      - |    1320 B |        1.11 |
