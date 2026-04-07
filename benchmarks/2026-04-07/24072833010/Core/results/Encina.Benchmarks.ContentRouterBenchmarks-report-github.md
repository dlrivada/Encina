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
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,066.7 ns |  7.59 ns | 5.02 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,394.7 ns | 11.35 ns | 6.75 ns |  0.67 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,396.5 ns |  9.97 ns | 5.93 ns |  0.68 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,118.2 ns | 10.58 ns | 7.00 ns |  1.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,059.8 ns |  7.74 ns | 4.61 ns |  1.00 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        391.3 ns |  3.01 ns | 1.79 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        650.0 ns | 11.62 ns | 7.69 ns |  0.31 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| SimpleRoute_SingleCondition  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 35,203,535.0 ns |       NA | 0.00 ns |  1.00 |      - |    1192 B |        1.00 |
| ComplexRoute_FirstMatch      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,013,947.0 ns |       NA | 0.00 ns |  0.91 |      - |     792 B |        0.66 |
| ComplexRoute_DefaultFallback | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,274,806.0 ns |       NA | 0.00 ns |  0.92 |      - |     792 B |        0.66 |
| ManyRoutes_FirstMatch        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,945,833.0 ns |       NA | 0.00 ns |  0.99 |      - |    1232 B |        1.03 |
| ManyRoutes_LateMatch         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 35,074,082.0 ns |       NA | 0.00 ns |  1.00 |      - |   13568 B |       11.38 |
| BuildDefinition_Simple       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    603,761.0 ns |       NA | 0.00 ns |  0.02 |      - |     584 B |        0.49 |
| BuildDefinition_Complex      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,112,746.0 ns |       NA | 0.00 ns |  0.03 |      - |    1320 B |        1.11 |
