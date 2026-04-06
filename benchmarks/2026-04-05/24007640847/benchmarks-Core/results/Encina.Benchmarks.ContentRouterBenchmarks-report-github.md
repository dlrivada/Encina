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
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,000.8 ns |  9.73 ns | 5.79 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,414.0 ns |  7.30 ns | 4.83 ns |  0.71 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,394.6 ns | 13.99 ns | 8.32 ns |  0.70 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,088.7 ns | 13.21 ns | 8.74 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,099.5 ns |  7.39 ns | 4.40 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        370.3 ns |  2.45 ns | 1.46 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        624.1 ns |  2.67 ns | 1.40 ns |  0.31 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| SimpleRoute_SingleCondition  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,302,682.0 ns |       NA | 0.00 ns |  1.00 |      - |    1192 B |        1.00 |
| ComplexRoute_FirstMatch      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,959,991.0 ns |       NA | 0.00 ns |  0.93 |      - |     792 B |        0.66 |
| ComplexRoute_DefaultFallback | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,753,847.0 ns |       NA | 0.00 ns |  0.93 |      - |     792 B |        0.66 |
| ManyRoutes_FirstMatch        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 35,109,964.0 ns |       NA | 0.00 ns |  1.02 |      - |    1232 B |        1.03 |
| ManyRoutes_LateMatch         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 35,430,161.0 ns |       NA | 0.00 ns |  1.03 |      - |    1232 B |        1.03 |
| BuildDefinition_Simple       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    597,383.0 ns |       NA | 0.00 ns |  0.02 |      - |     584 B |        0.49 |
| BuildDefinition_Complex      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,074,831.0 ns |       NA | 0.00 ns |  0.03 |      - |    1320 B |        1.11 |
